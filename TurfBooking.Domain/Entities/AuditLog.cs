using Domain.Common;

namespace Domain.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Details { get; set; } = string.Empty;
}
