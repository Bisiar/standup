// -----------------------------------------------------------------------
// <copyright file="StandupHtmlBuilderTests.cs" company="Standup">
//     Copyright (c) Standup. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for <see cref="StandupHtmlBuilder"/>.
/// </summary>
public sealed class StandupHtmlBuilderTests
{
    [Fact]
    public void AppendHeader_GeneratesValidHtmlDoctype()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        StandupHtmlBuilder.AppendHeader(sb);

        // Assert
        var result = sb.ToString();
        result.Should().StartWith("<!DOCTYPE html>");
        result.Should().Contain("<html>");
        result.Should().Contain("<head>");
        result.Should().Contain("<style>");
        result.Should().Contain("<body>");
    }

    [Fact]
    public void AppendHeader_IncludesStyleDefinitions()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        StandupHtmlBuilder.AppendHeader(sb);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("font-family:");
        result.Should().Contain(".summary");
        result.Should().Contain(".status");
        result.Should().Contain(".totals");
    }

    [Fact]
    public void AppendHeader_IncludesJavaScriptForToggle()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        StandupHtmlBuilder.AppendHeader(sb);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("<script>");
        result.Should().Contain("toggleAll");
        result.Should().Contain("</script>");
    }

    [Fact]
    public void AppendFooter_IncludesTotals()
    {
        // Arrange
        var sb = new StringBuilder();
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo>
            {
                CreateCommit("abc", "Commit 1"),
                CreateCommit("def", "Commit 2"),
            });
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendFooter(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Totals:");
        result.Should().Contain("2 commits");
        result.Should().Contain("0 PRs");
        result.Should().Contain("0 work items");
    }

    [Fact]
    public void AppendFooter_IncludesGeneratedTimestamp()
    {
        // Arrange
        var sb = new StringBuilder();
        var report = CreateReport(new List<ClientCodeSection>());

        // Act
        StandupHtmlBuilder.AppendFooter(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Generated at");
        result.Should().Contain("</body></html>");
    }

    [Fact]
    public void AppendConsolidatedStandup_WithHighlights_RendersAllHighlights()
    {
        // Arrange
        var sb = new StringBuilder();
        var highlights = new List<(string ClientCode, string Highlight)>
        {
            ("CLIENT-A", "Added new authentication feature"),
            ("CLIENT-B", "Fixed critical bug in payment processing"),
        };

        // Act
        StandupHtmlBuilder.AppendConsolidatedStandup(sb, highlights);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Quick Standup");
        result.Should().Contain("CLIENT-A");
        result.Should().Contain("Added new authentication feature");
        result.Should().Contain("CLIENT-B");
        result.Should().Contain("Fixed critical bug in payment processing");
    }

    [Fact]
    public void AppendConsolidatedStandup_WithEmptyHighlights_ShowsNoActivityMessage()
    {
        // Arrange
        var sb = new StringBuilder();
        var highlights = new List<(string ClientCode, string Highlight)>();

        // Act
        StandupHtmlBuilder.AppendConsolidatedStandup(sb, highlights);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("No significant activity to highlight");
    }

    [Fact]
    public void AppendConsolidatedStandup_WithEmptyClientCode_ShowsGeneral()
    {
        // Arrange
        var sb = new StringBuilder();
        var highlights = new List<(string ClientCode, string Highlight)>
        {
            (string.Empty, "Some work was done"),
            ("  ", "More work with whitespace client code"),
        };

        // Act
        StandupHtmlBuilder.AppendConsolidatedStandup(sb, highlights);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("General");
    }

    [Fact]
    public void AppendTeamOverview_FiltersSectionsWithoutCommits()
    {
        // Arrange
        var sb = new StringBuilder();
        var sectionWithCommits = CreateSection(
            "ACTIVE-CLIENT",
            commits: new List<CommitInfo> { CreateCommit("abc", "Active commit") });
        var sectionWithoutCommits = CreateSection("INACTIVE-CLIENT");
        var report = CreateReport(new List<ClientCodeSection> { sectionWithCommits, sectionWithoutCommits });

        // Act
        StandupHtmlBuilder.AppendTeamOverview(sb, report, section => "Quick summary");

        // Assert
        var result = sb.ToString();
        result.Should().Contain("ACTIVE-CLIENT");
        result.Should().NotContain("INACTIVE-CLIENT");
    }

    [Fact]
    public void AppendTeamOverview_ShowsCommitAndPRCounts()
    {
        // Arrange
        var sb = new StringBuilder();
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo>
            {
                CreateCommit("abc", "Commit 1"),
                CreateCommit("def", "Commit 2"),
            },
            pullRequests: new List<PullRequestInfo>
            {
                CreatePullRequest("pr-1", "PR 1"),
            });
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTeamOverview(sb, report, section => "Summary text");

        // Assert
        var result = sb.ToString();
        result.Should().Contain("2 commits");
        result.Should().Contain("1 PRs");
    }

    [Fact]
    public void GetSourceStatusIndicator_WithNullStatus_ReturnsEmpty()
    {
        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSourceStatusIndicator_WithAllSuccess_ReturnsEmpty()
    {
        // Arrange
        var status = DataSourceStatus.AllSuccess();

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSourceStatusIndicator_WithNoPatForPRs_ReturnsWarning()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Success,
            FetchStatus.NoPat,
            FetchStatus.Success);

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().Contain("⚠️ PRs");
        result.Should().Contain("no PAT");
    }

    [Fact]
    public void GetSourceStatusIndicator_WithErrorForPRs_ReturnsError()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Success,
            FetchStatus.Error,
            FetchStatus.Success);

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().Contain("❌ PRs");
        result.Should().Contain("failed");
    }

    [Fact]
    public void GetSourceStatusIndicator_WithNoPatForWorkItems_ReturnsWarning()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Success,
            FetchStatus.Success,
            FetchStatus.NoPat);

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().Contain("⚠️ Items");
    }

    [Fact]
    public void GetSourceStatusIndicator_WithErrorForWorkItems_ReturnsError()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Success,
            FetchStatus.Success,
            FetchStatus.Error);

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().Contain("❌ Items");
    }

    [Fact]
    public void GetSourceStatusIndicator_WithErrorForCommits_ReturnsError()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Error,
            FetchStatus.Success,
            FetchStatus.Success);

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().Contain("❌ Commits");
    }

    [Fact]
    public void GetSourceStatusIndicator_WithMultipleIssues_ReturnsAllIndicators()
    {
        // Arrange
        var status = new DataSourceStatus(
            FetchStatus.Error,
            FetchStatus.NoPat,
            FetchStatus.Error);

        // Act
        var result = StandupHtmlBuilder.GetSourceStatusIndicator(status);

        // Assert
        result.Should().Contain("❌ Commits");
        result.Should().Contain("⚠️ PRs");
        result.Should().Contain("❌ Items");
    }

    [Fact]
    public void AppendTechnicalDetails_WithTechnicalSummary_RendersSummary()
    {
        // Arrange
        var sb = new StringBuilder();
        var summaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "This is a technical summary with code changes." },
        };
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo> { CreateCommit("abc", "Commit") },
            allSummaries: summaries);
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Technical Details");
        result.Should().Contain("CLIENT-A");
        result.Should().Contain("technical summary");
    }

    [Fact]
    public void AppendTechnicalDetails_WithCodeReviewSummary_RendersCodeReview()
    {
        // Arrange
        var sb = new StringBuilder();
        var summaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Technical summary" },
            { SummaryType.CodeReview, "Code review findings" },
        };
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo> { CreateCommit("abc", "Commit") },
            allSummaries: summaries);
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Code Review");
        result.Should().Contain("Code review findings");
    }

    [Fact]
    public void AppendTechnicalDetails_WithNoSummaries_UsesFallbackSummary()
    {
        // Arrange
        var sb = new StringBuilder();
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo> { CreateCommit("abc", "Commit") },
            summary: "This is a fallback summary.");
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("fallback summary");
    }

    [Fact]
    public void AppendExecutiveSummaries_WithExecutiveSummary_RendersSection()
    {
        // Arrange
        var sb = new StringBuilder();
        var summaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Executive, "Executive summary for stakeholders" },
        };
        var section = CreateSection("CLIENT-A", allSummaries: summaries);
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendExecutiveSummaries(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Executive Summary");
        result.Should().Contain("Client-Shareable");
        result.Should().Contain("Executive summary for stakeholders");
    }

    [Fact]
    public void AppendExecutiveSummaries_WithNoExecutiveSummary_RendersNothing()
    {
        // Arrange
        var sb = new StringBuilder();
        var summaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Only technical" },
        };
        var section = CreateSection("CLIENT-A", allSummaries: summaries);
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendExecutiveSummaries(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().NotContain("Executive Summary");
    }

    [Fact]
    public void AppendTechnicalDetails_WithPullRequests_RendersPRSection()
    {
        // Arrange
        var sb = new StringBuilder();
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo> { CreateCommit("abc", "Commit") },
            pullRequests: new List<PullRequestInfo>
            {
                CreatePullRequest("pr-1", "Add authentication", "Merged"),
                CreatePullRequest("pr-2", "Fix bug", "Open"),
            });
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Pull Requests");
        result.Should().Contain("Add authentication");
        result.Should().Contain("status-merged");
        result.Should().Contain("status-open");
    }

    [Fact]
    public void AppendTechnicalDetails_WithWorkItems_RendersWorkItemsSection()
    {
        // Arrange
        var sb = new StringBuilder();
        var section = CreateSection(
            "CLIENT-A",
            commits: new List<CommitInfo> { CreateCommit("abc", "Commit") },
            workItems: new List<WorkItemInfo>
            {
                CreateWorkItem("wi-1", "Implement login", WorkItemStatus.Active),
                CreateWorkItem("wi-2", "Write tests", WorkItemStatus.Closed),
            });
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Work Items");
        result.Should().Contain("Implement login");
        result.Should().Contain("status-inprogress");
        result.Should().Contain("status-completed");
    }

    [Fact]
    public void AppendTechnicalDetails_WithMoreThan10Commits_ShowsTruncationMessage()
    {
        // Arrange
        var sb = new StringBuilder();
        var commits = Enumerable.Range(1, 15)
            .Select(i => CreateCommit($"sha{i:D3}", $"Commit number {i}"))
            .ToList();
        var section = CreateSection("CLIENT-A", commits: commits);
        var report = CreateReport(new List<ClientCodeSection> { section });

        // Act
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Assert
        var result = sb.ToString();
        result.Should().Contain("Commit number 1");
        result.Should().Contain("Commit number 10");
        result.Should().Contain("... and 5 more");
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
        string? summary = null,
        DataSourceStatus? sourceStatus = null)
    {
        return new ClientCodeSection(
            ClientCode: clientCode,
            Summary: summary,
            Commits: commits ?? new List<CommitInfo>(),
            PullRequests: pullRequests ?? new List<PullRequestInfo>(),
            WorkItems: workItems ?? new List<WorkItemInfo>(),
            AllSummaries: allSummaries,
            SourceStatus: sourceStatus);
    }

    private static GroupedStandupReportDto CreateReport(
        List<ClientCodeSection> sections,
        string groupName = "Test Group")
    {
        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: groupName,
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: sections.Sum(s => s.CommitCount),
            TotalPullRequests: sections.Sum(s => s.PullRequestCount),
            TotalWorkItems: sections.Sum(s => s.WorkItemCount));
    }
}
