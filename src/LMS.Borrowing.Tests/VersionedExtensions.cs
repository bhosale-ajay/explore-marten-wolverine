using LMS.Borrowing.Commands;
using LMS.Borrowing.Testing;

namespace LMS.Borrowing.Tests;

public static class VersionedExtensions 
{
    public static TestSpecification<TAggregate, VersionedCommand<TInner>> WhenVersioned<TAggregate, TInner>(
        this TestSpecification<TAggregate, VersionedCommand<TInner>> specification, 
        TInner inner)
        where TAggregate : class
    {
        return specification.When(new VersionedCommand<TInner>(inner, 1));
    }
    
    public static CommandSpecification<TAggregate> WhenVersioned<TAggregate, TCommand>(
        this CommandSpecification<TAggregate> specification, 
        TCommand command)
        where TAggregate : class
    {
        return specification.When(new VersionedCommand<TCommand>(command, 1));
    }
}