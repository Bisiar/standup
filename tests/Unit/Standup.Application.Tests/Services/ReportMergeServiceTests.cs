// -----------------------------------------------------------------------
// <copyright file="ReportMergeServiceTests.cs" company="Standup">
//     Copyright (c) Standup. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="ReportMergeService"/>.
/// </summary>
public sealed class ReportMergeServiceTests
{
    [Fact]
    public void MergeReports_WithEmptyNewReport_ReturnsExistingReport()
    {
        // Arrange
        var existingSection = CreateSection("CLIENT-A", commits: new List<CommitInfo>
        {
            CreateCommit("abc123", "Initial commit"),
        });
        var existingReport = CreateReport(new List<ClientCodeSection> { existingSection });
        var emptyReport = CreateReport(new List<ClientCodeSection>());

        // Act
        var result = ReportMergeService.MergeReports(existingReport, emptyReport);

        // Assert
        result.Should().Be(existingReport);
    }

    [Fact]
    public void MergeReports_WithNewSection_AddsInAlphabeticalOrder()
    {
        // Arrange
        var sectionC = CreateSection("CLIENT-C", commits: new List<CommitInfo>
        {
            CreateCommit("ccc111", "C commit"),
        });
        var existingReport = CreateReport(new List<ClientCodeSection> { sectionC });

        var sectionA = CreateSection("CLIENT-A", commits: new List<CommitInfo>
        {
            CreateCommit("aaa111", "A commit"),
        });
        var newReport = CreateReport(new List<ClientCodeSection> { sectionA });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert
        result.Sections.Should().HaveCount(2);
        result.Sections[0].ClientCode.Should().Be("CLIENT-A");
        result.Sections[1].ClientCode.Should().Be("CLIENT-C");
    }

    [Fact]
    public void MergeReports_WithExistingClientCode_MergesSectionsDeduplicatingCommits()
    {
        // Arrange
        var existingCommit = CreateCommit("abc123", "Existing commit", DateTimeOffset.UtcNow.AddHours(-1));
        var existingSection = CreateSection("CLIENT-A", commits: new List<CommitInfo> { existingCommit });
        var existingReport = CreateReport(new List<ClientCodeSection> { existingSection });

        var newCommit = CreateCommit("def456", "New commit", DateTimeOffset.UtcNow);
        var duplicateCommit = CreateCommit("abc123", "Existing commit duplicate", DateTimeOffset.UtcNow.AddHours(-1));
        var newSection = CreateSection("CLIENT-A", commits: new List<CommitInfo> { newCommit, duplicateCommit });
        var newReport = CreateReport(new List<ClientCodeSection> { newSection });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert
        result.Sections.Should().HaveCount(1);
        result.Sections[0].Commits.Should().HaveCount(2);
        result.Sections[0].Commits.Select(c => c.Sha).Should().Contain(new[] { "abc123", "def456" });
    }

    [Fact]
    public void MergeReports_WithExistingClientCode_DeduplicatesPullRequests()
    {
        // Arrange
        var existingPr = CreatePullRequest("pr-1", "Existing PR");
        var existingSection = CreateSection("CLIENT-A", pullRequests: new List<PullRequestInfo> { existingPr });
        var existingReport = CreateReport(new List<ClientCodeSection> { existingSection });

        var newPr = CreatePullRequest("pr-2", "New PR");
        var duplicatePr = CreatePullRequest("pr-1", "Duplicate PR");
        var newSection = CreateSection("CLIENT-A", pullRequests: new List<PullRequestInfo> { newPr, duplicatePr });
        var newReport = CreateReport(new List<ClientCodeSection> { newSection });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert
        result.Sections[0].PullRequests.Should().HaveCount(2);
        result.Sections[0].PullRequests.Select(p => p.Id).Should().Contain(new[] { "pr-1", "pr-2" });
    }

    [Fact]
    public void MergeReports_WithExistingClientCode_DeduplicatesWorkItems()
    {
        // Arrange
        var existingItem = CreateWorkItem("wi-1", "Existing work item");
        var existingSection = CreateSection("CLIENT-A", workItems: new List<WorkItemInfo> { existingItem });
        var existingReport = CreateReport(new List<ClientCodeSection> { existingSection });

        var newItem = CreateWorkItem("wi-2", "New work item");
        var duplicateItem = CreateWorkItem("wi-1", "Duplicate work item");
        var newSection = CreateSection("CLIENT-A", workItems: new List<WorkItemInfo> { newItem, duplicateItem });
        var newReport = CreateReport(new List<ClientCodeSection> { newSection });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert
        result.Sections[0].WorkItems.Should().HaveCount(2);
        result.Sections[0].WorkItems.Select(w => w.Id).Should().Contain(new[] { "wi-1", "wi-2" });
    }

    [Fact]
    public void MergeReports_RecalculatesTotals()
    {
        // Arrange
        var section1 = CreateSection("CLIENT-A", commits: new List<CommitInfo>
        {
            CreateCommit("aaa111", "Commit A1"),
            CreateCommit("aaa222", "Commit A2"),
        });
        var existingReport = CreateReport(new List<ClientCodeSection> { section1 });

        var section2 = CreateSection("CLIENT-B", commits: new List<CommitInfo>
        {
            CreateCommit("bbb111", "Commit B1"),
        });
        var newReport = CreateReport(new List<ClientCodeSection> { section2 });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert
        result.TotalCommits.Should().Be(3);
    }

