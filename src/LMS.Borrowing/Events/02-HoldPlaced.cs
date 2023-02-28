using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Events;

public record HoldPlaced(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset RequestedOn
);
