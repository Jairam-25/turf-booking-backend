using Domain.Common;

namespace Domain.Entities;

public class OwnerPayment : BaseEntity
{
    public int OwnerId { get; set; }
    public Owner? Owner { get; set; }

    public string RazorpayPaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Success, Failed
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
