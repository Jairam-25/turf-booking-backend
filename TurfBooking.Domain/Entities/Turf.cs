namespace Domain.Entities;

public class Turf
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public decimal PricePerHour { get; set; }

    // Navigation Property
    public ICollection<Slot> Slots { get; set; }
        = new List<Slot>();
}