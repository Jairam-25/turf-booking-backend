using Domain.Common;

namespace Domain.Entities;

public class TurfImage : BaseEntity
{
    public int TurfId { get; set; }
    public Turf? Turf { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
