using LMS.Borrowing.Aggregate;
using LMS.Borrowing.Commands;
using LMS.Borrowing.ExternalEvents;
using LMS.Borrowing.Storage;

namespace LMS.Borrowing.Handlers;

public static class MarkHoldReadyHandler
{
    public static async Task Handle(
        VersionedCommand<MarkHoldReady> versionedCommand,
        IStorageDocumentSession documentSession,
        CancellationToken cancellationToken
        )
    {
        var (command, expectedVersion) = versionedCommand;
        await documentSession.GetAndUpdateAggregate<MemberHoldRegister>(
            command.MemberId,
            expectedVersion,
            current => current.Process(command),
            cancellationToken,
            new BookReadyForPickup(command.MemberId, command.BookId, command.Format, command.ReadyOn, command.ExpireOn),
            new CheckIfPickupExpired(command.MemberId, command.BookId, command.Format, command.ReadyOn, command.ExpireOn));
    }
}
