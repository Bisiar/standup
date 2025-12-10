using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

public class StandupReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public StandupData RawData { get; set; } = new();
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<NotificationChannel> SentTo { get; set; } = new();

    public User? User { get; set; }
}
