// <copyright file="ActivityItem.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

namespace Standup.Application.Models;

/// <summary>
/// Represents a single activity item for the recent activity feed.
/// </summary>
public record ActivityItem(
    string Description,
    string TimeAgo,
    ActivityType Type,
    DateTimeOffset Timestamp);

/// <summary>
/// Types of activity items.
/// </summary>
public enum ActivityType
{
    /// <summary>Commit activity.</summary>
    Commit,

    /// <summary>Pull request activity.</summary>
    PullRequest,

    /// <summary>Task/work item activity.</summary>
    Task,

    /// <summary>Deployment activity.</summary>
    Deployment,
}
