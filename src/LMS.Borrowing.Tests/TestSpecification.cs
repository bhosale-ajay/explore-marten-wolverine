using Moq;
using FluentAssertions;
using LMS.Borrowing.Storage;

namespace LMS.Borrowing.Testing;

public class TestSpecification<TAggregate, TCommand> 
    where TAggregate : class
    where TCommand : class
{
    private readonly Func<TAggregate, object, TAggregate> _evolve;
    private readonly Func<TCommand, IStorageDocumentSession, Task> _handler;
    private object[]? _events;
    private TCommand? _command;
    private object? _actualEvent;

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ImpossibleException : Exception
    {
        public ImpossibleException() : base("Impossible Exception.")
        {
        }
    }

    public TestSpecification(Func<TAggregate,object,TAggregate> evolve, Func<TCommand, IStorageDocumentSession, Task> handler)
    {
        _evolve = evolve;
        _handler = handler;
    }

    public TestSpecification<TAggregate, TCommand> Given(params object[]? events)
    {
        _events = events;
        return this;
    }
    
    public TestSpecification<TAggregate, TCommand> When(TCommand command)
    {
        _command = command;
        return this;
    }
    
    private void Process<TException>(Action<TException>? assert = null) where TException : Exception
    {
        if (_command == null)
        {
            throw new InvalidOperationException("Command is not provided, please provide a valid command with 'When'.");
        }
        
        TAggregate? aggregate = default;

        if (_events != null)
        {
            aggregate = _events.Aggregate(aggregate, (a, e) => this._evolve(a, e));
        }

        var documentSessionMock = new Mock<IStorageDocumentSession>();
        documentSessionMock.Setup(d => d.GetAndUpdateAggregate<TAggregate>(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Func<TAggregate, object>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object[]>()))
            .Returns((
                Guid i,
                int v,
                Func<TAggregate, object> handle,
                CancellationToken t,
                object[] externalEvents) =>
            {
                this._actualEvent = handle(aggregate);
                return Task.CompletedTask;
            });

        documentSessionMock.Setup(d => d.InitiateAggregate<TAggregate>(
                It.IsAny<Guid>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object[]>()))
            .Returns((
                Guid i,
                object @event,
                CancellationToken t,
                object[] externalEvents) =>
            {
                if (aggregate != null)
                {
                    throw new InvalidOperationException("Aggregate should be null when initiating the Aggregate.");
                }
                this._actualEvent = @event;
                return Task.CompletedTask;
            });

        try
        {
            _handler(_command, documentSessionMock.Object);
        }
        catch (TException ex)
        {
            assert?.Invoke(ex);
        }
    }
    
    public void Then(object expectedEvent)
    {
        Process<ImpossibleException>();
        _actualEvent.Should().Be(expectedEvent, "Event generated correctly.");
    }
    
    public void ThenThrows<TException>(
        Action<TException>? assert = null)
        where TException : Exception
    {
        Process<TException>(assert);
    }
}
