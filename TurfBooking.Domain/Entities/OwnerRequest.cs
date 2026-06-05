using Domain.Common;

namespace Domain.Entities;

public class OwnerRequest : BaseEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int TurfId { get; set; }
    public Turf? Turf { get; set; }

    public string BusinessName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string ProofDocumentUrl { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
}
