namespace Application.DTOs
{
    public class UpdateProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Address { get; set; }
        public string? State { get; set; }
        public string? District { get; set; }
        public string? Pincode { get; set; }
        public string? MaritalStatus { get; set; }
        public string? PlayerType { get; set; }
        public string? PlayingLevel { get; set; }
    }
}
