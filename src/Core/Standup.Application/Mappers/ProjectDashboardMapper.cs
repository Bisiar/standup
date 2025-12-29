// <copyright file="ProjectDashboardMapper.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using Standup.Application.Models;
using Standup.Domain.Entities;
using Standup.Domain.ValueObjects;

namespace Standup.Application.Mappers;

/// <summary>
/// Maps domain data to Project Dashboard UI models.
/// </summary>
public static class ProjectDashboardMapper
{
    /// <summary>
    /// Gets initials from a name.
    /// </summary>
    /// <param name="name">The full name.</param>
    /// <returns>Two-letter initials.</returns>
    public static string GetInitials(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "??";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
        }

        return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
    }

    /// <summary>
    /// Gets a relative time string from a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>A human-readable relative time.</returns>
    public static string GetRelativeTime(DateTimeOffset timestamp)
    {
        var diff = DateTimeOffset.UtcNow - timestamp;

        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}m ago";
        }

        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}h ago";
        }

        if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}d ago";
        }

        return timestamp.ToString("MMM dd");
    }

    /// <summary>
    /// Maps work item tags to a task priority.
    /// </summary>
    /// <param name="tags">The tags list.</param>
    /// <returns>The mapped priority.</returns>
    public static TaskPriority MapPriority(List<string>? tags)
    {
        if (tags == null)
        {
            return TaskPriority.Medium;
        }

        if (tags.Any(t => t.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                         t.Contains("critical", StringComparison.OrdinalIgnoreCase)))
        {
            return TaskPriority.High;
        }

        if (tags.Any(t => t.Contains("low", StringComparison.OrdinalIgnoreCase)))
        {
            return TaskPriority.Low;
        }

        return TaskPriority.Medium;
    }

    /// <summary>
    /// Maps commits, PRs, and completed items to recent activity.
    /// </summary>
    /// <param name="commits">The commits.</param>
    /// <param name="prs">The pull requests.</param>
    /// <param name="completed">The completed work items.</param>
    /// <returns>List of project activities.</returns>
    public static List<ProjectActivity> MapRecentActivity(
        List<CommitInfo> commits,
        List<PullRequestInfo> prs,
        List<WorkItemInfo> completed)
    {
        var activities = new List<ProjectActivity>();

        foreach (var commit in commits.OrderByDescending(c => c.CommittedAt).Take(5))
        {
            activities.Add(new ProjectActivity(
                ActivityType.Commit,
                commit.Author ?? "Unknown",
                commit.Subject,
                GetRelativeTime(commit.CommittedAt)));
        }

        foreach (var item in completed.Take(3))
        {
            activities.Add(new ProjectActivity(
                ActivityType.Task,
                item.AssignedTo ?? "Unknown",
                $"completed {item.Title}",
                "Recently"));
        }

        foreach (var pr in prs.OrderByDescending(p => p.CreatedAt).Take(2))
        {
            activities.Add(new ProjectActivity(
                ActivityType.PullRequest,
                "Developer",
                $"opened PR #{pr.Id}: {pr.Title}",
                GetRelativeTime(pr.CreatedAt)));
        }

        return activities;
    }

    /// <summary>
    /// Maps work items to in-progress tasks.
    /// </summary>
    /// <param name="inProgress">The in-progress work items.</param>
    /// <returns>List of project tasks.</returns>
    public static List<ProjectTask> MapInProgressTasks(List<WorkItemInfo> inProgress)
    {
        return inProgress.Take(5).Select(item => new ProjectTask(
            item.Id,
            item.Title,
            GetInitials(item.AssignedTo),
            item.AssignedTo ?? "Unassigned",
            MapPriority(item.Tags),
            DateTimeOffset.UtcNow.AddDays(7))).ToList();
    }

    /// <summary>
    /// Maps pull requests to in-review tasks.
    /// </summary>
    /// <param name="prs">The pull requests.</param>
    /// <returns>List of project tasks.</returns>
    public static List<ProjectTask> MapInReviewTasks(List<PullRequestInfo> prs)
    {
        return prs.Take(5).Select(pr => new ProjectTask(
            $"PR-{pr.Id}",
            pr.Title,
            "PR",
            "Reviewer",
            pr.IsDraft ? TaskPriority.Low : TaskPriority.Medium,
            pr.CreatedAt.AddDays(3))).ToList();
    }

    /// <summary>
    /// Maps commits to team members based on author activity.
    /// </summary>
    /// <param name="commits">The commits.</param>
    /// <returns>List of team members.</returns>
    public static List<TeamMember> MapTeamMembers(List<CommitInfo> commits)
    {
        var members = commits
            .Where(c => !string.IsNullOrEmpty(c.Author))
            .GroupBy(c => c.Author!)
            .Select(g => new TeamMember(
                g.Key,
                GetInitials(g.Key),
                "Developer",
                TeamRole.Developer,
                g.Count(),
                MemberStatus.Active))
            .OrderByDescending(m => m.TaskCount)
            .Take(5)
            .ToList();

        if (members.Count == 0)
        {
            members.Add(new TeamMember(
                "No recent contributors",
                "—",
                "Connect a data source to see team activity",
                TeamRole.Developer,
                0,
                MemberStatus.Active));
        }

        return members;
    }
}
