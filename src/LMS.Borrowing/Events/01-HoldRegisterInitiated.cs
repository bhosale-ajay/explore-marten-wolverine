namespace LMS.Borrowing.Events;

public record HoldRegisterInitiated(string TenantId, Guid Id);