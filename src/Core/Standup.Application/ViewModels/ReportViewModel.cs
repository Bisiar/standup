using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for the Report view that displays generated standup reports
/// with collapsible sections for different summary types.
/// </summary>
public partial class ReportViewModel : ObservableObject
{
    private readonly ILocalStandupService _localStandupService;
    private readonly GroupService _groupService;
    private readonly IClipboardService _clipboardService;
    private readonly ReportHistoryService _reportHistoryService;

    /// <summary>
    /// Gets or sets the grouped report data containing all sections.
    /// </summary>
    [ObservableProperty]
    private GroupedStandupReportDto? _report;

    /// <summary>
    /// Gets or sets the group used to generate the report (needed for lazy generation).
    /// </summary>
    [ObservableProperty]
    private RepositoryGroup? _sourceGroup;

    /// <summary>
    /// Gets or sets the main executive summary content (for team standup).
    /// </summary>
    [ObservableProperty]
    private string _executiveSummary = string.Empty;

    /// <summary>
    /// Gets or sets the technical details content (lazy-loaded on expand).
    /// </summary>
    [ObservableProperty]
    private string _technicalDetails = string.Empty;

    /// <summary>
    /// Gets or sets the code review content (lazy-loaded on expand).
    /// </summary>
    [ObservableProperty]
    private string _codeReviewDetails = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the Technical section is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isTechnicalExpanded;

    /// <summary>
    /// Gets or sets a value indicating whether the Code Review section is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isCodeReviewExpanded;

    /// <summary>
    /// Gets or sets a value indicating whether Technical details have been generated.
    /// </summary>
    [ObservableProperty]
    private bool _hasTechnicalGenerated;

    /// <summary>
    /// Gets or sets a value indicating whether Code Review details have been generated.
    /// </summary>
    [ObservableProperty]
    private bool _hasCodeReviewGenerated;

    /// <summary>
    /// Gets or sets a value indicating whether Technical section is currently generating.
    /// </summary>
    [ObservableProperty]
    private bool _isGeneratingTechnical;

    /// <summary>
    /// Gets or sets a value indicating whether Code Review section is currently generating.
    /// </summary>
    [ObservableProperty]
    private bool _isGeneratingCodeReview;

    /// <summary>
    /// Gets or sets the status message for the report view.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets a value indicating whether there is report content to display.
    /// </summary>
    public bool HasReport => Report != null || !string.IsNullOrEmpty(ExecutiveSummary);

    /// <summary>
    /// Gets or sets the report title/header info.
    /// </summary>
    [ObservableProperty]
    private string _reportTitle = string.Empty;

    /// <summary>
    /// Gets or sets the report period display string.
    /// </summary>
    [ObservableProperty]
    private string _reportPeriod = string.Empty;

    /// <summary>
    /// Gets or sets the total commits in the report.
    /// </summary>
    [ObservableProperty]
    private int _totalCommits;

    /// <summary>
    /// Gets or sets the total PRs in the report.
    /// </summary>
    [ObservableProperty]
    private int _totalPRs;

    /// <summary>
    /// Gets or sets the total work items in the report.
    /// </summary>
    [ObservableProperty]
    private int _totalWorkItems;

    public ReportViewModel(
        ILocalStandupService localStandupService,
        GroupService groupService,
        IClipboardService clipboardService,
        ReportHistoryService reportHistoryService)
    {
        _localStandupService = localStandupService;
        _groupService = groupService;
        _clipboardService = clipboardService;
        _reportHistoryService = reportHistoryService;
    }

    /// <summary>
    /// Sets the report data after generation from StandupViewModel.
    /// </summary>
    /// <param name="report">The generated report data.</param>
    /// <param name="sourceGroup">The group used to generate the report.</param>
    public void SetReport(GroupedStandupReportDto report, RepositoryGroup sourceGroup)
    {
        Report = report;
        SourceGroup = sourceGroup;

        // Set header info
        ReportTitle = $"Standup Report - {sourceGroup.Name}";
        ReportPeriod = $"{report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}";
        TotalCommits = report.TotalCommits;
        TotalPRs = report.TotalPullRequests;
        TotalWorkItems = report.TotalWorkItems;

        // Build executive summary from the report
        ExecutiveSummary = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Reset collapsible sections
        IsTechnicalExpanded = false;
        IsCodeReviewExpanded = false;
        HasTechnicalGenerated = false;
        HasCodeReviewGenerated = false;
        TechnicalDetails = string.Empty;
        CodeReviewDetails = string.Empty;

        StatusMessage = $"Generated at {report.GeneratedAt:HH:mm}";

        OnPropertyChanged(nameof(HasReport));
    }

