using Domain.Common;

namespace Domain.Entities;

public class Slot : BaseEntity
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsBooked { get; set; }
    public int TurfId { get; set; }
    public Turf? Turf { get; set; }
    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();
}