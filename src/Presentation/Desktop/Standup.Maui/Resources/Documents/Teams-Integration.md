# Teams Integration

The Standup platform integrates with Microsoft Teams for notifications and interactive standup management.

## Overview

Teams integration provides:
- **Direct Messages**: Personal standup reminders
- **Channel Posts**: Async standup updates to team channels
- **Teams App**: Interactive bot and configuration tab
- **Subscriptions**: Team members can subscribe to channels

## Setting Up Teams Notifications

### Prerequisites

- Microsoft 365 account
- Teams admin approval (for org-wide deployment)
- API deployed with Graph permissions

### Required Graph Permissions

| Permission | Type | Purpose |
|------------|------|---------|
| `Chat.ReadWrite` | Delegated | Send DMs |
| `ChannelMessage.Send` | Delegated | Post to channels |
| `Team.ReadBasic.All` | Delegated | List teams/channels |
| `User.Read` | Delegated | Get user info |

## Notification Types

### Direct Messages

Personal standup summaries sent to your Teams chat:

```
📋 Standup Update - Dec 7 - Dec 8, 2024

Yesterday, I completed work on the authentication
module, fixing the token refresh issue and adding
retry logic for failed requests.

Today, I'm continuing with the user profile
feature and reviewing PRs from the team.

No blockers currently.

────────────────────
📊 5 commits | 🔀 2 PRs | 📝 3 work items
```

### Channel Posts

Posted to your team's standup channel for async standups:

```
📋 Standup Update (Dec 7 - Dec 8, 2024)

[Same content as DM]

Generated at 08:00 UTC | 📊 5 commits | 🔀 2 PRs | 📝 3 work items
```

## Teams App

### Installation

1. Get the app package from your Teams admin
2. Or side-load during development

### Bot Commands

| Command | Description |
|---------|-------------|
| `standup` | Generate your standup |
| `preview` | Preview without posting |
| `subscribe` | Subscribe to channel updates |
| `unsubscribe` | Stop channel updates |
| `help` | Show available commands |

### Configuration Tab

Add the Standup tab to your channel:

1. Click **+** to add a tab
2. Search for "Standup"
3. Configure:
   - Team members to include
   - Posting schedule
   - AI summarization preferences

## Subscriptions

### Channel Subscriptions

Team members can subscribe to have their standups auto-posted:

```
/standup subscribe

✅ You're now subscribed to post standups in #engineering
Your standups will be posted automatically at 8:00 AM MST
```

### Managing Subscriptions

```
/standup subscriptions

Your active subscriptions:
• #engineering - Auto-post enabled
• #frontend-team - Auto-post disabled

Use /standup unsubscribe [channel] to remove
```

## Scheduling

### Automatic Posts

Configure automatic standup posts:

1. **Sync with Calendar**: Check for standup meeting
2. **Async Days**: Post to channel when no meeting scheduled
3. **Time-based**: Post at configured time

### Calendar Integration

The platform checks Google Calendar/Outlook for standup meetings:

- If meeting found → Send DM reminder only
- If no meeting → Post to subscribed channels (async)

## Adaptive Cards

Standup posts use Adaptive Cards for rich formatting:

```json
{
  "type": "AdaptiveCard",
  "body": [
    {
      "type": "TextBlock",
      "text": "📋 Standup Update",
      "weight": "Bolder",
      "size": "Large"
    },
    {
      "type": "TextBlock",
      "text": "{{summary}}",
      "wrap": true
    },
    {
      "type": "ColumnSet",
      "columns": [
        {
          "type": "Column",
          "items": [{"type": "TextBlock", "text": "{{commits}} commits"}]
        }
      ]
    }
  ],
  "actions": [
    {
      "type": "Action.OpenUrl",
      "title": "View Details",
      "url": "{{detailsUrl}}"
    }
  ]
}
```

## Troubleshooting

### Messages not sending

- Verify Graph permissions are consented
- Check Teams user ID is configured
- Ensure channel subscription is active

### Bot not responding

- Verify bot is properly registered in Azure
- Check Teams app is installed
- Review bot service logs
