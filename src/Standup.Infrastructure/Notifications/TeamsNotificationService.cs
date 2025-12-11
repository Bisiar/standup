using Microsoft.Graph;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Graph = Microsoft.Graph.Models;

namespace Standup.Infrastructure.Notifications;

public class TeamsNotificationService : INotificationService
{
    private readonly GraphServiceClient _graphClient;

    public TeamsNotificationService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public NotificationChannel Channel => NotificationChannel.TeamsDirectMessage;

    public async Task<bool> SendStandupReportAsync(
        User user,
        StandupReport report,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.TeamsUserId))
            return false;

        try
        {
            var chat = await GetOrCreateChatAsync(user.TeamsUserId, cancellationToken);
            if (chat == null)
                return false;

            var message = new Graph.ChatMessage
            {
                Body = new Graph.ItemBody
                {
                    ContentType = Graph.BodyType.Html,
                    Content = FormatAsHtml(report)
                }
            };

            await _graphClient.Chats[chat.Id].Messages.PostAsync(message, cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
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
                    Content = FormatAsHtml(report)
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

    private async Task<Graph.Chat?> GetOrCreateChatAsync(string teamsUserId, CancellationToken cancellationToken)
    {
        try
        {
            var chats = await _graphClient.Me.Chats.GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Filter = $"chatType eq 'oneOnOne'";
            }, cancellationToken: cancellationToken);

            var existingChat = chats?.Value?.FirstOrDefault(c =>
                c.Members?.Any(m => m.Id == teamsUserId) == true);

            if (existingChat != null)
                return existingChat;

            var newChat = new Graph.Chat
            {
                ChatType = Graph.ChatType.OneOnOne,
                Members = new List<Graph.ConversationMember>
                {
                    new Graph.AadUserConversationMember
                    {
                        Roles = new List<string> { "owner" },
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users/{teamsUserId}" }
                        }
                    }
                }
            };

            return await _graphClient.Chats.PostAsync(newChat, cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string FormatAsHtml(StandupReport report)
    {
        var period = $"{report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}";

        return $@"
<div style='font-family: Segoe UI, sans-serif;'>
    <h3>📋 Standup Update - {period}</h3>
    <div style='white-space: pre-wrap;'>{report.Summary}</div>
    <hr/>
    <small style='color: #666;'>
        📊 {report.RawData.Commits.Count} commits |
        🔀 {report.RawData.PullRequests.Count} PRs |
        📝 {report.RawData.WorkItems.Count} work items
    </small>
</div>";
    }
}
