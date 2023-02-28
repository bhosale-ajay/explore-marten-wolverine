using LMS.Borrowing.Commands;
using LMS.Borrowing.Events;
using LMS.Borrowing.Schema;

namespace LMS.Borrowing.Aggregate;

public record Hold
(
    Guid BookId,
    BookFormat Format,
    DateTimeOffset PlacedOn,
    DateTimeOffset? ReadyOn,
    DateTimeOffset? ExpireOn
);

public class MemberHoldRegister
{
    public Guid Id { get; set; }
    
    public List<Hold> Holds { get; } = new();
    
    public string? TenantId { get; set; }
    
    public int Version { get; set; }
    
    public static HoldRegisterInitiated Process(InitiateHoldRegister @event)
    {
        return new HoldRegisterInitiated(@event.TenantId, @event.MemberId);
    }
    
    public HoldPlaced Process(PlaceHold command)
    {
        if (this.Holds.Count >= 5)
        {
            throw new InvalidOperationException("Member can not hold more than 5 books.");
        }

        if (this.Holds.Any(h => h.BookId == command.BookId && h.Format == command.Format))
        {
            throw new InvalidOperationException("A hold for requested book already exists.");
        }

        return new HoldPlaced(this.Id, command.BookId, command.Format, command.RequestedOn);
    }
    
    public HoldCanceled Process(CancelHold command)
    {
        if (!this.Holds.Any(h => h.BookId == command.BookId && h.Format == command.Format))
        {
            throw new InvalidOperationException("No such hold.");
        }
        
        return new HoldCanceled(this.Id, command.BookId, command.Format, command.RequestedOn);
    }
    
    public HoldReady Process(MarkHoldReady command)
    {
        var hold = this.Holds.FirstOrDefault(h => h.BookId == command.BookId && h.Format == command.Format);
        if (hold == null)
        {
            throw new InvalidOperationException("No such hold.");
        }

        // if (hold.ReadyOn != null)
        // {
        //     throw new InvalidOperationException("Hold already ready");
        // }
        
        return new HoldReady(this.Id, command.BookId, command.Format, command.ReadyOn, command.ExpireOn);
    }

    public static MemberHoldRegister Create(HoldRegisterInitiated @event) =>
        new() { Id = @event.Id, TenantId = @event.TenantId };

    public MemberHoldRegister Apply(HoldPlaced @event)
    {
        // According to Marten creator creating new instance (immutable style) is slow
        this.Holds.Add(new Hold(@event.BookId, @event.Format, @event.RequestedOn, null, null));
        return this;
    }
    
    public MemberHoldRegister Apply(HoldCanceled @event)
    {
        this.Holds.RemoveAll(h => h.BookId == @event.BookId && h.Format == @event.Format);
        return this;
    }
    
    public MemberHoldRegister Apply(HoldReady @event)
    {
        var hold = this.Holds.FirstOrDefault(h => h.BookId == @event.BookId && h.Format == @event.Format);
        if (hold == null || hold.ReadyOn != null)
        {
            return this;
        }
        this.Holds.RemoveAll(h => h.BookId == @event.BookId && h.Format == @event.Format);
        this.Holds.Add(hold with { ReadyOn = @event.ReadyOn, ExpireOn = @event.ExpireOn });
        Console.WriteLine("Hold Updated.");
        return this;
    }
}