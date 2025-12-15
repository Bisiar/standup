using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Abstraction for notification delivery services.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Gets the notification channel this service handles.
    /// </summary>
    NotificationChannel Channel { get; }

    /// <summary>
    /// Sends a standup report via this notification channel.
    /// </summary>
    /// <param name="user">The user to send the report to.</param>
    /// <param name="report">The standup report to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the report was sent successfully, false otherwise.</returns>
    Task<bool> SendStandupReportAsync(
        User user,
        StandupReport report,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a standup report to a specific channel.
    /// </summary>
    /// <param name="subscription">The team subscription defining the target channel.</param>
    /// <param name="report">The standup report to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the report was sent successfully, false otherwise.</returns>
    Task<bool> SendToChannelAsync(
        TeamSubscription subscription,
        StandupReport report,
        CancellationToken cancellationToken = default);
}
