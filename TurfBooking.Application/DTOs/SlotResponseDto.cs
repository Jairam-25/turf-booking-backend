namespace Application.DTOs
{
    public class SlotResponseDto
    {
        public int SlotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TurfId { get; set; }
        public bool IsBooked { get; set; }
    }
}
