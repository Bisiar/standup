namespace Standup.Domain.Entities;

/// <summary>
/// Represents a user's subscription to post standups to a Teams channel
/// </summary>
public class TeamSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Teams team ID
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// Teams channel ID
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the channel
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to auto-post standups to this channel
    /// </summary>
    public bool AutoPost { get; set; } = true;

    /// <summary>
    /// Whether this subscription is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }
}
