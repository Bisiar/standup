namespace Standup.Domain.Entities;

/// <summary>
/// Represents a standup meeting schedule for a tenant
/// </summary>
public class StandupSchedule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "Morning Standup", "Team Sync")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cron expression for schedule (e.g., "0 8 * * 1-5" for 8 AM Mon-Fri)
    /// </summary>
    public string CronExpression { get; set; } = "0 8 * * 1-5";

    /// <summary>
    /// Time zone for the schedule
    /// </summary>
    public string TimeZone { get; set; } = "America/Denver";

    /// <summary>
    /// Minutes before standup to send notifications
    /// </summary>
    public int NotifyMinutesBefore { get; set; } = 15;

    /// <summary>
    /// Teams channel to post standups to (if configured)
    /// </summary>
    public string? TeamsChannelId { get; set; }

    /// <summary>
    /// Whether this is an async standup (no meeting, just posts)
    /// </summary>
    public bool IsAsync { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
