namespace Application.DTOs
{
    public class CreateTurfDto
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
    }
}