    [Fact]
    public void MergeReports_SortsCommitsByDateDescending()
    {
        // Arrange
        var oldCommit = CreateCommit("old123", "Old commit", DateTimeOffset.UtcNow.AddDays(-2));
        var existingSection = CreateSection("CLIENT-A", commits: new List<CommitInfo> { oldCommit });
        var existingReport = CreateReport(new List<ClientCodeSection> { existingSection });

        var newCommit = CreateCommit("new456", "New commit", DateTimeOffset.UtcNow);
        var newSection = CreateSection("CLIENT-A", commits: new List<CommitInfo> { newCommit });
        var newReport = CreateReport(new List<ClientCodeSection> { newSection });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert
        result.Sections[0].Commits[0].Sha.Should().Be("new456");
        result.Sections[0].Commits[1].Sha.Should().Be("old123");
    }

    [Fact]
    public void MergeSummaries_WithExistingSummaries_MergesAll()
    {
        // Arrange
        var existingSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Existing technical summary" },
        };
        var existingSection = CreateSection("CLIENT-A", allSummaries: existingSummaries);

        var newSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Executive, "New executive summary" },
            { SummaryType.CodeReview, "New code review" },
        };
        var newSection = CreateSection("CLIENT-A", allSummaries: newSummaries);

        // Act
        var result = ReportMergeService.MergeSummaries(existingSection, newSection);

        // Assert
        result.AllSummaries.Should().HaveCount(3);
        result.AllSummaries.Should().ContainKey(SummaryType.Technical);
        result.AllSummaries.Should().ContainKey(SummaryType.Executive);
        result.AllSummaries.Should().ContainKey(SummaryType.CodeReview);
    }

    [Fact]
    public void MergeSummaries_WithOverlappingKeys_ReplacesWithNew()
    {
        // Arrange
        var existingSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Old technical summary" },
        };
        var existingSection = CreateSection("CLIENT-A", allSummaries: existingSummaries);

        var newSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Updated technical summary" },
        };
        var newSection = CreateSection("CLIENT-A", allSummaries: newSummaries);

        // Act
        var result = ReportMergeService.MergeSummaries(existingSection, newSection);

        // Assert
        result.AllSummaries![SummaryType.Technical].Should().Be("Updated technical summary");
    }

    [Fact]
    public void MergeSummaries_WithNullExistingSummaries_CreatesNew()
    {
        // Arrange
        var existingSection = CreateSection("CLIENT-A", allSummaries: null);

        var newSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Executive, "Executive summary" },
        };
        var newSection = CreateSection("CLIENT-A", allSummaries: newSummaries);

        // Act
        var result = ReportMergeService.MergeSummaries(existingSection, newSection);

        // Assert
        result.AllSummaries.Should().HaveCount(1);
        result.AllSummaries![SummaryType.Executive].Should().Be("Executive summary");
    }

    [Fact]
    public void MergeReports_AppendsSummariesForSameClientCode()
    {
        // Arrange
        var existingSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Tech from repo 1" },
        };
        var existingSection = CreateSection(
            "CLIENT-A",
            allSummaries: existingSummaries,
            commits: new List<CommitInfo> { CreateCommit("aaa111", "Commit 1") });
        var existingReport = CreateReport(new List<ClientCodeSection> { existingSection });

        var newSummaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Tech from repo 2" },
        };
        var newSection = CreateSection(
            "CLIENT-A",
            allSummaries: newSummaries,
            commits: new List<CommitInfo> { CreateCommit("bbb222", "Commit 2") });
        var newReport = CreateReport(new List<ClientCodeSection> { newSection });

        // Act
        var result = ReportMergeService.MergeReports(existingReport, newReport);

        // Assert - summaries should be appended with newlines
        result.Sections[0].AllSummaries![SummaryType.Technical]
            .Should().Contain("Tech from repo 1")
            .And.Contain("Tech from repo 2");
    }

    private static CommitInfo CreateCommit(string sha, string message, DateTimeOffset? committedAt = null)
    {
        return new CommitInfo(
            Sha: sha,
            Message: message,
            Repository: "test-repo",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt ?? DateTimeOffset.UtcNow);
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

    private static ClientCodeSection CreateSection(
        string clientCode,
        List<CommitInfo>? commits = null,
        List<PullRequestInfo>? pullRequests = null,
        List<WorkItemInfo>? workItems = null,
        Dictionary<SummaryType, string>? allSummaries = null,
        string? summary = null)
    {
        return new ClientCodeSection(
            ClientCode: clientCode,
            Summary: summary,
            Commits: commits ?? new List<CommitInfo>(),
            PullRequests: pullRequests ?? new List<PullRequestInfo>(),
            WorkItems: workItems ?? new List<WorkItemInfo>(),
            AllSummaries: allSummaries);
    }

    private static GroupedStandupReportDto CreateReport(
        List<ClientCodeSection> sections,
        int? totalCommits = null,
        int? totalPrs = null,
        int? totalWorkItems = null)
    {
        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: totalCommits ?? sections.Sum(s => s.CommitCount),
            TotalPullRequests: totalPrs ?? sections.Sum(s => s.PullRequestCount),
            TotalWorkItems: totalWorkItems ?? sections.Sum(s => s.WorkItemCount));
    }
}
