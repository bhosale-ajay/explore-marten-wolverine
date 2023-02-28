using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Events;

public record HoldExpired(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset ExpiredOn
);
