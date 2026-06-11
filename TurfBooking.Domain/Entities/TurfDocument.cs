using Domain.Common;

namespace Domain.Entities;

public class TurfDocument : BaseEntity
{
    public int TurfId { get; set; }
    public Turf? Turf { get; set; }

    public string DocumentType { get; set; } = string.Empty; // e.g. Business Registration, Property Proof, Electricity Bill, Owner ID, etc.
    public string FileUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
