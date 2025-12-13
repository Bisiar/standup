using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

/// <summary>
/// Tracks generated reports per client/group for smart date defaults.
/// </summary>
public class ReportHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClientCode { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public SummaryType SummaryType { get; set; }
    public string Summary { get; set; } = string.Empty;
}
