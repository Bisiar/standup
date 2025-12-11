using Microsoft.Graph;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Graph = Microsoft.Graph.Models;

namespace Standup.Infrastructure.Notifications;

public class TeamsChannelNotificationService : INotificationService
{
    private readonly GraphServiceClient _graphClient;

    public TeamsChannelNotificationService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public NotificationChannel Channel => NotificationChannel.TeamsChannel;

    public async Task<bool> SendStandupReportAsync(
        User user,
        StandupReport report,
        CancellationToken cancellationToken = default)
    {
        // For channel posting, we need to go through subscriptions
        // This method sends to all active subscriptions for the user
        var sent = false;

        foreach (var subscription in user.Subscriptions.Where(s => s.IsActive && s.AutoPost))
        {
            var result = await SendToChannelAsync(subscription, report, cancellationToken);
            sent = sent || result;
        }

        return sent;
    }

    public async Task<bool> SendToChannelAsync(
        TeamSubscription subscription,
        StandupReport report,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Graph.ChatMessage
            {
                Body = new Graph.ItemBody
                {
                    ContentType = Graph.BodyType.Html,
                    Content = FormatAsChannelPost(report)
                }
            };

            await _graphClient
                .Teams[subscription.TeamId]
                .Channels[subscription.ChannelId]
                .Messages
                .PostAsync(message, cancellationToken: cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatAsChannelPost(StandupReport report)
    {
        var period = $"{report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}";

        return $@"
<div style='font-family: Segoe UI, sans-serif;'>
    <h3>📋 Standup Update ({period})</h3>
    <div style='white-space: pre-wrap;'>{report.Summary}</div>
    <hr/>
    <small style='color: #666;'>
        Generated at {report.GeneratedAt:HH:mm} UTC |
        📊 {report.RawData.Commits.Count} commits |
        🔀 {report.RawData.PullRequests.Count} PRs |
        📝 {report.RawData.WorkItems.Count} work items
    </small>
</div>";
    }
}
