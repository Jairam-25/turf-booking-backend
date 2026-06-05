namespace Application.DTOs
{
    public class TurfResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public decimal? DayTimePrice { get; set; }
        public decimal? AfternoonPrice { get; set; }
        public decimal? NightTimePrice { get; set; }
        public double Rating { get; set; }
    }
}
