using LMS.Borrowing.Aggregate;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Storage;

namespace LMS.Borrowing.Handlers;

public static class InitiateHoldRegisterHandler
{
    public static async Task Handle(
        InitiateHoldRegister command,
        IStorageDocumentSession documentSession,
        CancellationToken cancellationToken)
    {
        await documentSession.InitiateAggregate<MemberHoldRegister>(
            command.MemberId,
            MemberHoldRegister.Process(command), 
            cancellationToken);
    }
}
