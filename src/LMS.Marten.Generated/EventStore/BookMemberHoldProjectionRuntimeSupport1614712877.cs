// <auto-generated/>
#pragma warning disable
using LMS.Borrowing.Projection;
using System.Linq;

namespace Marten.Generated.EventStore
{
    // START: BookMemberHoldProjectionInlineProjection1614712877
    public class BookMemberHoldProjectionInlineProjection1614712877 : Marten.Events.Projections.SyncEventProjection<LMS.Borrowing.Projection.BookMemberHoldProjection>
    {
        private readonly LMS.Borrowing.Projection.BookMemberHoldProjection _projection;

        public BookMemberHoldProjectionInlineProjection1614712877(LMS.Borrowing.Projection.BookMemberHoldProjection projection) : base(projection)
        {
            _projection = projection;
        }



        public override void ApplyEvent(Marten.IDocumentOperations operations, Marten.Events.StreamAction streamAction, Marten.Events.IEvent e)
        {
            switch (e)
            {
                case Marten.Events.IEvent<LMS.Borrowing.Events.HoldPlaced> event_HoldPlaced10:
                    Projection.Project(event_HoldPlaced10, operations);
                    break;
                case Marten.Events.IEvent<LMS.Borrowing.Events.HoldCanceled> event_HoldCanceled11:
                    Projection.Project(event_HoldCanceled11, operations);
                    break;
            }

        }

    }

    // END: BookMemberHoldProjectionInlineProjection1614712877
    
    
}

