using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.DTOs;

public class GroupedStandupReportDtoTests
{
    [Fact]
    public void ClientCodeSection_WithTypedSummary_PreservesSummaryInAllSummaries()
    {
        // Arrange
        var section = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: null,
            Commits: new List<CommitInfo>(),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>());

        // Act
        var updatedSection = section.WithTypedSummary(SummaryType.Technical, "Technical summary content");

        // Assert
        updatedSection.Summary.Should().Be("Technical summary content");
        updatedSection.AllSummaries.Should().NotBeNull();
        updatedSection.AllSummaries.Should().ContainKey(SummaryType.Technical);
        updatedSection.AllSummaries![SummaryType.Technical].Should().Be("Technical summary content");
    }

    [Fact]
    public void ClientCodeSection_WithMultipleSummaryTypes_PreservesAllSummaries()
    {
        // Arrange
        var section = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: null,
            Commits: new List<CommitInfo>(),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>());

        // Act - Add all three summary types
        var withTechnical = section.WithTypedSummary(SummaryType.Technical, "Technical: Code changes and APIs");
        var withExecutive = withTechnical.WithTypedSummary(SummaryType.Executive, "Executive: Business value delivered");
        var withCodeReview = withExecutive.WithTypedSummary(SummaryType.CodeReview, "CodeReview: Security assessment");

        // Assert - All three summaries should be preserved
        withCodeReview.AllSummaries.Should().HaveCount(3);
        withCodeReview.AllSummaries.Should().ContainKey(SummaryType.Technical);
        withCodeReview.AllSummaries.Should().ContainKey(SummaryType.Executive);
        withCodeReview.AllSummaries.Should().ContainKey(SummaryType.CodeReview);

        withCodeReview.AllSummaries![SummaryType.Technical].Should().Be("Technical: Code changes and APIs");
        withCodeReview.AllSummaries![SummaryType.Executive].Should().Be("Executive: Business value delivered");
        withCodeReview.AllSummaries![SummaryType.CodeReview].Should().Be("CodeReview: Security assessment");

        // Current summary should be the last one added
        withCodeReview.Summary.Should().Be("CodeReview: Security assessment");
    }

    [Fact]
    public void ClientCodeSection_GetSummary_ReturnsCorrectSummaryForType()
    {
        // Arrange
        var section = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: null,
            Commits: new List<CommitInfo>(),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>());

        var withAllSummaries = section
            .WithTypedSummary(SummaryType.Technical, "Technical summary")
            .WithTypedSummary(SummaryType.Executive, "Executive summary")
            .WithTypedSummary(SummaryType.CodeReview, "CodeReview summary");

        // Act & Assert
        withAllSummaries.GetSummary(SummaryType.Technical).Should().Be("Technical summary");
        withAllSummaries.GetSummary(SummaryType.Executive).Should().Be("Executive summary");
        withAllSummaries.GetSummary(SummaryType.CodeReview).Should().Be("CodeReview summary");
    }

    [Fact]
    public void ClientCodeSection_GetSummary_FallsBackToCurrentSummary_WhenTypeNotFound()
    {
        // Arrange
        var section = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: "Default summary",
            Commits: new List<CommitInfo>(),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>());

        // Act & Assert - No AllSummaries set, should fall back to Summary
        section.GetSummary(SummaryType.Executive).Should().Be("Default summary");
    }

    [Fact]
    public void GroupedStandupReportDto_TracksCurrentSummaryType()
    {
        // Arrange
        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "CLIENT1",
                Summary: "Test summary",
                Commits: new List<CommitInfo>(),
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>())
        };

        // Act
        var report = new GroupedStandupReportDto(
            Id: "test-id",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 5,
            TotalPullRequests: 2,
            TotalWorkItems: 3,
            CurrentSummaryType: SummaryType.Executive);

        // Assert
        report.CurrentSummaryType.Should().Be(SummaryType.Executive);
    }

    [Fact]
    public void GroupedStandupReportDto_DefaultSummaryType_IsTechnical()
    {
        // Arrange & Act
        var report = new GroupedStandupReportDto(
            Id: "test-id",
            GroupName: "Test Group",
            Sections: new List<ClientCodeSection>(),
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);

        // Assert - Default should be Technical
        report.CurrentSummaryType.Should().Be(SummaryType.Technical);
    }

    [Fact]
    public void ClientCodeSection_WithTypedSummary_DoesNotMutateOriginal()
    {
        // Arrange
        var original = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: "Original",
            Commits: new List<CommitInfo>(),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>());

        // Act
        var updated = original.WithTypedSummary(SummaryType.Technical, "New technical summary");

        // Assert - Original should be unchanged (immutability)
        original.Summary.Should().Be("Original");
        original.AllSummaries.Should().BeNull();

        updated.Summary.Should().Be("New technical summary");
        updated.AllSummaries.Should().NotBeNull();
    }

    [Fact]
    public void ClientCodeSection_WithTypedSummary_CanUpdateExistingSummaryType()
    {
        // Arrange
        var section = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: null,
            Commits: new List<CommitInfo>(),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>())
            .WithTypedSummary(SummaryType.Technical, "Version 1");

        // Act - Update the same summary type
        var updated = section.WithTypedSummary(SummaryType.Technical, "Version 2");

        // Assert
        updated.AllSummaries.Should().HaveCount(1);
        updated.AllSummaries![SummaryType.Technical].Should().Be("Version 2");
    }

    [Fact]
    public void GroupedStandupReport_WithMultipleSections_EachSectionCanHaveAllSummaryTypes()
    {
        // Arrange
        var section1 = new ClientCodeSection(
            ClientCode: "CLIENT1",
            Summary: null,
            Commits: CreateSampleCommits(3),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>())
            .WithTypedSummary(SummaryType.Technical, "Client1 Technical")
            .WithTypedSummary(SummaryType.Executive, "Client1 Executive")
            .WithTypedSummary(SummaryType.CodeReview, "Client1 CodeReview");

        var section2 = new ClientCodeSection(
            ClientCode: "CLIENT2",
            Summary: null,
            Commits: CreateSampleCommits(2),
            PullRequests: new List<PullRequestInfo>(),
            WorkItems: new List<WorkItemInfo>())
            .WithTypedSummary(SummaryType.Technical, "Client2 Technical")
            .WithTypedSummary(SummaryType.Executive, "Client2 Executive")
            .WithTypedSummary(SummaryType.CodeReview, "Client2 CodeReview");

        // Act
        var report = new GroupedStandupReportDto(
            Id: "multi-client-report",
            GroupName: "Multi-Client Group",
            Sections: new List<ClientCodeSection> { section1, section2 },
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-7),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 5,
            TotalPullRequests: 0,
            TotalWorkItems: 0,
            CurrentSummaryType: SummaryType.Technical);

        // Assert
        report.Sections.Should().HaveCount(2);

        foreach (var section in report.Sections)
        {
            section.AllSummaries.Should().HaveCount(3);
            section.AllSummaries.Should().ContainKey(SummaryType.Technical);
            section.AllSummaries.Should().ContainKey(SummaryType.Executive);
            section.AllSummaries.Should().ContainKey(SummaryType.CodeReview);
        }

        // Verify each client has distinct summaries
        report.Sections[0].GetSummary(SummaryType.Technical).Should().Contain("Client1");
        report.Sections[1].GetSummary(SummaryType.Technical).Should().Contain("Client2");
    }

    [Fact]
    public void AvailableSummaryTypes_ReturnsAllThreeTypes()
    {
        // Act
        var types = Enum.GetValues<SummaryType>();

        // Assert
        types.Should().HaveCount(3);
        types.Should().Contain(SummaryType.Technical);
        types.Should().Contain(SummaryType.Executive);
        types.Should().Contain(SummaryType.CodeReview);
    }

    private static List<CommitInfo> CreateSampleCommits(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new CommitInfo(
                Sha: $"sha{i}",
                Message: $"Commit message {i}",
                Repository: "test-repo",
                SourceType: SourceType.GitHub,
                CommittedAt: DateTimeOffset.UtcNow.AddHours(-i)))
            .ToList();
    }
}
