using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Domain.Tests.Entities;

public sealed class StandupReportTests
{
    [Fact]
    public void NewStandupReport_HasDefaultValues()
    {
        // Act
        var report = new StandupReport();

        // Assert
        report.Id.Should().NotBeNullOrEmpty();
        report.UserId.Should().BeEmpty();
        report.TenantId.Should().BeEmpty();
        report.Summary.Should().BeEmpty();
        report.RawData.Should().NotBeNull();
        report.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        report.SentTo.Should().BeEmpty();
        report.User.Should().BeNull();
    }

    [Fact]
    public void StandupReport_CanSetProperties()
    {
        // Arrange
        var report = new StandupReport();
        var now = DateTimeOffset.UtcNow;
        var periodStart = now.AddDays(-1);
        var periodEnd = now;

        // Act
        report.UserId = "user-123";
        report.TenantId = "tenant-456";
        report.Summary = "Worked on feature X, fixed bug Y";
        report.PeriodStart = periodStart;
        report.PeriodEnd = periodEnd;

        // Assert
        report.UserId.Should().Be("user-123");
        report.TenantId.Should().Be("tenant-456");
        report.Summary.Should().Be("Worked on feature X, fixed bug Y");
        report.PeriodStart.Should().Be(periodStart);
        report.PeriodEnd.Should().Be(periodEnd);
    }

    [Fact]
    public void StandupReport_CanTrackSentChannels()
    {
        // Arrange
        var report = new StandupReport();

        // Act
        report.SentTo.Add(NotificationChannel.Teams);
        report.SentTo.Add(NotificationChannel.Email);

        // Assert
        report.SentTo.Should().HaveCount(2);
        report.SentTo.Should().Contain(NotificationChannel.Teams);
        report.SentTo.Should().Contain(NotificationChannel.Email);
    }

    [Fact]
    public void StandupReport_CanHaveUser()
    {
        // Arrange
        var user = new User { DisplayName = "John Doe" };
        var report = new StandupReport { Summary = "Did some work" };

        // Act
        report.User = user;
        report.UserId = user.Id;

        // Assert
        report.User.Should().Be(user);
        report.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void StandupReport_CanHaveRawData()
    {
        // Arrange
        var commits = new List<CommitInfo>
        {
            new("sha1", "feat: add feature", "john", DateTimeOffset.UtcNow, "repo1"),
            new("sha2", "fix: fix bug", "john", DateTimeOffset.UtcNow, "repo1")
        };

        var pullRequests = new List<PullRequestInfo>
        {
            new(1, "Add feature", "merged", "john", DateTimeOffset.UtcNow, "repo1", "https://github.com/org/repo/pull/1")
        };

        var rawData = new StandupData(commits, pullRequests, new List<WorkItemInfo>());
        var report = new StandupReport();

        // Act
        report.RawData = rawData;

        // Assert
        report.RawData.Should().Be(rawData);
        report.RawData.Commits.Should().HaveCount(2);
        report.RawData.PullRequests.Should().HaveCount(1);
    }

    [Fact]
    public void StandupReport_GeneratesUniqueIds()
    {
        // Arrange & Act
        var report1 = new StandupReport();
        var report2 = new StandupReport();

        // Assert
        report1.Id.Should().NotBe(report2.Id);
    }
}
