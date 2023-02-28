using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Commands;

public record PlaceHold(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset RequestedOn
);