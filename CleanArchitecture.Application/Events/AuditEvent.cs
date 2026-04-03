namespace CleanArchitecture.Application.Events;

public class AuditEvent
{
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}