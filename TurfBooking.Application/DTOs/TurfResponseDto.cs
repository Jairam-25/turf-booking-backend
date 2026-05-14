namespace Application.DTOs
{
    public class TurfResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
    }
}
