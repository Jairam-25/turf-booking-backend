using Domain.Common;

namespace Domain.Entities;

public class Booking : BaseEntity
{
    public DateTime BookingDate { get; set; }
    public int UserId { get; set; }
    public int SlotId { get; set; }
    public User? User { get; set; }
    public Slot? Slot { get; set; }
}