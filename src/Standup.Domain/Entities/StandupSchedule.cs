namespace Standup.Domain.Entities;

/// <summary>
/// Represents a standup meeting schedule for a tenant.
/// </summary>
public class StandupSchedule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name (e.g., "Morning Standup", "Team Sync").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cron expression for schedule (e.g., "0 8 * * 1-5" for 8 AM Mon-Fri).
    /// </summary>
    public string CronExpression { get; set; } = "0 8 * * 1-5";

    /// <summary>
    /// Gets or sets the time zone for the schedule.
    /// </summary>
    public string TimeZone { get; set; } = "America/Denver";

    /// <summary>
    /// Gets or sets the minutes before standup to send notifications.
    /// </summary>
    public int NotifyMinutesBefore { get; set; } = 15;

    /// <summary>
    /// Gets or sets the Teams channel to post standups to (if configured).
    /// </summary>
    public string? TeamsChannelId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an async standup (no meeting, just posts).
    /// </summary>
    public bool IsAsync { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
