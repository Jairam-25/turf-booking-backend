namespace Application.DTOs
{
    public class SendOtpRequestDto
    {
        public string EmailOrPhone { get; set; } = string.Empty;
        public string? FcmToken { get; set; }
    }

    public class VerifyOtpRequestDto
    {
        public string EmailOrPhone { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
