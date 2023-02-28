using LMS.Borrowing.Events;
using LMS.Borrowing.Projection;
using LMS.Borrowing.Schema;
using Marten;
using Marten.Events;
using Moq;

namespace LMS.Borrowing.Tests;

public class BookMemberHoldProjectionTests
{
    private static readonly string TenantId = "T1";
    private static readonly Guid MemberId = new ("c62ec096-c7f7-45a4-a840-4a2e479a749e");
    private static readonly Guid BookId1 = new ("9368e8b5-acc9-4451-8857-76cf758b220c");
    private static readonly DateTimeOffset CurrentDateTimeOffset = DateTimeOffset.UtcNow;
    
    [Fact]
    public void Project_For_PlaceHoldAccepted_CorrectlyPassTheParameters()
    {
        // Arrange
        var eventMock = new Mock<IEvent<HoldPlaced>>();
        var @event = new HoldPlaced(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset);
        eventMock.Setup(e => e.Data).Returns(@event);
        eventMock.Setup(e => e.TenantId).Returns(TenantId);
        
        var optionsMock = new Mock<IDocumentOperations>();
        
        var projection = new BookMemberHoldProjection();
        
        // Act
        projection.Project(eventMock.Object, optionsMock.Object);

        // Assert
        object[] expectedParams = { TenantId, MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset};
        optionsMock.Verify(o => o.QueueSqlCommand(
            It.IsAny<string>(),
            It.Is<object[]>(ps => ps.Length == expectedParams.Length &&
                                  ps[0] == expectedParams[0] &&
                                  ps[1].ToString() == expectedParams[1].ToString() &&
                                  ps[2].ToString() == expectedParams[2].ToString() &&
                                  (int) ps[3] == (int) expectedParams[3] &&
                                  ps[4].ToString() == expectedParams[4].ToString()
            )));
    }
    
    [Fact]
    public void Project_For_CancelHoldAccepted_CorrectlyPassTheParameters()
    {
        // Arrange
        var eventMock = new Mock<IEvent<HoldCanceled>>();
        var @event = new HoldCanceled(MemberId, BookId1, BookFormat.Hardbound, CurrentDateTimeOffset);
        eventMock.Setup(e => e.Data).Returns(@event);
        eventMock.Setup(e => e.TenantId).Returns(TenantId);
        
        var optionsMock = new Mock<IDocumentOperations>();
        
        var projection = new BookMemberHoldProjection();
        
        // Act
        projection.Project(eventMock.Object, optionsMock.Object);

        // Assert
        object[] expectedParams = { TenantId, MemberId, BookId1, BookFormat.Hardbound};
        optionsMock.Verify(o => o.QueueSqlCommand(
            It.IsAny<string>(),
            It.Is<object[]>(ps => ps.Length == expectedParams.Length &&
                                  ps[0] == expectedParams[0] &&
                                  ps[1].ToString() == expectedParams[1].ToString() &&
                                  ps[2].ToString() == expectedParams[2].ToString() &&
                                  (int) ps[3] == (int) expectedParams[3]
            )));
    }
}