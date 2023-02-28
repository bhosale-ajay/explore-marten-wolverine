using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.CodeGeneration;
using LMS.Borrowing.Aggregate;
using LMS.Borrowing.API.Middlewares.ExceptionHandling;
using LMS.Borrowing.API.Services;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Handlers;
using LMS.Borrowing.Projection;
using LMS.Borrowing.Schema;
using LMS.Borrowing.Storage;
using Marten;
using Marten.AspNetCore;
using Marten.Events.Projections;
using Marten.Generated.EventStore;
using Marten.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oakton.Resources;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Marten;
using Wolverine.RabbitMQ;
using static LMS.Borrowing.API.Headers.ETagExtensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Adds minimal webapi
    .AddEndpointsApiExplorer()
    // Needed to access HttpContext in service
    .AddHttpContextAccessor()
    .AddScoped<IEnvironmentContext, EnvironmentContext>()
    .AddScoped<IMultiTenantReadOnlySessionFactory, TenantSessionFactoryFactory>();

builder.Services.AddMarten(options =>
    {
        options.Connection(builder.Configuration.GetConnectionString("LMS_DB") ??
                           throw new InvalidOperationException());

        // No default tenant
        options.Advanced.DefaultTenantUsageEnabled = false;

        // associating each record with a tenant identifier (tenant_id column)
        options.Policies.ForAllDocuments(config => config.TenancyStyle = TenancyStyle.Conjoined);
        options.Events.TenancyStyle = TenancyStyle.Conjoined;
        options.Projections.SelfAggregate<MemberHoldRegister>(ProjectionLifecycle.Inline);
        options.Projections.Add<BookMemberHoldProjection>(ProjectionLifecycle.Inline);

        options.GeneratedCodeMode = TypeLoadMode.Static;
        options.SetApplicationProject(typeof(GeneratedEventDocumentStorage).Assembly, "./");
    })
    .IntegrateWithWolverine()
    .BuildSessionsWith<TenantSessionFactoryFactory>(ServiceLifetime.Scoped);

builder.Services.AddScoped<IStorageDocumentSession, StorageDocumentSession>();

builder.Host.UseWolverine((options) =>
    {
        options.Discovery.IncludeAssembly(typeof(InitiateHoldRegisterHandler).Assembly);
        options.UseFluentValidation();
        options.Services.AddSingleton(typeof(IFailureAction<>), typeof(CommandFailureAction<>));

        options
            .UseRabbitMq(rabbit =>
            {
                rabbit.HostName = "localhost";
                rabbit.UserName = "guest";
                rabbit.Password = "guest";
            });

        options
            .PublishAllMessages()
            .ToRabbitExchange("LMS-Borrowing")
            .UseDurableOutbox();
    })
    .UseResourceSetupOnStartup();

var app = builder.Build();

app.UseExceptionHandlingMiddleware(exception => exception switch
{
    Marten.Exceptions.ConcurrencyException => HttpStatusCode.PreconditionFailed,
    StorageAggregateNotFoundException _ => HttpStatusCode.NotFound,
    CommandValidationException _ => HttpStatusCode.BadRequest,
    _ => HttpStatusCode.InternalServerError
});

app.MapGet(
    "/", 
    (IEnvironmentContext envContext) => $"Borrowing API Home Page : {envContext.GetTenantId()}");

app.MapPost(
    "api/holdregister/join/{memberId:guid}",
    async (
        IMessageBus bus,
        IEnvironmentContext envContext,
        Guid memberId,
        CancellationToken ct) =>
    {
        var command = new InitiateHoldRegister(envContext.GetTenantId(), memberId);
        await bus.InvokeAsync(command, ct);
    }
);

app.MapPost(
    "api/holdregister/{memberId:guid}/placeHold",
    async (
        IMessageBus bus,
        Guid memberId,
        [FromHeader(Name = "If-Match")] string eTag,
        HoldHttpRequest body,
        CancellationToken ct) =>
    {
        var command = new PlaceHold(memberId, body.BookId, body.Format, DateTimeOffset.UtcNow);
        var versionedCommand = new VersionedCommand<PlaceHold>(command, ToExpectedVersion(eTag));
        await bus.InvokeAsync(versionedCommand, ct);
    }
);  

app.MapPost(
    "api/holdregister/{memberId:guid}/cancelHold", 
    async (
        IMessageBus bus,
        Guid memberId,
        [FromHeader(Name = "If-Match")] string eTag,
        HoldHttpRequest body,
        CancellationToken ct) =>
    {
        var command = new CancelHold(memberId, body.BookId, body.Format, DateTimeOffset.UtcNow);
        var versionedCommand = new VersionedCommand<CancelHold>(command, ToExpectedVersion(eTag));
        await bus.InvokeAsync(versionedCommand, ct);
    }
);

app.MapPost(
    "api/holdregister/{memberId:guid}/markReady", 
    async (
        IMessageBus bus,
        Guid memberId,
        [FromHeader(Name = "If-Match")] string eTag,
        HoldHttpRequest body,
        CancellationToken ct) =>
    {
        var readyOn = DateTimeOffset.UtcNow;
        // TODO :: Domain Policy
        var expireOn = readyOn.AddDays(5);
        var command = new MarkHoldReady(memberId, body.BookId, body.Format, readyOn, expireOn);
        var versionedCommand = new VersionedCommand<MarkHoldReady>(command, ToExpectedVersion(eTag));
        await bus.InvokeAsync(versionedCommand, ct);
    }
);

app.MapGet(
    "api/holdregister/{memberId:guid}/holds", 
    [OutputCache(Duration = 0)] 
    async (
        HttpContext context,
        IQuerySession session,
        Guid memberId) =>
    {
        await session
                .Json
                .WriteById<MemberHoldRegister>(memberId, context);
    }
);

app.MapGet(
    "api/book-hold-report/{memberId:guid}/", 
    [OutputCache(Duration = 0)]
    async (
        HttpContext context,
        IMultiTenantReadOnlySessionFactory factory,
        IEnvironmentContext envContext,
        Guid memberId) =>
    {
        var tenants = envContext
            .GetActiveTenants()
            .ToList();
        var results = await Task.WhenAll(tenants.Select(t => GetMemberHoldRegister(t, factory, memberId)));
        return results;
    }
);

static async Task<Result> GetMemberHoldRegister(
    string tenantId,
    IMultiTenantReadOnlySessionFactory factory,
    Guid memberId)
{
    var session = factory.QuerySessionForTenant(tenantId);
    var record = await session.LoadAsync<MemberHoldRegister>(memberId);
    return new Result(tenantId, record);
}

app.Run();

public record HoldHttpRequest(
    Guid BookId,
    BookFormat Format
);

public record Result(string TenantId, MemberHoldRegister? Record);