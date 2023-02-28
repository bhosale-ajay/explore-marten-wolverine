using LMS.Borrowing.Events;
using Marten;
using Marten.Events;
using Marten.Events.Projections;
using Weasel.Postgresql.Tables;

namespace LMS.Borrowing.Projection;

public class BookMemberHoldProjection : EventProjection
{
    public BookMemberHoldProjection()
    {
        var table = new Table("book_member_holds");
        table.AddColumn<string>("tenant_id").AsPrimaryKey();
        table.AddColumn<Guid>("member_id").AsPrimaryKey();
        table.AddColumn<Guid>("book_id").AsPrimaryKey();
        table.AddColumn<int>("format").AsPrimaryKey();
        table.AddColumn<DateTimeOffset>("requested_on");
        SchemaObjects.Add(table);
    }
    
    public void Project(IEvent<HoldPlaced> e, IDocumentOperations ops)
    {
        ops.QueueSqlCommand(
            "INSERT INTO book_member_holds (tenant_id, member_id, book_id, format, requested_on) VALUES (?, ?, ?, ?, ?) ON CONFLICT DO NOTHING",
            e.TenantId, e.Data.MemberId, e.Data.BookId, (int) e.Data.Format, e.Data.RequestedOn
        );
    }
 
    public void Project(IEvent<HoldCanceled> e, IDocumentOperations ops)
    {
        ops.QueueSqlCommand(
            "DELETE FROM book_member_holds WHERE tenant_id = ? AND member_id = ? AND book_id = ? AND format = ?",
            e.TenantId, e.Data.MemberId, e.Data.BookId, (int) e.Data.Format
        );
    }
}