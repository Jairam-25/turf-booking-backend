namespace Application.DTOs
{
    public class CreateBookingDto
    {
        public int SlotId { get; set; }
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }
    }
}
