using LMS.Borrowing.API.Middlewares.ExceptionHandling;
using LMS.Borrowing.Handlers;
using LMS.Borrowing.Storage;
using LMS.Borrowing.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Tracking;

namespace LMS.Borrowing.Tests;

public class WolverineHostFixture : IDisposable, IAsyncLifetime
{
    public IHost WolverineHost { get; private set; }

    public async Task InitializeAsync()
    {
        var builder = Host.CreateDefaultBuilder();
        builder.UseWolverine(SetUpWolverine);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<FakeStorageDocumentSession>(
                serviceProvider => new FakeStorageDocumentSession(serviceProvider.GetService<IMessageContext>() ?? throw new InvalidOperationException("No valid dependency")));
            services.AddSingleton<IStorageDocumentSession>(
                serviceProvider => serviceProvider.GetService<FakeStorageDocumentSession>()!);
        });
        WolverineHost = await builder.StartAsync();
    }
 
    public Task DisposeAsync()
    {
        return WolverineHost.StopAsync();
    }
 
    public void Dispose()
    {
        WolverineHost?.Dispose();
    }
    
    private static void SetUpWolverine( HostBuilderContext context, WolverineOptions options)
    {
        options.StubAllExternalTransports();
        options.Discovery.IncludeAssembly(typeof(InitiateHoldRegisterHandler).Assembly);
        options.Services.AddSingleton(typeof(IFailureAction<>), typeof(CommandFailureAction<>));
        options.UseFluentValidation();
    }
}

[CollectionDefinition("WolverineIntegration")]
public class WolverineFixtureCollection : ICollectionFixture<WolverineHostFixture>
{
}

[Collection("WolverineIntegration")]
public abstract class WolverineIntegrationContext
{
    public IHost WolverineHost { get; }
    
    public IServiceProvider ServiceProvider { get; }
    
    protected WolverineIntegrationContext(WolverineHostFixture fixture)
    {
        WolverineHost = fixture.WolverineHost;
        ServiceProvider = fixture.WolverineHost.Services;
    }

    public FakeStorageDocumentSession DocumentSession => this.ServiceProvider.GetService<FakeStorageDocumentSession>()!;

    public async Task<IEnumerable<object>> Dispatch(object command)
    {
        var session = await this.WolverineHost.InvokeMessageAndWaitAsync(command);
        var sentMessages = session
                            .AllRecordsInOrder()
                            .Where(e => e.EventType == EventType.NoRoutes)
                            .Select(e => e.Message!);
        return sentMessages;
    }
}