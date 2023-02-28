namespace LMS.Borrowing.Commands;

public record VersionedCommand<T>(
    T Command,
    int ExpectedVersion
);