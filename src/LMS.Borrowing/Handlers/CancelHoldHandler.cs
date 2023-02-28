using LMS.Borrowing.Aggregate;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Storage;

namespace LMS.Borrowing.Handlers;

public static class CancelHoldHandler
{
    public static async Task Handle(
        VersionedCommand<CancelHold> versionedCommand,
        IStorageDocumentSession documentSession,
        CancellationToken cancellationToken)
    {
        var (command, expectedVersion) = versionedCommand;
        await documentSession.GetAndUpdateAggregate<MemberHoldRegister>(
            command.MemberId,
            expectedVersion,
            current => current.Process(command),
            cancellationToken);
    }
}