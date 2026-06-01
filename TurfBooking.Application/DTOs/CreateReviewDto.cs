namespace Application.DTOs
{
    public class CreateReviewDto
    {
        public int TurfId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
