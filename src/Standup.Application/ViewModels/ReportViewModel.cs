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
    /// The grouped report data containing all sections.
    /// </summary>
    [ObservableProperty]
    private GroupedStandupReportDto? _report;

    /// <summary>
    /// The group used to generate the report (needed for lazy generation).
    /// </summary>
    [ObservableProperty]
    private RepositoryGroup? _sourceGroup;

    /// <summary>
    /// Main executive summary content (for team standup).
    /// </summary>
    [ObservableProperty]
    private string _executiveSummary = string.Empty;

    /// <summary>
    /// Technical details content (lazy-loaded on expand).
    /// </summary>
    [ObservableProperty]
    private string _technicalDetails = string.Empty;

    /// <summary>
    /// Code review content (lazy-loaded on expand).
    /// </summary>
    [ObservableProperty]
    private string _codeReviewDetails = string.Empty;

    /// <summary>
    /// Whether the Technical section is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isTechnicalExpanded;

    /// <summary>
    /// Whether the Code Review section is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isCodeReviewExpanded;

    /// <summary>
    /// Whether Technical details have been generated.
    /// </summary>
    [ObservableProperty]
    private bool _hasTechnicalGenerated;

    /// <summary>
    /// Whether Code Review details have been generated.
    /// </summary>
    [ObservableProperty]
    private bool _hasCodeReviewGenerated;

    /// <summary>
    /// Whether Technical section is currently generating.
    /// </summary>
    [ObservableProperty]
    private bool _isGeneratingTechnical;

    /// <summary>
    /// Whether Code Review section is currently generating.
    /// </summary>
    [ObservableProperty]
    private bool _isGeneratingCodeReview;

    /// <summary>
    /// Status message for the report view.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Whether there is a report to display.
    /// </summary>
    public bool HasReport => Report != null;

    /// <summary>
    /// Report title/header info.
    /// </summary>
    [ObservableProperty]
    private string _reportTitle = string.Empty;

    /// <summary>
    /// Report period display string.
    /// </summary>
    [ObservableProperty]
    private string _reportPeriod = string.Empty;

    /// <summary>
    /// Total commits in the report.
    /// </summary>
    [ObservableProperty]
    private int _totalCommits;

    /// <summary>
    /// Total PRs in the report.
    /// </summary>
    [ObservableProperty]
    private int _totalPRs;

    /// <summary>
    /// Total work items in the report.
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
