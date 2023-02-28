using FluentValidation.Results;
using Wolverine.FluentValidation;

namespace LMS.Borrowing.API.Middlewares.ExceptionHandling;

public class CommandValidationException : Exception
{
    public CommandValidationException(string message) : base(message)
    {
    }
}

public class CommandFailureAction<T> : IFailureAction<T>
{
    public void Throw(T message, IReadOnlyList<ValidationFailure> failures)
    {
        throw new CommandValidationException("Validation failed : " + string.Join(",", failures.Select(x => x.ErrorMessage)));
    }
}