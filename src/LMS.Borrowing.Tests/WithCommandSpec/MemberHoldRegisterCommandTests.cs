using FluentAssertions;
using LMS.Borrowing.Aggregate;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Events;
using LMS.Borrowing.ExternalEvents;
using LMS.Borrowing.Schema;
using LMS.Borrowing.Testing;

namespace LMS.Borrowing.Tests;

public class MemberHoldRegisterCommandTests : WolverineIntegrationContext
{
    private readonly CommandSpecification<MemberHoldRegister> _specification;
    public MemberHoldRegisterCommandTests(WolverineHostFixture fixture) : base(fixture)
    {
        _specification = new CommandSpecification<MemberHoldRegister>(
            Evolve,
            this.DocumentSession, 
            this.Dispatch);
    }
    
    private static readonly string TenantId = "T1";
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
    public async Task InitiateHoldRegisterAccepted()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                // Nothing
            )
            .When(new InitiateHoldRegister(TenantId, memberId))
            .ThenFor(
                memberId,
                new HoldRegisterInitiated(TenantId, memberId));
    }
    
    [Fact]
    public async Task PlaceHoldIsAcceptedOnEmptyRegister()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId)
            )
            .WhenVersioned(new PlaceHold(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenFor(
                memberId,
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public async Task PlaceHoldIsAcceptedWhenThereIsNoHoldForSameBook()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(memberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenFor(
                memberId,
                new HoldPlaced(memberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public async Task PlaceHoldIsRejectedWhenMemberAlreadyPlaced5Holds()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(memberId, BookId2, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(memberId, BookId3, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(memberId, BookId4, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldPlaced(memberId, BookId5, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(memberId, BookId6, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenThrows<InvalidOperationException>(
                exception => exception.Message
                    .Should().Be("Member can not hold more than 5 books."));
    }
    
    [Fact]
    public async Task PlaceHoldIsRejectedWhenThereIsAlreadyAHold()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenThrows<InvalidOperationException>(
                exception => exception.Message
                    .Should().Be("A hold for requested book already exists."));
    }
    
    [Fact]
    public async Task PlaceHoldIsAcceptedWhenHoldPlacedForTwoDifferentFormats()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(memberId, BookId1, BookFormat.Paperback, CurrentDateTimeOffset))
            .ThenFor(
                memberId,
                new HoldPlaced(memberId, BookId1, BookFormat.Paperback, CurrentDateTimeOffset));
    }
    
    [Fact]
    public async Task CancelHoldIsAcceptedWhenHoldExists()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new CancelHold(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenFor(
                memberId,
                new HoldCanceled(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public async Task CancelHoldIsRejectedWhenHoldDoesNotExists()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId)
            )
            .WhenVersioned(new CancelHold(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenThrows<InvalidOperationException>(exception => exception.Message.Should().Be("No such hold."));
    }
    
    [Fact]
    public async Task PlaceHoldIsAcceptedAfterCancellingPreviousHold()
    {
        var memberId = Guid.NewGuid();
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset),
                new HoldCanceled(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new PlaceHold(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset))
            .ThenFor(
                memberId,
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset));
    }
    
    [Fact]
    public async Task MarkHoldReadyIsAcceptedIfAHoldPresentAndRaiseCorrectExternalEvents()
    {
        var memberId = Guid.NewGuid();
        var readyOn = DateTimeOffset.UtcNow;
        var expireOn = DateTimeOffset.UtcNow.AddDays(5);
        await _specification
            .Given(
                new HoldRegisterInitiated(TenantId, memberId),
                new HoldPlaced(memberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset)
            )
            .WhenVersioned(new MarkHoldReady(memberId, BookId1, BookFormat.Hardbound, readyOn, expireOn))
            .ThenFor(
                memberId,
                new HoldReady(memberId, BookId1, BookFormat.Hardbound, readyOn, expireOn),
                new object[]
                {
                    new BookReadyForPickup(memberId, BookId1, BookFormat.Hardbound, readyOn, expireOn),
                    new CheckIfPickupExpired(memberId, BookId1, BookFormat.Hardbound, readyOn, expireOn)
                });
    }
}