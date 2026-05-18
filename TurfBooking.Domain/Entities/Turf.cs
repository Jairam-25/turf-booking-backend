using Domain.Common;

namespace Domain.Entities;

public class Turf : BaseEntity 
{
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal PricePerHour { get; set; }

    public ICollection<Slot> Slots { get; set; }
        = new List<Slot>();
}