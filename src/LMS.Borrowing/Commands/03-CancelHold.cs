using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Commands;

public record CancelHold(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset RequestedOn
);