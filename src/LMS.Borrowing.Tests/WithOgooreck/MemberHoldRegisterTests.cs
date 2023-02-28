using LMS.Borrowing.Aggregate;
using Ogooreck.BusinessLogic;
using FluentAssertions;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Events;
using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Tests;
public class MemberHoldRegisterTests
{
    private readonly HandlerSpecification<MemberHoldRegister> _spec = Specification.For<MemberHoldRegister>(Evolve);

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
    public void PlaceHoldIsAcceptedOnEmptyRegister()
    {
        var emptyRegister = new MemberHoldRegister() { Id = MemberId };
        _spec.Given(emptyRegister)
            .When(state => state.Process(new PlaceHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .Then(new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedWhenThereIsNoHoldForSameBook()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .When(state => state.Process(new PlaceHold(MemberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .Then(new HoldPlaced(MemberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void PlaceHoldIsRejectedWhenMemberAlreadyPlaced5Holds()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId3, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId4, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(MemberId, BookId5, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .When(state => state.Process(new PlaceHold(MemberId, BookId6, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("Member can not hold more than 5 books."));
    }
    
    [Fact]
    public void PlaceHoldIsRejectedWhenThereIsAlreadyAHold()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .When(state => state.Process(new PlaceHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("A hold for requested book already exists."));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedWhenHoldPlacedForTwoDifferentFormats()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .When(state => state.Process(new PlaceHold(MemberId, BookId1, BookFormat.Paperback, CurrentDateTimeOffset)))
            .Then(new HoldPlaced(MemberId, BookId1, BookFormat.Paperback, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void CancelHoldIsAcceptedWhenHoldExists()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .When(state => state.Process(new CancelHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .Then(new HoldCanceled(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public void CancelHoldIsRejectedWhenHoldDoesNotExists()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId)
            )
            .When(state => state.Process(new CancelHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("No such hold."));
    }
    
    [Fact]
    public void PlaceHoldIsAcceptedAfterCancellingPreviousHold()
    {
        _spec.Given(
                new HoldRegisterInitiated(TenantId, MemberId),
                new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldCanceled(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .When(state => state.Process(new PlaceHold(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)))
            .Then(new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
}