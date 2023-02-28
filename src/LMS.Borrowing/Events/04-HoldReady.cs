using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Events;

public record HoldReady(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset ReadyOn,
    DateTimeOffset ExpireOn
);
