using AdaptiveCards;
using Microsoft.Bot.Schema;
using Standup.Teams.Models;

namespace Standup.Teams.Services;

public sealed class CardService : ICardService
{
    public Attachment CreateWelcomeCard()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = "Welcome to Standup Bot!",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = "I help you generate daily standup reports by aggregating your commits, pull requests, and work items from GitHub and Azure DevOps.",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
                },
                new AdaptiveTextBlock
                {
                    Text = "**Getting Started:**",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Large
                },
                new AdaptiveTextBlock
                {
                    Text = "1. Configure your repositories in the MAUI app or web portal\n2. Say **standup** to generate your daily report\n3. Say **subscribe** to get daily reminders",
                    Wrap = true
                }
            ],
            Actions =
            [
                new AdaptiveSubmitAction
                {
                    Title = "Generate Standup",
                    Data = new { action = "generateStandup" }
                },
                new AdaptiveSubmitAction
                {
                    Title = "Subscribe to Reminders",
                    Data = new { action = "subscribe" }
                },
                new AdaptiveOpenUrlAction
                {
                    Title = "Open Settings",
                    Url = new Uri("https://standup.journeyteam.com/settings")
                }
            ]
        };

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }

    public Attachment CreateStandupCard(StandupResponse standup)
    {
        var bodyItems = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = $"Standup for {standup.Date:dddd, MMMM d}",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true
            }
        };

        // Summary section
        if (!string.IsNullOrWhiteSpace(standup.Summary))
        {
            bodyItems.Add(new AdaptiveTextBlock
            {
                Text = standup.Summary,
                Wrap = true,
                Spacing = AdaptiveSpacing.Medium
            });
        }

        // Commits section
        if (standup.Commits.Count > 0)
        {
            bodyItems.Add(new AdaptiveTextBlock
            {
                Text = $"**Commits ({standup.Commits.Count})**",
                Wrap = true,
                Spacing = AdaptiveSpacing.Large
            });

            foreach (var commit in standup.Commits.Take(5))
            {
                bodyItems.Add(new AdaptiveTextBlock
                {
                    Text = $"- {commit.Message} ({commit.Repository})",
                    Wrap = true,
                    Size = AdaptiveTextSize.Small
                });
            }

            if (standup.Commits.Count > 5)
            {
                bodyItems.Add(new AdaptiveTextBlock
                {
                    Text = $"_...and {standup.Commits.Count - 5} more commits_",
                    Wrap = true,
                    Size = AdaptiveTextSize.Small,
                    IsSubtle = true
                });
            }
        }

        // Pull Requests section
        if (standup.PullRequests.Count > 0)
        {
            bodyItems.Add(new AdaptiveTextBlock
            {
                Text = $"**Pull Requests ({standup.PullRequests.Count})**",
                Wrap = true,
                Spacing = AdaptiveSpacing.Large
            });

            foreach (var pr in standup.PullRequests.Take(5))
            {
                var statusIcon = pr.Status switch
                {
                    "merged" => "✅",
                    "open" => "🔵",
                    "closed" => "❌",
                    _ => "⚪"
                };

                bodyItems.Add(new AdaptiveTextBlock
                {
                    Text = $"{statusIcon} {pr.Title} ({pr.Repository})",
                    Wrap = true,
                    Size = AdaptiveTextSize.Small
                });
            }
        }

        // Work Items section
        if (standup.WorkItems.Count > 0)
        {
            bodyItems.Add(new AdaptiveTextBlock
            {
                Text = $"**Work Items ({standup.WorkItems.Count})**",
                Wrap = true,
                Spacing = AdaptiveSpacing.Large
            });

            foreach (var workItem in standup.WorkItems.Take(5))
            {
                var typeIcon = workItem.Type switch
                {
                    "Bug" => "🐛",
                    "Task" => "📋",
                    "User Story" => "📖",
                    "Feature" => "⭐",
                    _ => "📌"
                };

                bodyItems.Add(new AdaptiveTextBlock
                {
                    Text = $"{typeIcon} {workItem.Title} ({workItem.Status})",
                    Wrap = true,
                    Size = AdaptiveTextSize.Small
                });
            }
        }

        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = bodyItems,
            Actions =
            [
                new AdaptiveSubmitAction
                {
                    Title = "Refresh",
                    Data = new { action = "generateStandup" }
                },
                new AdaptiveSubmitAction
                {
                    Title = "Copy",
                    Data = new { action = "copyToClipboard", content = standup.Summary }
                },
                new AdaptiveShowCardAction
                {
                    Title = "Share",
                    Card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
                    {
                        Body =
                        [
                            new AdaptiveTextInput
                            {
                                Id = "channelId",
                                Placeholder = "Enter channel ID to share",
                                Label = "Channel"
                            }
                        ],
                        Actions =
                        [
                            new AdaptiveSubmitAction
                            {
                                Title = "Share to Channel",
                                Data = new { action = "shareToChannel" }
                            }
                        ]
                    }
                }
            ]
        };

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }

    public Attachment CreateHelpCard()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = "Standup Bot Help",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = "Here are the commands I understand:",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
                },
                new AdaptiveFactSet
                {
                    Facts =
                    [
                        new AdaptiveFact("standup", "Generate your daily standup report"),
                        new AdaptiveFact("subscribe", "Get daily standup reminders"),
                        new AdaptiveFact("unsubscribe", "Stop receiving reminders"),
                        new AdaptiveFact("settings", "View and update your preferences"),
                        new AdaptiveFact("help", "Show this help message")
                    ],
                    Spacing = AdaptiveSpacing.Medium
                },
                new AdaptiveTextBlock
                {
                    Text = "**Tip:** You can also mention me in channels to generate standups for the team!",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Large,
                    IsSubtle = true
                }
            ]
        };

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }

    public Attachment CreateSettingsCard()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = "Settings",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = "Configure your standup preferences:",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
                },
                new AdaptiveChoiceSetInput
                {
                    Id = "tone",
                    Label = "Summary Tone",
                    Value = "professional",
                    Choices =
                    [
                        new AdaptiveChoice { Title = "Professional", Value = "professional" },
                        new AdaptiveChoice { Title = "Casual", Value = "casual" },
                        new AdaptiveChoice { Title = "Detailed", Value = "detailed" },
                        new AdaptiveChoice { Title = "Brief", Value = "brief" }
                    ]
                },
                new AdaptiveChoiceSetInput
                {
                    Id = "reminderTime",
                    Label = "Reminder Time",
                    Value = "09:00",
                    Choices =
                    [
                        new AdaptiveChoice { Title = "8:00 AM", Value = "08:00" },
                        new AdaptiveChoice { Title = "9:00 AM", Value = "09:00" },
                        new AdaptiveChoice { Title = "9:30 AM", Value = "09:30" },
                        new AdaptiveChoice { Title = "10:00 AM", Value = "10:00" }
                    ]
                },
                new AdaptiveToggleInput
                {
                    Id = "includeWorkItems",
                    Title = "Include work items in standup",
                    Value = "true"
                },
                new AdaptiveToggleInput
                {
                    Id = "includePRs",
                    Title = "Include pull requests in standup",
                    Value = "true"
                }
            ],
            Actions =
            [
                new AdaptiveSubmitAction
                {
                    Title = "Save Settings",
                    Data = new { action = "saveSettings" }
                },
                new AdaptiveOpenUrlAction
                {
                    Title = "Open Full Settings",
                    Url = new Uri("https://standup.journeyteam.com/settings")
                }
            ]
        };

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }

    public Attachment CreateReminderCard()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = "Time for your standup!",
                    Size = AdaptiveTextSize.Large,
                    Weight = AdaptiveTextWeight.Bolder,
                    Wrap = true
                },
                new AdaptiveTextBlock
                {
                    Text = "Good morning! Ready to generate your standup report for today?",
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
                }
            ],
            Actions =
            [
                new AdaptiveSubmitAction
                {
                    Title = "Generate Standup",
                    Data = new { action = "generateStandup" },
                    Style = "positive"
                },
                new AdaptiveSubmitAction
                {
                    Title = "Snooze 30 min",
                    Data = new { action = "snooze", minutes = 30 }
                },
                new AdaptiveSubmitAction
                {
                    Title = "Skip Today",
                    Data = new { action = "skipToday" }
                }
            ]
        };

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }
}
