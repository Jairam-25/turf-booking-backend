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
    public User? Owner { get; set; }
}