    /// <summary>
    /// Sets the report view from saved history (for viewing existing reports).
    /// </summary>
    /// <param name="history">The saved report history entry.</param>
    /// <param name="projectName">Optional project name for display.</param>
    public void SetReportFromHistory(ReportHistory history, string? projectName = null)
    {
        // We don't have the full report DTO, just the saved content
        Report = null;
        SourceGroup = null;

        // Set header info from history
        ReportTitle = $"Standup Report - {projectName ?? history.GroupName}";
        ReportPeriod = $"{history.PeriodStart:MMM dd} - {history.PeriodEnd:MMM dd, yyyy}";
        TotalCommits = history.TotalCommits;
        TotalPRs = history.TotalPullRequests;
        TotalWorkItems = history.TotalWorkItems;

        // Set the saved content as executive summary
        ExecutiveSummary = history.ReportContent;

        // Disable lazy loading sections since we don't have the source data
        IsTechnicalExpanded = false;
        IsCodeReviewExpanded = false;
        HasTechnicalGenerated = false;
        HasCodeReviewGenerated = false;
        TechnicalDetails = string.Empty;
        CodeReviewDetails = string.Empty;

        StatusMessage = $"Report from {history.GeneratedAt:MMM dd, HH:mm}";

        OnPropertyChanged(nameof(HasReport));
    }

    /// <summary>
    /// Clears the current report.
    /// </summary>
    [RelayCommand]
    private void ClearReport()
    {
        Report = null;
        SourceGroup = null;
        ExecutiveSummary = string.Empty;
        TechnicalDetails = string.Empty;
        CodeReviewDetails = string.Empty;
        IsTechnicalExpanded = false;
        IsCodeReviewExpanded = false;
        HasTechnicalGenerated = false;
        HasCodeReviewGenerated = false;
        StatusMessage = string.Empty;

        OnPropertyChanged(nameof(HasReport));
    }

    /// <summary>
    /// Toggles the Technical details section. Generates on first expand.
    /// </summary>
    [RelayCommand]
    private async Task ToggleTechnicalAsync()
    {
        IsTechnicalExpanded = !IsTechnicalExpanded;

        if (IsTechnicalExpanded && !HasTechnicalGenerated && Report != null && SourceGroup != null)
        {
            await GenerateSectionAsync(SummaryType.Technical);
        }
    }

    /// <summary>
    /// Toggles the Code Review section. Generates on first expand.
    /// </summary>
    [RelayCommand]
    private async Task ToggleCodeReviewAsync()
    {
        IsCodeReviewExpanded = !IsCodeReviewExpanded;

        if (IsCodeReviewExpanded && !HasCodeReviewGenerated && Report != null && SourceGroup != null)
        {
            await GenerateSectionAsync(SummaryType.CodeReview);
        }
    }

    private async Task GenerateSectionAsync(SummaryType summaryType)
    {
        if (Report == null || SourceGroup == null)
        {
            return;
        }

        try
        {
            if (summaryType == SummaryType.Technical)
            {
                IsGeneratingTechnical = true;
                StatusMessage = "Generating technical details...";
            }
            else if (summaryType == SummaryType.CodeReview)
            {
                IsGeneratingCodeReview = true;
                StatusMessage = "Generating code review...";
            }

            // Generate the new summary type using the same date range
            var newReport = await _localStandupService.GenerateGroupedStandupAsync(
                SourceGroup,
                async repo => await _groupService.GetDecryptedPatForRepositoryAsync(repo),
                Report.PeriodStart,
                Report.PeriodEnd,
                summaryType);

            // Build the markdown for this summary type
            var content = StandupReportFormatter.BuildGroupedReportMarkdown(newReport);

            if (summaryType == SummaryType.Technical)
            {
                TechnicalDetails = content;
                HasTechnicalGenerated = true;
                IsGeneratingTechnical = false;
            }
            else if (summaryType == SummaryType.CodeReview)
            {
                CodeReviewDetails = content;
                HasCodeReviewGenerated = true;
                IsGeneratingCodeReview = false;
            }

            StatusMessage = $"{summaryType} details generated.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating {summaryType}: {ex.Message}";

            if (summaryType == SummaryType.Technical)
            {
                IsGeneratingTechnical = false;
            }
            else if (summaryType == SummaryType.CodeReview)
            {
                IsGeneratingCodeReview = false;
            }
        }
    }

    /// <summary>
    /// Copies the executive summary to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyExecutiveSummaryAsync()
    {
        if (!string.IsNullOrEmpty(ExecutiveSummary))
        {
            await _clipboardService.SetTextAsync(ExecutiveSummary);
            StatusMessage = "Executive summary copied!";
        }
    }

    /// <summary>
    /// Copies all generated content to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyAllAsync()
    {
        var content = ExecutiveSummary;

        if (HasTechnicalGenerated && !string.IsNullOrEmpty(TechnicalDetails))
        {
            content += "\n\n---\n\n## Technical Details\n\n" + TechnicalDetails;
        }

        if (HasCodeReviewGenerated && !string.IsNullOrEmpty(CodeReviewDetails))
        {
            content += "\n\n---\n\n## Code Review\n\n" + CodeReviewDetails;
        }

        await _clipboardService.SetTextAsync(content);
        StatusMessage = "All content copied!";
    }

    /// <summary>
    /// Saves the report to history.
    /// </summary>
    [RelayCommand]
    private async Task SaveReportAsync()
    {
        if (Report == null)
        {
            StatusMessage = "No report to save.";
            return;
        }

        try
        {
            var saved = await _reportHistoryService.SaveReportAsync(Report, ExecutiveSummary);
            StatusMessage = $"Report saved ({saved.DisplaySummary})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }
}
