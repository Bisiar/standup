// <copyright file="StandupDataTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="StandupData"/> entity.
/// </summary>
public sealed class StandupDataTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandupDataTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public StandupDataTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StandupData_WithNoParameters_HasEmptyCollections()
    {
        // Act
        var data = new StandupData();

        // Assert
        data.Commits.Should().NotBeNull().And.BeEmpty();
        data.PullRequests.Should().NotBeNull().And.BeEmpty();
        data.WorkItems.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void StandupData_WithNullParameters_HasEmptyCollections()
    {
        // Act
        var data = new StandupData(null, null, null);

        // Assert
        data.Commits.Should().NotBeNull().And.BeEmpty();
        data.PullRequests.Should().NotBeNull().And.BeEmpty();
        data.WorkItems.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void StandupData_WithCommits_StoresCommitsCorrectly()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo(
                Sha: "abc123",
                Message: "Fix bug",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow,
                Author: "John Doe"),
            new CommitInfo(
                Sha: "def456",
                Message: "Add feature",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow.AddHours(-1),
                Author: "Jane Smith")
        };

        // Act
        var data = new StandupData(Commits: commits);

        // Assert
        data.Commits.Should().HaveCount(2);
        data.Commits[0].Message.Should().Be("Fix bug");
        _output.WriteLine($"Stored {data.Commits.Count} commits");
    }

    [Fact]
    public void StandupData_WithPullRequests_StoresPullRequestsCorrectly()
    {
        // Arrange
        var prs = new List<PullRequestInfo>
        {
            new PullRequestInfo(
                Id: "1",
                Title: "Feature PR",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Active",
                Url: "https://dev.azure.com/pr/1",
                CreatedAt: DateTimeOffset.UtcNow),
            new PullRequestInfo(
                Id: "2",
                Title: "Bug Fix PR",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Completed",
                Url: "https://dev.azure.com/pr/2",
                CreatedAt: DateTimeOffset.UtcNow)
        };

        // Act
        var data = new StandupData(PullRequests: prs);

        // Assert
        data.PullRequests.Should().HaveCount(2);
        data.PullRequests[0].Title.Should().Be("Feature PR");
        _output.WriteLine($"Stored {data.PullRequests.Count} pull requests");
    }

    [Fact]
    public void StandupData_WithWorkItems_StoresWorkItemsCorrectly()
    {
        // Arrange
        var workItems = new List<WorkItemInfo>
        {
            new WorkItemInfo(
                Id: "123",
                Title: "Task 1",
                Type: "Task",
                Status: WorkItemStatus.Active,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com/wi/123"),
            new WorkItemInfo(
                Id: "456",
                Title: "Bug 2",
                Type: "Bug",
                Status: WorkItemStatus.Resolved,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com/wi/456")
        };

        // Act
        var data = new StandupData(WorkItems: workItems);

        // Assert
        data.WorkItems.Should().HaveCount(2);
        data.WorkItems[0].Title.Should().Be("Task 1");
        _output.WriteLine($"Stored {data.WorkItems.Count} work items");
    }

    [Fact]
    public void StandupData_WithAllData_StoresAllCorrectly()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new CommitInfo("123", "Commit 1", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow)
        };
        var prs = new List<PullRequestInfo>
        {
            new PullRequestInfo("1", "PR 1", "repo", SourceType.AzureDevOps, "Active", "url", DateTimeOffset.UtcNow)
        };
        var workItems = new List<WorkItemInfo>
        {
            new WorkItemInfo("1", "Task 1", "Task", WorkItemStatus.Active, SourceType.AzureDevOps, "url")
        };

        // Act
        var data = new StandupData(commits, prs, workItems);

        // Assert
        data.Commits.Should().HaveCount(1);
        data.PullRequests.Should().HaveCount(1);
        data.WorkItems.Should().HaveCount(1);
        _output.WriteLine($"Total: {data.Commits.Count} commits, {data.PullRequests.Count} PRs, {data.WorkItems.Count} work items");
    }

    [Fact]
    public void StandupData_WithDefaultLists_HasEmptyLists()
    {
        // Arrange
        var data1 = new StandupData();
        var data2 = new StandupData();

        // Act & Assert - Both have empty lists (but different list instances)
        data1.Commits.Should().BeEmpty();
        data2.Commits.Should().BeEmpty();
        data1.PullRequests.Should().BeEmpty();
        data2.PullRequests.Should().BeEmpty();
        data1.WorkItems.Should().BeEmpty();
        data2.WorkItems.Should().BeEmpty();
    }
}
