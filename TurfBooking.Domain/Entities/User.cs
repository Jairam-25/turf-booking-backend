namespace TurfBooking.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Navigation Property
    public ICollection<Booking> Bookings { get; set; }
        = new List<Booking>();
}