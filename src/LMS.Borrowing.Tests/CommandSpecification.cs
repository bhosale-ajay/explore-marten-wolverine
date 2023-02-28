using FluentAssertions;

namespace LMS.Borrowing.Testing;

public class CommandSpecification<TAggregate> 
    where TAggregate : class
{
    private readonly Func<TAggregate, object, TAggregate> _evolve;
    private readonly FakeStorageDocumentSession _documentSession;
    private readonly Func<object, Task<IEnumerable<object>>> _dispatcher;

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ImpossibleException : Exception
    {
        private ImpossibleException() : base("Impossible Exception.")
        {
        }
    }
    
    private object[]? _previousEvents;
    private object? _command;
    private IEnumerable<object>? _externalEvents;
    private bool _expectedErrorCaught;
    
    public CommandSpecification(
        Func<TAggregate,object,TAggregate> evolve,
        FakeStorageDocumentSession documentSession,
        Func<object,Task<IEnumerable<object>>> dispatcher)
    {
        _evolve = evolve;
        _documentSession = documentSession;
        _dispatcher = dispatcher;
    }
    
    public CommandSpecification<TAggregate> Given(params object[]? events)
    {
        _previousEvents = events;
        return this;
    }
    
    public CommandSpecification<TAggregate> When(object command)
    {
        _command = command;
        return this;
    }
    
    private async Task Process<TException>(Action<TException>? assert = null) where TException : Exception
    {
        if (_command == null)
        {
            throw new InvalidOperationException("Command is not provided, please provide a valid command with 'When'.");
        }
        
        if (_previousEvents != null && _previousEvents.Length != 0)
        {
            TAggregate? aggregate = default;
            aggregate = _previousEvents.Aggregate(aggregate, (a, e) => _evolve(a, e));
            _documentSession.SetAggregate(aggregate);
        }
        try
        {
            _externalEvents = await _dispatcher(_command);
        }
        catch (TException ex)
        {
            _expectedErrorCaught = true;
            assert?.Invoke(ex);
        }
    }

    public async Task ThenFor(
        Guid id,
        object expectedAggregateEvent)
    {
        await ThenFor(id, expectedAggregateEvent, new List<object>());
    }
    
    public async Task ThenFor(
        Guid id,
        object expectedAggregateEvent,
        IEnumerable<object>? expectedExternalEvents)
    {
        await Process<ImpossibleException>();
        _documentSession.GetLastEvent(id).Should().Be(expectedAggregateEvent, "Event generated correctly.");
        _externalEvents.Should().Equal(expectedExternalEvents, "");
    }
    
    public async Task ThenThrows<TException>(
        Action<TException>? assert = null)
        where TException : Exception
    {
        await Process<TException>(assert);
        _expectedErrorCaught.Should().Be(true, "As expected exception must have caught");
    }
}
