using FluentValidation;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Schema;

namespace LMS.Borrowing.CommandValidator;

public class PlaceHoldValidator : AbstractValidator<VersionedCommand<PlaceHold>>
{
    public PlaceHoldValidator()
    {
        RuleFor(x => x.Command.MemberId)
            .NotNull()
            .NotEmpty()
            .WithName("MemberId");
        RuleFor(x => x.Command.BookId)
            .NotNull()
            .NotEmpty()
            .WithName("BookId");
        RuleFor(x => x.Command.Format)
            .NotEqual(BookFormat.None)
            .WithName("Book Format");
        RuleFor(x => x.Command.RequestedOn)
            .Must(d => d.Offset.Ticks == 0)
            .WithMessage("RequestedOn should be a UTC Date.");
    }
}