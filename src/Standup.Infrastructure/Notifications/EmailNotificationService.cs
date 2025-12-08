using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Infrastructure.Notifications;

public class EmailNotificationService : INotificationService
{
    private readonly GraphServiceClient _graphClient;

    public EmailNotificationService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<bool> SendStandupReportAsync(
        User user,
        StandupReport report,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.Email))
            return false;

        try
        {
            var message = new Message
            {
                Subject = $"Standup Update - {report.PeriodEnd:MMMM dd, yyyy}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = FormatAsEmail(report)
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = user.Email,
                            Name = user.DisplayName
                        }
                    }
                }
            };

            var requestBody = new SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = false
            };

            await _graphClient.Me.SendMail.PostAsync(requestBody, cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> SendToChannelAsync(
        TeamSubscription subscription,
        StandupReport report,
        CancellationToken cancellationToken = default)
    {
        // Email doesn't support channel posting
        return Task.FromResult(false);
    }

    private static string FormatAsEmail(StandupReport report)
    {
        var period = $"{report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; }}
        .summary {{ background: white; padding: 15px; border-radius: 8px; margin: 15px 0; white-space: pre-wrap; }}
        .stats {{ display: flex; gap: 20px; margin-top: 15px; }}
        .stat {{ background: white; padding: 10px 15px; border-radius: 8px; text-align: center; }}
        .stat-value {{ font-size: 24px; font-weight: bold; color: #667eea; }}
        .stat-label {{ font-size: 12px; color: #666; }}
        .footer {{ text-align: center; padding: 15px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>📋 Standup Update</h1>
            <p style='margin: 5px 0 0 0; opacity: 0.9;'>{period}</p>
        </div>
        <div class='content'>
            <div class='summary'>
                {report.Summary}
            </div>
            <div class='stats'>
                <div class='stat'>
                    <div class='stat-value'>{report.RawData.Commits.Count}</div>
                    <div class='stat-label'>Commits</div>
                </div>
                <div class='stat'>
                    <div class='stat-value'>{report.RawData.PullRequests.Count}</div>
                    <div class='stat-label'>Pull Requests</div>
                </div>
                <div class='stat'>
                    <div class='stat-value'>{report.RawData.WorkItems.Count}</div>
                    <div class='stat-label'>Work Items</div>
                </div>
            </div>
        </div>
        <div class='footer'>
            Generated at {report.GeneratedAt:HH:mm} UTC by Standup Automation
        </div>
    </div>
</body>
</html>";
    }
}
