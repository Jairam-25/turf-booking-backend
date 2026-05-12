namespace Domain.Entities;

public class Slot
{
    public int Id { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsBooked { get; set; }

    // Foreign Key
    public int TurfId { get; set; }

    // Navigation Property
    public Turf? Turf { get; set; } 

    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();
}