using Domain.Common;

namespace Domain.Entities;

public class Owner : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Pending Verification"; // Pending Verification, Under Review, Approved, Rejected
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OwnerPayment> Payments { get; set; } = new List<OwnerPayment>();
    public ICollection<Turf> Turfs { get; set; } = new List<Turf>();
}
