// <copyright file="ReportViewModelTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using NSubstitute;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="ReportViewModel"/> methods and behavior.
/// Uses NSubstitute for mocking dependencies to test the ViewModel in isolation.
/// </summary>
public sealed class ReportViewModelTests
{
    private readonly ILocalStandupService _localStandupService;
    private readonly GroupService _groupService;
    private readonly IClipboardService _clipboardService;
    private readonly ReportHistoryService _reportHistoryService;
    private readonly ReportViewModel _viewModel;

    public ReportViewModelTests()
    {
        _localStandupService = Substitute.For<ILocalStandupService>();
        _clipboardService = Substitute.For<IClipboardService>();

        // Create GroupService with substitutes
        var groupRepo = Substitute.For<IGroupRepository>();
        var credRepo = Substitute.For<ICredentialRepository>();
        var encryptionService = Substitute.For<IEncryptionService>();
        _groupService = new GroupService(groupRepo, credRepo, encryptionService);

        // Create ReportHistoryService with substitute
        var reportHistoryRepo = Substitute.For<IReportHistoryRepository>();
        _reportHistoryService = new ReportHistoryService(reportHistoryRepo);

        _viewModel = new ReportViewModel(
            _localStandupService,
            _groupService,
            _clipboardService,
            _reportHistoryService);
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_AllPropertiesHaveDefaults()
    {
        // Assert
        _viewModel.Report.Should().BeNull();
        _viewModel.SourceGroup.Should().BeNull();
        _viewModel.ExecutiveSummary.Should().BeEmpty();
        _viewModel.TechnicalDetails.Should().BeEmpty();
        _viewModel.CodeReviewDetails.Should().BeEmpty();
        _viewModel.IsTechnicalExpanded.Should().BeFalse();
        _viewModel.IsCodeReviewExpanded.Should().BeFalse();
        _viewModel.HasTechnicalGenerated.Should().BeFalse();
        _viewModel.HasCodeReviewGenerated.Should().BeFalse();
        _viewModel.IsGeneratingTechnical.Should().BeFalse();
        _viewModel.IsGeneratingCodeReview.Should().BeFalse();
        _viewModel.StatusMessage.Should().BeEmpty();
        _viewModel.HasReport.Should().BeFalse();
    }

    #endregion

    #region SetReport Tests

    [Fact]
    public void SetReport_WithValidReport_SetsAllProperties()
    {
        // Arrange
        var report = CreateTestReport("Test Group", 5, 2, 3);
        var group = new RepositoryGroup { Id = "group-1", Name = "Test Group" };

        // Act
        _viewModel.SetReport(report, group);

        // Assert
        _viewModel.Report.Should().Be(report);
        _viewModel.SourceGroup.Should().Be(group);
        _viewModel.ReportTitle.Should().Be("Standup Report - Test Group");
        _viewModel.ReportPeriod.Should().Contain("-");
        _viewModel.TotalCommits.Should().Be(5);
        _viewModel.TotalPRs.Should().Be(2);
        _viewModel.TotalWorkItems.Should().Be(3);
        _viewModel.HasReport.Should().BeTrue();
        _viewModel.ExecutiveSummary.Should().NotBeEmpty();
    }

    [Fact]
    public void SetReport_ResetsCollapsibleSections()
    {
        // Arrange - first set some state
        var report1 = CreateTestReport("Group 1", 1, 0, 0);
        var group1 = new RepositoryGroup { Id = "group-1", Name = "Group 1" };
        _viewModel.SetReport(report1, group1);

        // Simulate expanded sections
        // Note: We can't directly set these as they're private, but we can verify they reset

        var report2 = CreateTestReport("Group 2", 2, 1, 0);
        var group2 = new RepositoryGroup { Id = "group-2", Name = "Group 2" };

        // Act
        _viewModel.SetReport(report2, group2);

        // Assert - sections should be reset
        _viewModel.IsTechnicalExpanded.Should().BeFalse();
        _viewModel.IsCodeReviewExpanded.Should().BeFalse();
        _viewModel.HasTechnicalGenerated.Should().BeFalse();
        _viewModel.HasCodeReviewGenerated.Should().BeFalse();
        _viewModel.TechnicalDetails.Should().BeEmpty();
        _viewModel.CodeReviewDetails.Should().BeEmpty();
    }

    [Fact]
    public void SetReport_SetsStatusMessage()
    {
        // Arrange
        var report = CreateTestReport("Test Group", 1, 0, 0);
        var group = new RepositoryGroup { Id = "group-1", Name = "Test Group" };

        // Act
        _viewModel.SetReport(report, group);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Generated at");
    }

    #endregion

    #region SetReportFromHistory Tests

    [Fact]
    public void SetReportFromHistory_SetsPropertiesFromHistory()
    {
        // Arrange
        var history = new ReportHistory
        {
            Id = "history-1",
            GroupId = "group-1",
            GroupName = "Test Group",
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd = DateTimeOffset.UtcNow,
            GeneratedAt = DateTimeOffset.UtcNow.AddHours(-1),
            TotalCommits = 10,
            TotalPullRequests = 5,
            TotalWorkItems = 3,
            ReportContent = "# Test Report\n\nThis is a test report."
        };

        // Act
        _viewModel.SetReportFromHistory(history, "Custom Project");

        // Assert
        _viewModel.Report.Should().BeNull(); // No full DTO
        _viewModel.SourceGroup.Should().BeNull();
        _viewModel.ReportTitle.Should().Be("Standup Report - Custom Project");
        _viewModel.TotalCommits.Should().Be(10);
        _viewModel.TotalPRs.Should().Be(5);
        _viewModel.TotalWorkItems.Should().Be(3);
        _viewModel.ExecutiveSummary.Should().Be("# Test Report\n\nThis is a test report.");
        _viewModel.HasReport.Should().BeTrue();
    }

    [Fact]
    public void SetReportFromHistory_WithoutProjectName_UsesGroupName()
    {
        // Arrange
        var history = new ReportHistory
        {
            Id = "history-1",
            GroupId = "group-1",
            GroupName = "My Group",
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd = DateTimeOffset.UtcNow,
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalCommits = 1,
            TotalPullRequests = 0,
            TotalWorkItems = 0,
            ReportContent = "Content"
        };

        // Act
        _viewModel.SetReportFromHistory(history);

        // Assert
        _viewModel.ReportTitle.Should().Be("Standup Report - My Group");
    }

    [Fact]
    public void SetReportFromHistory_DisablesLazyLoading()
    {
        // Arrange
        var history = new ReportHistory
        {
            Id = "history-1",
            GroupId = "group-1",
            GroupName = "Test",
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd = DateTimeOffset.UtcNow,
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalCommits = 1,
            TotalPullRequests = 0,
            TotalWorkItems = 0,
            ReportContent = "Content"
        };

        // Act
        _viewModel.SetReportFromHistory(history);

        // Assert - sections should be reset and generation flags cleared
        _viewModel.IsTechnicalExpanded.Should().BeFalse();
        _viewModel.IsCodeReviewExpanded.Should().BeFalse();
        _viewModel.HasTechnicalGenerated.Should().BeFalse();
        _viewModel.HasCodeReviewGenerated.Should().BeFalse();
    }

    #endregion

    #region ClearReport Tests

    [Fact]
    public void ClearReport_ResetsAllProperties()
    {
        // Arrange - set up a report first
        var report = CreateTestReport("Test Group", 5, 2, 3);
        var group = new RepositoryGroup { Id = "group-1", Name = "Test Group" };
        _viewModel.SetReport(report, group);
        _viewModel.HasReport.Should().BeTrue();

        // Act
        _viewModel.ClearReportCommand.Execute(null);

        // Assert
        _viewModel.Report.Should().BeNull();
        _viewModel.SourceGroup.Should().BeNull();
        _viewModel.ExecutiveSummary.Should().BeEmpty();
        _viewModel.TechnicalDetails.Should().BeEmpty();
        _viewModel.CodeReviewDetails.Should().BeEmpty();
        _viewModel.IsTechnicalExpanded.Should().BeFalse();
        _viewModel.IsCodeReviewExpanded.Should().BeFalse();
        _viewModel.HasTechnicalGenerated.Should().BeFalse();
        _viewModel.HasCodeReviewGenerated.Should().BeFalse();
        _viewModel.StatusMessage.Should().BeEmpty();
        _viewModel.HasReport.Should().BeFalse();
    }

    #endregion

    #region CopyExecutiveSummary Tests

    [Fact]
    public async Task CopyExecutiveSummaryAsync_WithContent_CopiesAndSetsStatus()
    {
        // Arrange
        var report = CreateTestReport("Test Group", 1, 0, 0);
        var group = new RepositoryGroup { Id = "group-1", Name = "Test Group" };
        _viewModel.SetReport(report, group);

        // Act
        await _viewModel.CopyExecutiveSummaryCommand.ExecuteAsync(null);

        // Assert
        await _clipboardService.Received(1).SetTextAsync(Arg.Is<string>(s => !string.IsNullOrEmpty(s)));
        _viewModel.StatusMessage.Should().Contain("copied");
    }

    [Fact]
    public async Task CopyExecutiveSummaryAsync_WithNoContent_DoesNotCopy()
    {
        // Arrange - no report set, so ExecutiveSummary is empty

        // Act
        await _viewModel.CopyExecutiveSummaryCommand.ExecuteAsync(null);

        // Assert
        await _clipboardService.DidNotReceive().SetTextAsync(Arg.Any<string>());
    }

    #endregion

    #region CopyAll Tests

    [Fact]
    public async Task CopyAllAsync_WithOnlyExecutiveSummary_CopiesExecutive()
    {
        // Arrange
        var report = CreateTestReport("Test Group", 1, 0, 0);
        var group = new RepositoryGroup { Id = "group-1", Name = "Test Group" };
        _viewModel.SetReport(report, group);

        // Act
        await _viewModel.CopyAllCommand.ExecuteAsync(null);

        // Assert
        await _clipboardService.Received(1).SetTextAsync(Arg.Is<string>(s => s.Contains("Standup Report")));
        _viewModel.StatusMessage.Should().Contain("All content copied");
    }

    #endregion

    #region ToggleTechnical Tests

    [Fact]
    public async Task ToggleTechnicalAsync_TogglesExpansionState()
    {
        // Arrange
        _viewModel.IsTechnicalExpanded.Should().BeFalse();

        // Act
        await _viewModel.ToggleTechnicalCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsTechnicalExpanded.Should().BeTrue();

        // Toggle again
        await _viewModel.ToggleTechnicalCommand.ExecuteAsync(null);
        _viewModel.IsTechnicalExpanded.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleTechnicalAsync_WithNoReportOrGroup_DoesNotGenerate()
    {
        // Arrange - no report set

        // Act
        await _viewModel.ToggleTechnicalCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsTechnicalExpanded.Should().BeTrue();
        _viewModel.HasTechnicalGenerated.Should().BeFalse();
        _viewModel.TechnicalDetails.Should().BeEmpty();
    }

    #endregion

    #region ToggleCodeReview Tests

    [Fact]
    public async Task ToggleCodeReviewAsync_TogglesExpansionState()
    {
        // Arrange
        _viewModel.IsCodeReviewExpanded.Should().BeFalse();

        // Act
        await _viewModel.ToggleCodeReviewCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsCodeReviewExpanded.Should().BeTrue();

        // Toggle again
        await _viewModel.ToggleCodeReviewCommand.ExecuteAsync(null);
        _viewModel.IsCodeReviewExpanded.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleCodeReviewAsync_WithNoReportOrGroup_DoesNotGenerate()
    {
        // Arrange - no report set

        // Act
        await _viewModel.ToggleCodeReviewCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsCodeReviewExpanded.Should().BeTrue();
        _viewModel.HasCodeReviewGenerated.Should().BeFalse();
        _viewModel.CodeReviewDetails.Should().BeEmpty();
    }

    #endregion

    #region SaveReport Tests

    [Fact]
    public async Task SaveReportAsync_WithNoReport_SetsErrorMessage()
    {
        // Arrange - no report set

        // Act
        await _viewModel.SaveReportCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("No report to save");
    }

    #endregion

    #region HasReport Tests

    [Fact]
    public void HasReport_WithReport_ReturnsTrue()
    {
        // Arrange
        var report = CreateTestReport("Test", 1, 0, 0);
        var group = new RepositoryGroup { Id = "g1", Name = "Test" };

        // Act
        _viewModel.SetReport(report, group);

        // Assert
        _viewModel.HasReport.Should().BeTrue();
    }

    [Fact]
    public void HasReport_WithExecutiveSummaryOnly_ReturnsTrue()
    {
        // Arrange
        var history = new ReportHistory
        {
            Id = "h1",
            GroupId = "g1",
            GroupName = "Test",
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd = DateTimeOffset.UtcNow,
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalCommits = 1,
            TotalPullRequests = 0,
            TotalWorkItems = 0,
            ReportContent = "Some content"
        };

        // Act
        _viewModel.SetReportFromHistory(history);

        // Assert
        _viewModel.Report.Should().BeNull();
        _viewModel.ExecutiveSummary.Should().NotBeEmpty();
        _viewModel.HasReport.Should().BeTrue();
    }

    [Fact]
    public void HasReport_WithNoReportOrSummary_ReturnsFalse()
    {
        // Assert
        _viewModel.HasReport.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static GroupedStandupReportDto CreateTestReport(
        string groupName,
        int commits,
        int prs,
        int workItems)
    {
        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "TEST",
                Summary: "Test summary content",
                Commits: Enumerable.Range(0, commits)
                    .Select(i => new CommitInfo(
                        Sha: $"sha{i}",
                        Message: $"Commit message {i}",
                        Repository: "test-repo",
                        SourceType: SourceType.AzureDevOps,
                        CommittedAt: DateTimeOffset.UtcNow.AddHours(-i),
                        Author: "Test Author"))
                    .ToList(),
                PullRequests: Enumerable.Range(0, prs)
                    .Select(i => new PullRequestInfo(
                        Id: i.ToString(),
                        Title: $"PR Title {i}",
                        Repository: "test-repo",
                        SourceType: SourceType.AzureDevOps,
                        Status: "Active",
                        Url: $"https://dev.azure.com/pr/{i}",
                        CreatedAt: DateTimeOffset.UtcNow.AddDays(-i)))
                    .ToList(),
                WorkItems: Enumerable.Range(0, workItems)
                    .Select(i => new WorkItemInfo(
                        Id: i.ToString(),
                        Title: $"Work Item {i}",
                        Type: "Task",
                        Status: WorkItemStatus.Active,
                        SourceType: SourceType.AzureDevOps,
                        Url: $"https://dev.azure.com/wi/{i}",
                        AssignedTo: "Test User"))
                    .ToList())
        };

        return new GroupedStandupReportDto(
            Id: Guid.NewGuid().ToString(),
            GroupName: groupName,
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: commits,
            TotalPullRequests: prs,
            TotalWorkItems: workItems);
    }

    #endregion
}
