using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.DTOs;

public class StandupReportDtoTests
{
    [Fact]
    public void StandupReportDto_CanBeCreated()
    {
        // Arrange
        var periodStart = DateTimeOffset.UtcNow.AddHours(-24);
        var periodEnd = DateTimeOffset.UtcNow;
        var generatedAt = DateTimeOffset.UtcNow;
        var sentTo = new List<NotificationChannel> { NotificationChannel.TeamsChannel, NotificationChannel.Email };

        // Act
        var dto = new StandupReportDto(
            Id: "report-123",
            Summary: "Today I worked on feature X",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            GeneratedAt: generatedAt,
            CommitCount: 5,
            PullRequestCount: 2,
            WorkItemCount: 3,
            SentTo: sentTo);

        // Assert
        dto.Id.Should().Be("report-123");
        dto.Summary.Should().Be("Today I worked on feature X");
        dto.PeriodStart.Should().Be(periodStart);
        dto.PeriodEnd.Should().Be(periodEnd);
        dto.GeneratedAt.Should().Be(generatedAt);
        dto.CommitCount.Should().Be(5);
        dto.PullRequestCount.Should().Be(2);
        dto.WorkItemCount.Should().Be(3);
        dto.SentTo.Should().HaveCount(2);
        dto.SentTo.Should().Contain(NotificationChannel.TeamsChannel);
        dto.SentTo.Should().Contain(NotificationChannel.Email);
    }

    [Fact]
    public void StandupReportDto_WithNoActivity_HasZeroCounts()
    {
        // Arrange
        var periodStart = DateTimeOffset.UtcNow.AddHours(-24);
        var periodEnd = DateTimeOffset.UtcNow;

        // Act
        var dto = new StandupReportDto(
            Id: "report-empty",
            Summary: "No activity today",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            GeneratedAt: DateTimeOffset.UtcNow,
            CommitCount: 0,
            PullRequestCount: 0,
            WorkItemCount: 0,
            SentTo: new List<NotificationChannel>());

        // Assert
        dto.CommitCount.Should().Be(0);
        dto.PullRequestCount.Should().Be(0);
        dto.WorkItemCount.Should().Be(0);
        dto.SentTo.Should().BeEmpty();
    }

    [Fact]
    public void StandupReportDto_IsRecord_SupportsEquality()
    {
        // Arrange
        var periodStart = DateTimeOffset.UtcNow.AddHours(-24);
        var periodEnd = DateTimeOffset.UtcNow;
        var generatedAt = DateTimeOffset.UtcNow;
        var sentTo = new List<NotificationChannel> { NotificationChannel.TeamsChannel };

        var dto1 = new StandupReportDto(
            Id: "report-1",
            Summary: "Summary",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            GeneratedAt: generatedAt,
            CommitCount: 5,
            PullRequestCount: 2,
            WorkItemCount: 3,
            SentTo: sentTo);

        var dto2 = new StandupReportDto(
            Id: "report-1",
            Summary: "Summary",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            GeneratedAt: generatedAt,
            CommitCount: 5,
            PullRequestCount: 2,
            WorkItemCount: 3,
            SentTo: sentTo);

        // Act & Assert
        dto1.Should().Be(dto2);
    }

    [Fact]
    public void StandupReportDto_DifferentValues_AreNotEqual()
    {
        // Arrange
        var periodStart = DateTimeOffset.UtcNow.AddHours(-24);
        var periodEnd = DateTimeOffset.UtcNow;
        var generatedAt = DateTimeOffset.UtcNow;
        var sentTo = new List<NotificationChannel>();

        var dto1 = new StandupReportDto("report-1", "Summary A", periodStart, periodEnd, generatedAt, 5, 2, 3, sentTo);
        var dto2 = new StandupReportDto("report-1", "Summary B", periodStart, periodEnd, generatedAt, 5, 2, 3, sentTo);

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void StandupReportDto_SupportsDeconstruction()
    {
        // Arrange
        var periodStart = DateTimeOffset.UtcNow.AddHours(-24);
        var periodEnd = DateTimeOffset.UtcNow;
        var generatedAt = DateTimeOffset.UtcNow;
        var sentTo = new List<NotificationChannel> { NotificationChannel.TeamsChannel };

        var dto = new StandupReportDto(
            Id: "report-1",
            Summary: "Test Summary",
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            GeneratedAt: generatedAt,
            CommitCount: 10,
            PullRequestCount: 5,
            WorkItemCount: 7,
            SentTo: sentTo);

        // Act
        var (id, summary, start, end, generated, commits, prs, workItems, channels) = dto;

        // Assert
        id.Should().Be("report-1");
        summary.Should().Be("Test Summary");
        start.Should().Be(periodStart);
        end.Should().Be(periodEnd);
        generated.Should().Be(generatedAt);
        commits.Should().Be(10);
        prs.Should().Be(5);
        workItems.Should().Be(7);
        channels.Should().BeEquivalentTo(sentTo);
    }

    [Fact]
    public void StandupReportDto_SupportsWithExpression()
    {
        // Arrange
        var original = new StandupReportDto(
            Id: "report-1",
            Summary: "Original Summary",
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            CommitCount: 5,
            PullRequestCount: 2,
            WorkItemCount: 3,
            SentTo: new List<NotificationChannel>());

        // Act
        var modified = original with
        {
            Summary = "Updated Summary",
            CommitCount = 10,
        };

        // Assert
        modified.Summary.Should().Be("Updated Summary");
        modified.CommitCount.Should().Be(10);
        modified.Id.Should().Be(original.Id);
        modified.PullRequestCount.Should().Be(original.PullRequestCount);
        modified.WorkItemCount.Should().Be(original.WorkItemCount);
    }

    [Fact]
    public void StandupReportDto_SentToMultipleChannels()
    {
        // Arrange
        var sentTo = new List<NotificationChannel>
        {
            NotificationChannel.TeamsChannel,
            NotificationChannel.Email,
            NotificationChannel.TeamsDirectMessage,
        };

        // Act
        var dto = new StandupReportDto(
            Id: "report-multi",
            Summary: "Summary",
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            CommitCount: 1,
            PullRequestCount: 1,
            WorkItemCount: 1,
            SentTo: sentTo);

        // Assert
        dto.SentTo.Should().HaveCount(3);
        dto.SentTo.Should().Contain(NotificationChannel.TeamsChannel);
        dto.SentTo.Should().Contain(NotificationChannel.Email);
        dto.SentTo.Should().Contain(NotificationChannel.TeamsDirectMessage);
    }

    [Fact]
    public void StandupReportDto_WithLargeActivityCounts()
    {
        // Arrange & Act
        var dto = new StandupReportDto(
            Id: "report-large",
            Summary: "Very productive day",
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            CommitCount: 100,
            PullRequestCount: 25,
            WorkItemCount: 50,
            SentTo: new List<NotificationChannel>());

        // Assert
        dto.CommitCount.Should().Be(100);
        dto.PullRequestCount.Should().Be(25);
        dto.WorkItemCount.Should().Be(50);
    }
}
