using FluentAssertions;
using LMS.Borrowing.Aggregate;
using LMS.Borrowing.Commands;
using LMS.Borrowing.Events;
using LMS.Borrowing.Handlers;
using LMS.Borrowing.Schema;
using LMS.Borrowing.Storage;
using Moq;

namespace LMS.Borrowing.Tests;

public class MemberHoldRegisterDomainServiceTests
{
    private static CancellationToken GetToken()
    {
        var source = new CancellationTokenSource();
        return source.Token;
    }

    [Fact]
    public async void ConsumerShouldRaiseHoldRegisterInitiated()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var ct = GetToken();
        var documentSessionMock = new Mock<IStorageDocumentSession>();
        var command = new InitiateHoldRegister("T1", memberId);

        // Act
        await InitiateHoldRegisterHandler.Handle(command, documentSessionMock.Object, ct);
            
        // Assert
        documentSessionMock.Verify(d =>
            d.InitiateAggregate<MemberHoldRegister>(
                memberId,
                It.Is<HoldRegisterInitiated>(e => e == new HoldRegisterInitiated("T1", memberId)),
                It.IsAny<CancellationToken>()),
            "It should raise HoldRegisterInitiated event.");
    }
    
    [Fact]
    public async void ConsumerShouldRaisePlaceHoldAccepted()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var version = 0;
        var bookId = Guid.NewGuid();
        var bookFormat = BookFormat.Paperback;
        var time = DateTimeOffset.UtcNow;
        var ct = GetToken();
        var documentSessionMock = new Mock<IStorageDocumentSession>();
        var command = new PlaceHold(memberId, bookId, bookFormat, time);

        var emptyRegister = new MemberHoldRegister() { Id = memberId };

        var actualMemberId = Guid.NewGuid();
        var actualExpectedVersion = -1;
        HoldPlaced? actualEvent = null;
        
        documentSessionMock.Setup(d => d.GetAndUpdateAggregate<MemberHoldRegister>(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Func<MemberHoldRegister, object>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object[]>()))
            .Returns((
                Guid i,
                int v,
                Func<MemberHoldRegister, object> handle,
                CancellationToken t,
                object[] externalEvents) =>
                {
                    actualMemberId = i;
                    actualExpectedVersion = v;
                    actualEvent = handle(emptyRegister) as HoldPlaced;
                    return Task.CompletedTask;
                });

        // Act
        await PlaceHoldHandler.Handle(new VersionedCommand<PlaceHold>(command, version), documentSessionMock.Object, ct);

        // Assert
        actualMemberId.Should().Be(memberId, "MemberId passed correctly.");
        actualExpectedVersion.Should().Be(version, "Version passed correctly.");
        actualEvent.Should().Be(new HoldPlaced(memberId, bookId, bookFormat, time));
    }
    
    [Fact]
    public async void ConsumerShouldRaiseHoldMemberHoldRegister()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var version = 0;
        var bookId = Guid.NewGuid();
        var bookFormat = BookFormat.Paperback;
        var time = DateTimeOffset.UtcNow;
        var ct = GetToken();
        var command = new CancelHold(memberId, bookId, bookFormat, time);
        var actualMemberId = Guid.NewGuid();
        var actualExpectedVersion = -1;
        HoldCanceled? actualEvent = null;
        
        var registerWithOneBook = new MemberHoldRegister() { Id = memberId, Holds = { new Hold(bookId, bookFormat, time, null, null) }};
        var documentSessionMock = new Mock<IStorageDocumentSession>();
        documentSessionMock.Setup(d => d.GetAndUpdateAggregate<MemberHoldRegister>(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<Func<MemberHoldRegister, object>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object[]>()))
            .Returns((
                Guid i,
                int v,
                Func<MemberHoldRegister, object> handle,
                CancellationToken t,
                object[] externalEvents) =>
            {
                actualMemberId = i;
                actualExpectedVersion = v;
                actualEvent = handle(registerWithOneBook) as HoldCanceled;
                return Task.CompletedTask;
            });

        // Act
        await CancelHoldHandler.Handle(new VersionedCommand<CancelHold>(command, version), documentSessionMock.Object, ct);

        // Assert
        actualMemberId.Should().Be(memberId, "MemberId passed correctly.");
        actualExpectedVersion.Should().Be(version, "Version passed correctly.");
        actualEvent.Should().Be(new HoldCanceled(memberId, bookId, bookFormat, time));
    }
}