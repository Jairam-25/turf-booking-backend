namespace Application.DTOs
{
    public class CreateOrderRequestDto
    {
        public decimal Amount { get; set; }
    }
    
    public class CreateOrderResponseDto
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
    }
}
