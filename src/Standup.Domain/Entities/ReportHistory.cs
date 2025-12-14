using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

/// <summary>
/// Represents a saved standup report for historical reference.
/// </summary>
public class ReportHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GroupId { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public SummaryType SummaryType { get; set; }
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int TotalWorkItems { get; set; }

    /// <summary>
    /// The full markdown content of the report.
    /// </summary>
    public string ReportContent { get; set; } = string.Empty;

    /// <summary>
    /// Client codes included in this report.
    /// </summary>
    public List<string> ClientCodes { get; set; } = new();

    /// <summary>
    /// Display string for the report (e.g., "Dec 7 - Dec 14: 20 commits, 2 PRs")
    /// </summary>
    public string DisplaySummary => $"{PeriodStart:MMM d} - {PeriodEnd:MMM d}: {TotalCommits} commits, {TotalPullRequests} PRs";
}
