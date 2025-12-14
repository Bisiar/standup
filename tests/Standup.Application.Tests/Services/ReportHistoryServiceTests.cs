using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Application.Services;
using Standup.Application.Tests.TestHelpers;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for ReportHistoryService - manages report history persistence.
/// NO MOCKS - uses real test implementations.
/// </summary>
public sealed class ReportHistoryServiceTests
{
    private readonly InMemoryReportHistoryRepository _repository;
    private readonly ReportHistoryService _service;

    public ReportHistoryServiceTests()
    {
        _repository = new InMemoryReportHistoryRepository();
        _service = new ReportHistoryService(_repository);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoReports()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveReportAsync_CreatesReportHistory()
    {
        // Arrange
        var sections = new List<ClientCodeSection>
        {
            new("CLIENT1", "Summary 1", new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()),
            new("CLIENT2", "Summary 2", new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>())
        };

        var reportDto = new GroupedStandupReportDto(
            Id: "group-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 10,
            TotalPullRequests: 5,
            TotalWorkItems: 3,
            CurrentSummaryType: SummaryType.Technical);

        var reportContent = "Test report content";

        // Act
        var result = await _service.SaveReportAsync(reportDto, reportContent);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.GroupId.Should().Be("group-1");
        result.GroupName.Should().Be("Test Group");
        result.PeriodStart.Should().Be(reportDto.PeriodStart);
        result.PeriodEnd.Should().Be(reportDto.PeriodEnd);
        result.SummaryType.Should().Be(SummaryType.Technical);
        result.TotalCommits.Should().Be(10);
        result.TotalPullRequests.Should().Be(5);
        result.TotalWorkItems.Should().Be(3);
        result.ReportContent.Should().Be("Test report content");
        result.ClientCodes.Should().BeEquivalentTo(new[] { "CLIENT1", "CLIENT2" });
        result.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsReport_WhenExists()
    {
        // Arrange
        var reportDto = CreateTestReportDto("group-1");
        var saved = await _service.SaveReportAsync(reportDto, "content");

        // Act
        var result = await _service.GetByIdAsync(saved.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(saved.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _service.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByGroupIdAsync_ReturnsReportsForGroup()
    {
        // Arrange
        var group1Report1 = await _service.SaveReportAsync(
            CreateTestReportDto("group-1"), "content1");
        var group1Report2 = await _service.SaveReportAsync(
            CreateTestReportDto("group-1"), "content2");
        var group2Report = await _service.SaveReportAsync(
            CreateTestReportDto("group-2"), "content3");

        // Act
        var result = await _service.GetByGroupIdAsync("group-1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Id == group1Report1.Id);
        result.Should().Contain(r => r.Id == group1Report2.Id);
        result.Should().NotContain(r => r.Id == group2Report.Id);
    }

    [Fact]
    public async Task GetByGroupIdAsync_ReturnsEmpty_WhenNoReportsForGroup()
    {
        // Arrange
        await _service.SaveReportAsync(CreateTestReportDto("group-1"), "content");

        // Act
        var result = await _service.GetByGroupIdAsync("group-2");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByGroupIdAsync_ReturnsReportsOrderedByGeneratedAtDescending()
    {
        // Arrange - Create reports with different timestamps
        var dto = CreateTestReportDto("group-1");

        var report1 = await _service.SaveReportAsync(dto, "content1");
        await Task.Delay(10); // Small delay to ensure different timestamps

        var report2 = await _service.SaveReportAsync(dto, "content2");
        await Task.Delay(10);

        var report3 = await _service.SaveReportAsync(dto, "content3");

        // Act
        var result = (await _service.GetByGroupIdAsync("group-1")).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be(report3.Id); // Most recent first
        result[1].Id.Should().Be(report2.Id);
        result[2].Id.Should().Be(report1.Id);
    }

    [Fact]
    public async Task GetLatestByGroupIdAsync_ReturnsNewestReport()
    {
        // Arrange
        var dto = CreateTestReportDto("group-1");

        var report1 = await _service.SaveReportAsync(dto, "content1");
        await Task.Delay(10);

        var report2 = await _service.SaveReportAsync(dto, "content2");
        await Task.Delay(10);

        var report3 = await _service.SaveReportAsync(dto, "content3");

        // Act
        var result = await _service.GetLatestByGroupIdAsync("group-1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(report3.Id);
    }

    [Fact]
    public async Task GetLatestByGroupIdAsync_ReturnsNull_WhenNoReportsForGroup()
    {
        // Act
        var result = await _service.GetLatestByGroupIdAsync("non-existent-group");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesReport()
    {
        // Arrange
        var saved = await _service.SaveReportAsync(
            CreateTestReportDto("group-1"), "content");

        // Act
        await _service.DeleteAsync(saved.Id);

        // Assert
        var result = await _service.GetByIdAsync(saved.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByGroupIdAsync_RemovesAllReportsForGroup()
    {
        // Arrange
        await _service.SaveReportAsync(CreateTestReportDto("group-1"), "content1");
        await _service.SaveReportAsync(CreateTestReportDto("group-1"), "content2");
        await _service.SaveReportAsync(CreateTestReportDto("group-2"), "content3");

        // Act
        await _service.DeleteByGroupIdAsync("group-1");

        // Assert
        var group1Reports = await _service.GetByGroupIdAsync("group-1");
        group1Reports.Should().BeEmpty();

        var group2Reports = await _service.GetByGroupIdAsync("group-2");
        group2Reports.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllReports()
    {
        // Arrange
        await _service.SaveReportAsync(CreateTestReportDto("group-1"), "content1");
        await _service.SaveReportAsync(CreateTestReportDto("group-1"), "content2");
        await _service.SaveReportAsync(CreateTestReportDto("group-2"), "content3");

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(SummaryType.Technical)]
    [InlineData(SummaryType.Executive)]
    [InlineData(SummaryType.CodeReview)]
    public async Task SaveReportAsync_SupportsDifferentSummaryTypes(SummaryType summaryType)
    {
        // Arrange
        var dto = CreateTestReportDto("group-1", summaryType);

        // Act
        var result = await _service.SaveReportAsync(dto, "content");

        // Assert
        result.SummaryType.Should().Be(summaryType);
    }

    [Fact]
    public async Task SaveReportAsync_HandlesEmptyClientCodes()
    {
        // Arrange
        var dto = new GroupedStandupReportDto(
            Id: "group-1",
            GroupName: "Test Group",
            Sections: new List<ClientCodeSection>(),
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);

        // Act
        var result = await _service.SaveReportAsync(dto, "content");

        // Assert
        result.ClientCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveReportAsync_HandlesZeroCounts()
    {
        // Arrange
        var dto = new GroupedStandupReportDto(
            Id: "group-1",
            GroupName: "Test Group",
            Sections: new List<ClientCodeSection>(),
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);

        // Act
        var result = await _service.SaveReportAsync(dto, "content");

        // Assert
        result.TotalCommits.Should().Be(0);
        result.TotalPullRequests.Should().Be(0);
        result.TotalWorkItems.Should().Be(0);
    }

    [Fact]
    public async Task SaveReportAsync_HandlesMultipleClientCodes()
    {
        // Arrange
        var sections = new List<ClientCodeSection>
        {
            new("CLIENT1", null, new(), new(), new()),
            new("CLIENT2", null, new(), new(), new()),
            new("CLIENT3", null, new(), new(), new()),
            new("CLIENT4", null, new(), new(), new())
        };

        var dto = new GroupedStandupReportDto(
            Id: "group-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);

        // Act
        var result = await _service.SaveReportAsync(dto, "content");

        // Assert
        result.ClientCodes.Should().HaveCount(4);
        result.ClientCodes.Should().BeEquivalentTo(
            new[] { "CLIENT1", "CLIENT2", "CLIENT3", "CLIENT4" });
    }

    private static GroupedStandupReportDto CreateTestReportDto(
        string groupId,
        SummaryType summaryType = SummaryType.Technical)
    {
        var sections = new List<ClientCodeSection>
        {
            new("TEST", "Test summary", new(), new(), new())
        };

        return new GroupedStandupReportDto(
            Id: groupId,
            GroupName: $"Group {groupId}",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddHours(-24),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 5,
            TotalPullRequests: 2,
            TotalWorkItems: 1,
            CurrentSummaryType: summaryType);
    }
}
