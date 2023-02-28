using LMS.Borrowing.Storage;
using Wolverine;

namespace LMS.Borrowing.Testing;

public class FakeStorageDocumentSession : IStorageDocumentSession
{
    private readonly IMessageContext _outbox;
    
    private readonly  Dictionary<Guid, List<object>> _events = new();
    
    private readonly Dictionary<Guid, object> _aggregates = new();

    public FakeStorageDocumentSession(IMessageContext outbox)
    {
        _outbox = outbox;
    }
    
    private void AppendEvent(Guid id, object @event)
    {
        if (_events.ContainsKey(id))
        {
            _events[id].Add(@event);
        }
        else
        {
            _events.Add(id, new List<object>() { @event});
        }
    }
    public async Task InitiateAggregate<T>(Guid id, object seedEvent, CancellationToken ct, params object[] externalEvents) where T : class
    {
        if (_aggregates.ContainsKey(id))
        {
            throw new InvalidOperationException("Aggregate should be null when initiating the Aggregate.");
        }
        AppendEvent(id, seedEvent);
        // var staticMethod = typeof(T).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        // var aggregate = staticMethod!.Invoke(null, new [] { seedEvent });
        // _aggregates.Add(id, aggregate!);
        foreach (var externalEvent in externalEvents)
        {
            await _outbox.PublishAsync(externalEvent);
        }
    }

    public async Task GetAndUpdateAggregate<T>(Guid id, int version, Func<T, object> handle, CancellationToken ct, params object[] externalEvents) where T : class
    {
        if (!_aggregates.ContainsKey(id))
        {
            throw new InvalidOperationException("Aggregate not set.");
        }
        var @event = handle((_aggregates[id] as T)!);
        AppendEvent(id, @event);
        foreach (var externalEvent in externalEvents)
        {
            await _outbox.PublishAsync(externalEvent);
        }
    }

    public void SetAggregate<T>(T aggregate)
    {
        var idProperty = typeof(T)
                            .GetProperties()
                            .FirstOrDefault(p => 
                                string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase) && 
                                p.PropertyType == typeof(Guid));
        
        if (idProperty == null)
        {
            throw new InvalidOperationException("No ID property");
        }
        
        _aggregates[(Guid)idProperty.GetValue(aggregate)!] = aggregate!;
    }

    public object? GetLastEvent(Guid id)
    {
        return _events.ContainsKey(id) ? _events[id].LastOrDefault() : null;
    }
}