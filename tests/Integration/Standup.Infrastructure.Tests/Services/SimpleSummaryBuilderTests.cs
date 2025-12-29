// -----------------------------------------------------------------------
// <copyright file="SimpleSummaryBuilderTests.cs" company="Standup">
//     Copyright (c) Standup. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Infrastructure.Services;
using Xunit;

namespace Standup.Infrastructure.Tests.Services;

/// <summary>
/// Tests for <see cref="SimpleSummaryBuilder"/>.
/// </summary>
public sealed class SimpleSummaryBuilderTests
{
    private static readonly DateTimeOffset Since = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Until = new(2024, 1, 7, 23, 59, 59, TimeSpan.Zero);

    [Fact]
    public void Build_WithCommitsOnly_GeneratesSummaryWithCommits()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            CreateCommit("abc123", "Add new feature"),
            CreateCommit("def456", "Fix bug in login"),
        };
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("## Standup Report");
        result.Should().Contain("### Commits");
        result.Should().Contain("Add new feature");
        result.Should().Contain("Fix bug in login");
        result.Should().NotContain("### Pull Requests");
        result.Should().NotContain("### Work Items");
    }

    [Fact]
    public void Build_WithPullRequestsOnly_GeneratesSummaryWithPRs()
    {
        // Arrange
        var commits = new List<CommitInfo>();
        var prs = new List<PullRequestInfo>
        {
            CreatePullRequest("pr-1", "Feature: Add authentication", "Open"),
            CreatePullRequest("pr-2", "Bugfix: Fix memory leak", "Merged"),
        };
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("### Pull Requests");
        result.Should().Contain("[Open] Feature: Add authentication");
        result.Should().Contain("[Merged] Bugfix: Fix memory leak");
    }

    [Fact]
    public void Build_WithWorkItemsOnly_GeneratesSummaryWithWorkItems()
    {
        // Arrange
        var commits = new List<CommitInfo>();
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>
        {
            CreateWorkItem("wi-1", "Implement login page", WorkItemStatus.Active),
            CreateWorkItem("wi-2", "Review security audit", WorkItemStatus.Closed),
        };

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("### Work Items");
        result.Should().Contain("[Active] Implement login page (Task)");
        result.Should().Contain("[Closed] Review security audit (Task)");
    }

    [Fact]
    public void Build_WithAllData_GeneratesComprehensiveSummary()
    {
        // Arrange
        var commits = new List<CommitInfo> { CreateCommit("abc123", "Add feature") };
        var prs = new List<PullRequestInfo> { CreatePullRequest("pr-1", "Feature PR") };
        var workItems = new List<WorkItemInfo> { CreateWorkItem("wi-1", "Task item") };

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("## Standup Report");
        result.Should().Contain("### Commits");
        result.Should().Contain("### Pull Requests");
        result.Should().Contain("### Work Items");
    }

    [Fact]
    public void Build_WithNoData_GeneratesEmptyActivityMessage()
    {
        // Arrange
        var commits = new List<CommitInfo>();
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("*No activity found for this period.*");
    }

    [Fact]
    public void Build_WithMoreThan10Commits_TruncatesAndShowsCount()
    {
        // Arrange
        var commits = Enumerable.Range(1, 15)
            .Select(i => CreateCommit($"sha{i:D3}", $"Commit number {i}"))
            .ToList();
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("Commit number 1");
        result.Should().Contain("Commit number 10");
        result.Should().NotContain("Commit number 11");
        result.Should().Contain("... and 5 more");
    }

    [Fact]
    public void Build_WithLongCommitMessage_TruncatesAt80Characters()
    {
        // Arrange
        var longMessage = new string('x', 100);
        var commits = new List<CommitInfo> { CreateCommit("abc123", longMessage) };
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("...");
        result.Should().NotContain(longMessage);
    }

    [Fact]
    public void Build_WithMultilineCommitMessage_UsesOnlyFirstLine()
    {
        // Arrange
        var multilineMessage = "First line subject\n\nSecond line body\nThird line details";
        var commits = new List<CommitInfo> { CreateCommit("abc123", multilineMessage) };
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("First line subject");
        result.Should().NotContain("Second line body");
    }

    [Fact]
    public void Build_IncludesPeriodDates()
    {
        // Arrange
        var commits = new List<CommitInfo>();
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Act
        var result = SimpleSummaryBuilder.Build(commits, prs, workItems, Since, Until);

        // Assert
        result.Should().Contain("**Period:**");
        result.Should().Contain("Jan 01");
        result.Should().Contain("Jan 07, 2024");
    }

    private static CommitInfo CreateCommit(string sha, string message)
    {
        return new CommitInfo(
            Sha: sha,
            Message: message,
            Repository: "test-repo",
            SourceType: SourceType.GitHub,
            CommittedAt: DateTimeOffset.UtcNow);
    }

    private static PullRequestInfo CreatePullRequest(string id, string title, string status = "Open")
    {
        return new PullRequestInfo(
            Id: id,
            Title: title,
            Repository: "test-repo",
            SourceType: SourceType.GitHub,
            Status: status,
            Url: $"https://github.com/test/{id}",
            CreatedAt: DateTimeOffset.UtcNow);
    }

    private static WorkItemInfo CreateWorkItem(string id, string title, WorkItemStatus status = WorkItemStatus.Active)
    {
        return new WorkItemInfo(
            Id: id,
            Title: title,
            Type: "Task",
            Status: status,
            SourceType: SourceType.AzureDevOps,
            Url: $"https://dev.azure.com/test/{id}");
    }
}
