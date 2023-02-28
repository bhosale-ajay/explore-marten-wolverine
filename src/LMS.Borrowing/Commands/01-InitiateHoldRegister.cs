namespace LMS.Borrowing.Commands;

public record InitiateHoldRegister(
    string TenantId,
    Guid MemberId
);