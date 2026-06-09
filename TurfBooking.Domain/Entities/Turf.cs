using Domain.Common;

namespace Domain.Entities;

public class Turf : BaseEntity 
{
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal PricePerHour { get; set; }
    
    public decimal? DayTimePrice { get; set; }
    public decimal? AfternoonPrice { get; set; }
    public decimal? NightTimePrice { get; set; }

    public ICollection<Slot> Slots { get; set; }
        = new List<Slot>();

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();

    public int? OwnerId { get; set; }
    public Owner? Owner { get; set; }

    // New fields
    public string Description { get; set; } = string.Empty;
    public string TurfType { get; set; } = string.Empty; // Football, Cricket, Badminton, Multi Sports
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Pending Verification";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TurfDocument> Documents { get; set; } = new List<TurfDocument>();
    public ICollection<TurfImage> Images { get; set; } = new List<TurfImage>();
}