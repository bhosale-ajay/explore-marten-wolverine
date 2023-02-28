using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Events;

public record HoldCanceled(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset RequestedOn
);
