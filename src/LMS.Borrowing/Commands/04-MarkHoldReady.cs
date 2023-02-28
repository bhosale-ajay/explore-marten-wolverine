using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Commands;

public record MarkHoldReady(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset ReadyOn,
    DateTimeOffset ExpireOn
);
