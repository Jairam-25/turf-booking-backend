namespace TurfBooking.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public DateTime BookingDate { get; set; }

    // Foreign Keys
    public int UserId { get; set; }

    public int SlotId { get; set; }

    // Navigation Properties
    public User? User { get; set; }

    public Slot? Slot { get; set; }
}