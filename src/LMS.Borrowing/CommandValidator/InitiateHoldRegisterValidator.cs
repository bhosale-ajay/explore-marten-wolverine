using FluentValidation;
using LMS.Borrowing.Commands;

namespace LMS.Borrowing.CommandValidator;

public class InitiateHoldRegisterValidator : AbstractValidator<InitiateHoldRegister>
{
    public InitiateHoldRegisterValidator()
    {
        RuleFor(x => x.TenantId)
            .NotNull()
            .NotEmpty();
        RuleFor(x => x.MemberId)
            .NotNull()
            .NotEmpty();
    }
}