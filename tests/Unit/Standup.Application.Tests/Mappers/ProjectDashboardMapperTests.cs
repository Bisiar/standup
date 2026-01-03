// <copyright file="ProjectDashboardMapperTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Mappers;
using Standup.Application.Models;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Mappers;

/// <summary>
/// Tests for the <see cref="ProjectDashboardMapper"/> static class.
/// </summary>
public sealed class ProjectDashboardMapperTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDashboardMapperTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ProjectDashboardMapperTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region GetInitials Tests

    [Fact]
    public void GetInitials_WithTwoWordName_ReturnsTwoLetters()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials("John Doe");

        // Assert
        result.Should().Be("JD");
        _output.WriteLine($"Initials for 'John Doe': {result}");
    }

    [Fact]
    public void GetInitials_WithThreeWordName_ReturnsFirstTwoLetters()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials("John Michael Doe");

        // Assert
        result.Should().Be("JM");
    }

    [Fact]
    public void GetInitials_WithSingleName_ReturnsFirstTwoChars()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials("John");

        // Assert
        result.Should().Be("JO");
    }

    [Fact]
    public void GetInitials_WithSingleChar_ReturnsUppercaseChar()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials("j");

        // Assert
        result.Should().Be("J");
    }

    [Fact]
    public void GetInitials_WithNull_ReturnsQuestionMarks()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials(null);

        // Assert
        result.Should().Be("??");
    }

    [Fact]
    public void GetInitials_WithEmptyString_ReturnsQuestionMarks()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials(string.Empty);

        // Assert
        result.Should().Be("??");
    }

    [Fact]
    public void GetInitials_WithLowercaseName_ReturnsUppercase()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials("john doe");

        // Assert
        result.Should().Be("JD");
    }

    [Fact]
    public void GetInitials_WithExtraSpaces_IgnoresEmptyParts()
    {
        // Act
        var result = ProjectDashboardMapper.GetInitials("John  Doe");

        // Assert
        result.Should().Be("JD");
    }

    #endregion

    #region GetRelativeTime Tests

    [Fact]
    public void GetRelativeTime_WithinMinutes_ReturnsMinutesAgo()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-30);

        // Act
        var result = ProjectDashboardMapper.GetRelativeTime(timestamp);

        // Assert
        result.Should().Be("30m ago");
        _output.WriteLine($"Relative time: {result}");
    }

    [Fact]
    public void GetRelativeTime_WithinHours_ReturnsHoursAgo()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddHours(-5);

        // Act
        var result = ProjectDashboardMapper.GetRelativeTime(timestamp);

        // Assert
        result.Should().Be("5h ago");
    }

    [Fact]
    public void GetRelativeTime_WithinWeek_ReturnsDaysAgo()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddDays(-3);

        // Act
        var result = ProjectDashboardMapper.GetRelativeTime(timestamp);

        // Assert
        result.Should().Be("3d ago");
    }

    [Fact]
    public void GetRelativeTime_OverWeek_ReturnsFormattedDate()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 12, 15, 10, 0, 0, TimeSpan.Zero);

        // Act
        var result = ProjectDashboardMapper.GetRelativeTime(timestamp);

        // Assert
        result.Should().Be("Dec 15");
        _output.WriteLine($"Formatted date: {result}");
    }

    [Fact]
    public void GetRelativeTime_JustNow_ReturnsZeroMinutes()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var result = ProjectDashboardMapper.GetRelativeTime(timestamp);

        // Assert
        result.Should().Be("0m ago");
    }

    #endregion

    #region MapPriority Tests

    [Fact]
    public void MapPriority_WithNull_ReturnsMedium()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(null);

        // Assert
        result.Should().Be(TaskPriority.Medium);
    }

    [Fact]
    public void MapPriority_WithEmptyList_ReturnsMedium()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(new List<string>());

        // Assert
        result.Should().Be(TaskPriority.Medium);
    }

    [Fact]
    public void MapPriority_WithHighTag_ReturnsHigh()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(new List<string> { "high", "feature" });

        // Assert
        result.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void MapPriority_WithHighTagCaseInsensitive_ReturnsHigh()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(new List<string> { "HIGH-PRIORITY" });

        // Assert
        result.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void MapPriority_WithCriticalTag_ReturnsHigh()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(new List<string> { "critical-bug" });

        // Assert
        result.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void MapPriority_WithLowTag_ReturnsLow()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(new List<string> { "low-priority", "enhancement" });

        // Assert
        result.Should().Be(TaskPriority.Low);
    }

    [Fact]
    public void MapPriority_WithNoMatchingTags_ReturnsMedium()
    {
        // Act
        var result = ProjectDashboardMapper.MapPriority(new List<string> { "bug", "feature", "documentation" });

        // Assert
        result.Should().Be(TaskPriority.Medium);
    }

    #endregion

    #region MapRecentActivity Tests

    [Fact]
    public void MapRecentActivity_WithCommits_MapsToActivities()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo(
                Sha: "abc123",
                Message: "Fixed login bug",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow.AddHours(-1),
                Author: "John Doe")
        };

        // Act
        var result = ProjectDashboardMapper.MapRecentActivity(commits, new List<PullRequestInfo>(), new List<WorkItemInfo>());

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ActivityType.Commit);
        result[0].Author.Should().Be("John Doe");
        result[0].Description.Should().Be("Fixed login bug");
        _output.WriteLine($"Mapped activity: {result[0].Description}");
    }

    [Fact]
    public void MapRecentActivity_WithNullAuthor_UsesUnknown()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo(
                Sha: "abc123",
                Message: "Anonymous commit",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow,
                Author: null)
        };

        // Act
        var result = ProjectDashboardMapper.MapRecentActivity(commits, new List<PullRequestInfo>(), new List<WorkItemInfo>());

        // Assert
        result[0].Author.Should().Be("Unknown");
    }

    [Fact]
    public void MapRecentActivity_WithCompletedWorkItems_MapsToTaskActivity()
    {
        // Arrange
        var completed = new List<WorkItemInfo>
        {
            new WorkItemInfo(
                Id: "123",
                Title: "Task 123",
                Type: "Task",
                Status: WorkItemStatus.Resolved,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com",
                AssignedTo: "Jane Smith")
        };

        // Act
        var result = ProjectDashboardMapper.MapRecentActivity(new List<CommitInfo>(), new List<PullRequestInfo>(), completed);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ActivityType.Task);
        result[0].Description.Should().Contain("completed Task 123");
    }

    [Fact]
    public void MapRecentActivity_WithPullRequests_MapsToPRActivity()
    {
        // Arrange
        var prs = new List<PullRequestInfo>
        {
            new PullRequestInfo(
                Id: "42",
                Title: "Feature PR",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Active",
                Url: "https://dev.azure.com",
                CreatedAt: DateTimeOffset.UtcNow)
        };

        // Act
        var result = ProjectDashboardMapper.MapRecentActivity(new List<CommitInfo>(), prs, new List<WorkItemInfo>());

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ActivityType.PullRequest);
        result[0].Description.Should().Contain("PR #42");
        result[0].Description.Should().Contain("Feature PR");
    }

    [Fact]
    public void MapRecentActivity_LimitsCommitsToFive()
    {
        // Arrange
        var commits = Enumerable.Range(1, 10)
            .Select(i => new CommitInfo(
                Sha: $"sha-{i}",
                Message: $"Commit {i}",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow.AddMinutes(-i),
                Author: "Author"))
            .ToList();

        // Act
        var result = ProjectDashboardMapper.MapRecentActivity(commits, new List<PullRequestInfo>(), new List<WorkItemInfo>());

        // Assert
        result.Count(a => a.Type == ActivityType.Commit).Should().Be(5);
    }

    #endregion

    #region MapInProgressTasks Tests

    [Fact]
    public void MapInProgressTasks_MapsWorkItemsToTasks()
    {
        // Arrange
        var workItems = new List<WorkItemInfo>
        {
            new WorkItemInfo(
                Id: "123",
                Title: "Implement login",
                Type: "Task",
                Status: WorkItemStatus.Active,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com",
                AssignedTo: "John Doe",
                Tags: new List<string> { "high" })
        };

        // Act
        var result = ProjectDashboardMapper.MapInProgressTasks(workItems);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("123");
        result[0].Title.Should().Be("Implement login");
        result[0].AssigneeInitials.Should().Be("JD");
        result[0].AssigneeName.Should().Be("John Doe");
        result[0].Priority.Should().Be(TaskPriority.High);
        _output.WriteLine($"Mapped task: {result[0].Title}");
    }

    [Fact]
    public void MapInProgressTasks_WithUnassignedItem_UsesUnassigned()
    {
        // Arrange
        var workItems = new List<WorkItemInfo>
        {
            new WorkItemInfo(
                Id: "123",
                Title: "Task",
                Type: "Task",
                Status: WorkItemStatus.Active,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com",
                AssignedTo: null)
        };

        // Act
        var result = ProjectDashboardMapper.MapInProgressTasks(workItems);

        // Assert
        result[0].AssigneeName.Should().Be("Unassigned");
    }

    [Fact]
    public void MapInProgressTasks_LimitsToFive()
    {
        // Arrange
        var workItems = Enumerable.Range(1, 10)
            .Select(i => new WorkItemInfo(
                Id: i.ToString(),
                Title: $"Task {i}",
                Type: "Task",
                Status: WorkItemStatus.Active,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com"))
            .ToList();

        // Act
        var result = ProjectDashboardMapper.MapInProgressTasks(workItems);

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region MapInReviewTasks Tests

    [Fact]
    public void MapInReviewTasks_MapsPullRequestsToTasks()
    {
        // Arrange
        var prs = new List<PullRequestInfo>
        {
            new PullRequestInfo(
                Id: "42",
                Title: "Feature PR",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Active",
                Url: "https://dev.azure.com",
                CreatedAt: DateTimeOffset.UtcNow,
                IsDraft: false)
        };

        // Act
        var result = ProjectDashboardMapper.MapInReviewTasks(prs);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("PR-42");
        result[0].Title.Should().Be("Feature PR");
        result[0].AssigneeInitials.Should().Be("PR");
        result[0].Priority.Should().Be(TaskPriority.Medium);
        _output.WriteLine($"Mapped PR: {result[0].Title}");
    }

    [Fact]
    public void MapInReviewTasks_DraftPR_HasLowPriority()
    {
        // Arrange
        var prs = new List<PullRequestInfo>
        {
            new PullRequestInfo(
                Id: "42",
                Title: "Draft PR",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Active",
                Url: "https://dev.azure.com",
                CreatedAt: DateTimeOffset.UtcNow,
                IsDraft: true)
        };

        // Act
        var result = ProjectDashboardMapper.MapInReviewTasks(prs);

        // Assert
        result[0].Priority.Should().Be(TaskPriority.Low);
    }

    [Fact]
    public void MapInReviewTasks_LimitsToFive()
    {
        // Arrange
        var prs = Enumerable.Range(1, 10)
            .Select(i => new PullRequestInfo(
                Id: i.ToString(),
                Title: $"PR {i}",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Active",
                Url: "https://dev.azure.com",
                CreatedAt: DateTimeOffset.UtcNow))
            .ToList();

        // Act
        var result = ProjectDashboardMapper.MapInReviewTasks(prs);

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region MapTeamMembers Tests

    [Fact]
    public void MapTeamMembers_GroupsByAuthor()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo("sha1", "Msg 1", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "John Doe"),
            new CommitInfo("sha2", "Msg 2", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "John Doe"),
            new CommitInfo("sha3", "Msg 3", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "Jane Smith"),
        };

        // Act
        var result = ProjectDashboardMapper.MapTeamMembers(commits);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("John Doe"); // More commits, so first
        result[0].TaskCount.Should().Be(2);
        result[1].Name.Should().Be("Jane Smith");
        result[1].TaskCount.Should().Be(1);
        _output.WriteLine($"Team members: {string.Join(", ", result.Select(m => m.Name))}");
    }

    [Fact]
    public void MapTeamMembers_WithNoCommits_ReturnsEmptyList()
    {
        // Arrange
        var commits = new List<CommitInfo>();

        // Act
        var result = ProjectDashboardMapper.MapTeamMembers(commits);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void MapTeamMembers_WithNullAuthors_ExcludesThem()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo("sha1", "Msg 1", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: null),
            new CommitInfo("sha2", "Msg 2", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "John Doe"),
            new CommitInfo("sha3", "Msg 3", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: string.Empty),
        };

        // Act
        var result = ProjectDashboardMapper.MapTeamMembers(commits);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("John Doe");
    }

    [Fact]
    public void MapTeamMembers_LimitsToFive()
    {
        // Arrange
        var commits = Enumerable.Range(1, 10)
            .Select(i => new CommitInfo(
                Sha: $"sha-{i}",
                Message: "Commit",
                Repository: "repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow,
                Author: $"Author {i}"))
            .ToList();

        // Act
        var result = ProjectDashboardMapper.MapTeamMembers(commits);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public void MapTeamMembers_SetsInitialsCorrectly()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo("sha1", "Msg", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "John Doe")
        };

        // Act
        var result = ProjectDashboardMapper.MapTeamMembers(commits);

        // Assert
        result[0].Initials.Should().Be("JD");
    }

    [Fact]
    public void MapTeamMembers_SetsRoleAndStatus()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo("sha1", "Msg", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "John Doe")
        };

        // Act
        var result = ProjectDashboardMapper.MapTeamMembers(commits);

        // Assert
        result[0].Role.Should().Be("Developer");
        result[0].RoleType.Should().Be(TeamRole.Developer);
        result[0].Status.Should().Be(MemberStatus.Active);
    }

    #endregion
}
