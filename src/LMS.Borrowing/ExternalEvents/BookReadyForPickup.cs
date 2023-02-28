using LMS.Borrowing.Schema;

namespace LMS.Borrowing.ExternalEvents;

public record BookReadyForPickup(
    Guid MemberId,
    Guid BookId,
    BookFormat Format,
    DateTimeOffset ReadyOn,
    DateTimeOffset PickBy
);
