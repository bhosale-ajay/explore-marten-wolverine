using FluentAssertions;
using LMS.Borrowing.Aggregate;
using LMS.Borrowing.Handlers;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Events;
using LMS.Borrowing.Schema;
using LMS.Borrowing.Testing;

namespace LMS.Borrowing.Tests;
public class MemberHoldRegisterTestsWithTestSpec
{
    private static CancellationToken GetToken()
    {
        var source = new CancellationTokenSource();
        return source.Token;
    }
    
    private static readonly string TenantId = "T1";
    private static readonly Guid MemberId = new Guid("c62ec096-c7f7-45a4-a840-4a2e479a749e");
    private static readonly Guid BookId1 = new Guid("9368e8b5-acc9-4451-8857-76cf758b220c");
    private static readonly Guid BookId2 = new Guid("a5e7b78c-7275-4c1d-9595-6102885042f7");
    private static readonly Guid BookId3 = new Guid("1f433d2c-796d-4042-b89e-ad27c68dba79");
    private static readonly Guid BookId4 = new Guid("0705b696-3cc9-4d37-afce-fa2959f6715d");
    private static readonly Guid BookId5 = new Guid("78cd719c-3e23-47aa-97e7-77d9b479b842");    
    private static readonly Guid BookId6 = new Guid("e7d6f70f-fe76-4b93-a50f-fb448e3f1363");
    private static readonly DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.UtcNow;

    private static readonly Func<MemberHoldRegister, object, MemberHoldRegister> Evolve =
        (memberHoldRegister, @event) =>
        {
            return @event switch
            {
                HoldRegisterInitiated holdRegisterInitiated => MemberHoldRegister.Create(holdRegisterInitiated),
                HoldPlaced placeHoldAccepted => memberHoldRegister.Apply(placeHoldAccepted),
                HoldCanceled cancelHoldAccepted => memberHoldRegister.Apply(cancelHoldAccepted),
                _ => memberHoldRegister,
            };
        };

    [Fact]
    public void InitiateHoldRegisterAccepted()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, InitiateHoldRegister>(
            Evolve, 
            async (c, ds) => await InitiateHoldRegisterHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                // Nothing
            )
            .When(new InitiateHoldRegister(TenantId, MemberId))
            .Then(new HoldRegisterInitiated(TenantId, MemberId));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedOnEmptyRegister()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<PlaceHold>>(
            Evolve, 
            async (c, ds) => await PlaceHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId)
            )
            .WhenVersioned(new PlaceHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .Then(new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedWhenThereIsNoHoldForSameBook()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<PlaceHold>>(
            Evolve, 
            async (c, ds) => await PlaceHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(MemberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset))
            .Then(new HoldPlaced(MemberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void PlaceHoldIsRejectedWhenMemberAlreadyPlaced5Holds()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<PlaceHold>>(
            Evolve, 
            async (c, ds) => await PlaceHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId3, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId4, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId5, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(MemberId, BookId6, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("Member can not hold more than 5 books."));
    }
    
    [Fact]
    public void PlaceHoldIsRejectedWhenThereIsAlreadyAHold()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<PlaceHold>>(
            Evolve, 
            async (c, ds) => await PlaceHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("A hold for requested book already exists."));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedWhenHoldPlacedForTwoDifferentFormats()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<PlaceHold>>(
            Evolve, 
            async (c, ds) => await PlaceHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(MemberId, BookId1, BookFormat.Paperback, CurrentDateTimeOffset))
            .Then(new HoldPlaced(MemberId, BookId1, BookFormat.Paperback, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void CancelHoldIsAcceptedWhenHoldExists()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<CancelHold>>(
            Evolve, 
            async (c, ds) => await CancelHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new CancelHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .Then(new HoldCanceled(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void CancelHoldIsRejectedWhenHoldDoesNotExists()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<CancelHold>>(
            Evolve, 
            async (c, ds) => await CancelHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId)
            )
            .WhenVersioned(new CancelHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("No such hold."));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedAfterCancellingPreviousHold()
    {
        var testSpecification = new TestSpecification<MemberHoldRegister, VersionedCommand<PlaceHold>>(
            Evolve, 
            async (c, ds) => await PlaceHoldHandler.Handle(c, ds, GetToken()));
        
        testSpecification.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldCanceled(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .Then(new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
}