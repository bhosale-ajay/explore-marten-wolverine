using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Commands;

public record CheckIfPickupExpired(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset ReadyOn,
    DateTimeOffset ExpireOn
);
