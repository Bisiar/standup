// <copyright file="ProjectActivity.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Represents a project activity item.
/// </summary>
public record ProjectActivity(ActivityType Type, string Author, string Description, string TimeAgo)
{
    /// <summary>
    /// Gets the activity icon.
    /// </summary>
    public string Icon => Type switch
    {
        ActivityType.Commit => "📝",
        ActivityType.Task => "✅",
        ActivityType.PullRequest => "🔀",
        ActivityType.Deployment => "🚀",
        _ => "📋",
    };

    /// <summary>
    /// Gets the activity background color.
    /// </summary>
    public string BackgroundColor => Type switch
    {
        ActivityType.Commit => "#1E3A5F",
        ActivityType.Task => "#14532D",
        ActivityType.PullRequest => "#4C1D95",
        ActivityType.Deployment => "#7F1D1D",
        _ => "#334155",
    };

    /// <summary>
    /// Gets the formatted text with author highlighted.
    /// </summary>
    public string FormattedText => string.IsNullOrEmpty(Author) ? Description : $"{Author} {Description}";
}
