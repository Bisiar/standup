using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Abstraction for notification delivery services
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// The notification channel this service handles
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Sends a standup report via this notification channel
    /// </summary>
    Task<bool> SendStandupReportAsync(
        User user,
        StandupReport report,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a standup report to a specific channel
    /// </summary>
    Task<bool> SendToChannelAsync(
        TeamSubscription subscription,
        StandupReport report,
        CancellationToken cancellationToken = default);
}
