using Marten;
using Marten.Exceptions;
using Wolverine.Marten;

namespace LMS.Borrowing.Storage;

public interface IStorageDocumentSession
{
    Task InitiateAggregate<T>(
        Guid id, 
        object seedEvent,
        CancellationToken ct,
        params object[] externalEvents)
        where T : class;

    Task GetAndUpdateAggregate<T>(
        Guid id,
        int version,
        Func<T, object> handle, 
        CancellationToken ct, 
        params object[] externalEvents)
        where T : class;
}

public class StorageDocumentSession :
    IStorageDocumentSession
{
    private readonly IDocumentSession _documentSession;
    private readonly IMartenOutbox _outbox;

    public StorageDocumentSession(IMartenOutbox outbox)
    {
        _documentSession = outbox.Session!;
        _outbox = outbox;
    }

    public async Task InitiateAggregate<T>(
        Guid id,
        object seedEvent,
        CancellationToken ct,
        params object[] externalEvents)
        where T : class
    {
        try
        {
            _documentSession.Events.StartStream<T>(id, seedEvent);
            foreach (var externalEvent in externalEvents)
            {
                await _outbox.SendAsync(externalEvent);
            }
            // await needed here as ExistingStreamIdCollisionException needs to be suppressed
            await _documentSession.SaveChangesAsync(token: ct);
        }
        catch (ExistingStreamIdCollisionException)
        {
        }
    }

    public Task GetAndUpdateAggregate<T>(
        Guid id,
        int version,
        Func<T, object> handle,
        CancellationToken ct,
        params object[] externalEvents)
        where T : class =>
        _documentSession.Events.WriteToAggregate<T>(
            id,
            version,
            async stream =>
            {
                var aggregate = stream.Aggregate;
                if (aggregate == null)
                {
                    throw StorageAggregateNotFoundException.For<T>(id);
                }
                var @event = handle(aggregate);
                stream.AppendOne(@event);
                foreach (var externalEvent in externalEvents)
                {
                    await _outbox.PublishAsync(externalEvent);
                }
            },
            ct);
}