using Domain.Common;

namespace Domain.Entities;

public class Review : BaseEntity
{
    public int TurfId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; } // 1 to 5 stars
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Turf? Turf { get; set; }
    public User? User { get; set; }
}
