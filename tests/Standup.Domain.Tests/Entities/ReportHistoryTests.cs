using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class ReportHistoryTests
{
    [Fact]
    public void NewReportHistory_HasDefaultValues()
    {
        // Act
        var report = new ReportHistory();

        // Assert
        report.Id.Should().NotBeNullOrEmpty();
        report.GroupId.Should().BeEmpty();
        report.GroupName.Should().BeEmpty();
        report.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        report.PeriodStart.Should().Be(default);
        report.PeriodEnd.Should().Be(default);
        report.SummaryType.Should().Be(SummaryType.Technical);
        report.TotalCommits.Should().Be(0);
        report.TotalPullRequests.Should().Be(0);
        report.TotalWorkItems.Should().Be(0);
        report.ReportContent.Should().BeEmpty();
        report.ClientCodes.Should().BeEmpty();
    }

    [Fact]
    public void ReportHistory_GeneratesUniqueIds()
    {
        // Act
        var report1 = new ReportHistory();
        var report2 = new ReportHistory();

        // Assert
        report1.Id.Should().NotBe(report2.Id);
    }

    [Fact]
    public void ReportHistory_CanSetProperties()
    {
        // Arrange
        var report = new ReportHistory();
        var periodStart = DateTimeOffset.UtcNow.AddDays(-7);
        var periodEnd = DateTimeOffset.UtcNow;

        // Act
        report.GroupId = "group-123";
        report.GroupName = "Team A";
        report.PeriodStart = periodStart;
        report.PeriodEnd = periodEnd;
        report.SummaryType = SummaryType.Executive;
        report.TotalCommits = 25;
        report.TotalPullRequests = 5;
        report.TotalWorkItems = 10;
        report.ReportContent = "# Standup Report\n\nDetails...";
        report.ClientCodes.Add("CLIENT-001");

        // Assert
        report.GroupId.Should().Be("group-123");
        report.GroupName.Should().Be("Team A");
        report.PeriodStart.Should().Be(periodStart);
        report.PeriodEnd.Should().Be(periodEnd);
        report.SummaryType.Should().Be(SummaryType.Executive);
        report.TotalCommits.Should().Be(25);
        report.TotalPullRequests.Should().Be(5);
        report.TotalWorkItems.Should().Be(10);
        report.ReportContent.Should().Contain("Standup Report");
        report.ClientCodes.Should().Contain("CLIENT-001");
    }

    [Fact]
    public void DisplaySummary_ReturnsCorrectFormat()
    {
        // Arrange
        var report = new ReportHistory
        {
            PeriodStart = new DateTimeOffset(2025, 12, 7, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 12, 14, 0, 0, 0, TimeSpan.Zero),
            TotalCommits = 20,
            TotalPullRequests = 2
        };

        // Act
        var summary = report.DisplaySummary;

        // Assert
        summary.Should().Be("Dec 7 - Dec 14: 20 commits, 2 PRs");
    }

    [Fact]
    public void DisplaySummary_HandlesZeroCounts()
    {
        // Arrange
        var report = new ReportHistory
        {
            PeriodStart = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 1, 7, 0, 0, 0, TimeSpan.Zero),
            TotalCommits = 0,
            TotalPullRequests = 0
        };

        // Act
        var summary = report.DisplaySummary;

        // Assert
        summary.Should().Be("Jan 1 - Jan 7: 0 commits, 0 PRs");
    }

    [Fact]
    public void ReportHistory_CanHaveMultpleClientCodes()
    {
        // Arrange
        var report = new ReportHistory();

        // Act
        report.ClientCodes.Add("CLIENT-001");
        report.ClientCodes.Add("CLIENT-002");
        report.ClientCodes.Add("CLIENT-003");

        // Assert
        report.ClientCodes.Should().HaveCount(3);
        report.ClientCodes.Should().ContainInOrder("CLIENT-001", "CLIENT-002", "CLIENT-003");
    }

    [Theory]
    [InlineData(SummaryType.Technical)]
    [InlineData(SummaryType.Executive)]
    [InlineData(SummaryType.CodeReview)]
    public void ReportHistory_SupportsDifferentSummaryTypes(SummaryType summaryType)
    {
        // Arrange
        var report = new ReportHistory();

        // Act
        report.SummaryType = summaryType;

        // Assert
        report.SummaryType.Should().Be(summaryType);
    }
}
