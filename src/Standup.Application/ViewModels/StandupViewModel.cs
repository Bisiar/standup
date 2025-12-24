using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Application.ViewModels;

public partial class StandupViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IStandupApiClient _apiClient;
    private readonly ILocalStandupService _localStandupService;
    private readonly IClipboardService _clipboardService;
    private readonly GroupService _groupService;
    private readonly ReportHistoryService _reportHistoryService;

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private StandupReportDto? _latestReport;

    [ObservableProperty]
    private GroupedStandupReportDto? _groupedReport;

    [ObservableProperty]
    private ObservableCollection<RepositoryGroup> _groups = new();

    [ObservableProperty]
    private RepositoryGroup? _selectedGroup;

    [ObservableProperty]
    private DateTimeOffset _periodStart = DateTimeOffset.UtcNow.AddDays(-1);

    [ObservableProperty]
    private DateTimeOffset _periodEnd = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the selected period preset (0=Yesterday, 1=Today, 2=Week).
    /// </summary>
    [ObservableProperty]
    private int _selectedPeriod;

    [ObservableProperty]
    private bool _useGroupMode = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate Executive summary (business-focused, for stakeholders).
    /// </summary>
    [ObservableProperty]
    private bool _generateExecutive = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate Technical summary (developer-focused, code details).
    /// </summary>
    [ObservableProperty]
    private bool _generateTechnical;

    /// <summary>
    /// Gets or sets a value indicating whether to generate Code Review summary (security, quality, testing).
    /// </summary>
    [ObservableProperty]
    private bool _generateCodeReview;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private double _generationProgress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _reportContent = string.Empty;

    /// <summary>
    /// Gets or sets the HTML-formatted report content for WebView display.
    /// </summary>
    [ObservableProperty]
    private string _htmlReportContent = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to display the report as rendered HTML (true) or raw markdown (false).
    /// Defaults to true for better visual presentation.
    /// </summary>
    [ObservableProperty]
    private bool _showAsHtml = true;

    /// <summary>
    /// Gets or sets the current view mode within the Standup tab.
    /// 0 = Config (group selection), 1 = Report summary, 2 = Commits, 3 = PRs, 4 = Work Items.
    /// </summary>
    [ObservableProperty]
    private int _currentViewMode;

    /// <summary>
    /// Gets a value indicating whether we have a report generated (to show internal tabs).
    /// </summary>
    public bool HasReport => GroupedReport != null;

    /// <summary>
    /// Gets a formatted summary of repos in the selected group (e.g., "UPREHS/AI-Chat-Bot, JT-Ops/jt-azr").
    /// </summary>
    public string SelectedGroupReposSummary => SelectedGroup?.Repositories
        .Select(r => string.IsNullOrWhiteSpace(r.ClientCode) ? r.Repository : $"{r.ClientCode}/{r.Repository}")
        .DefaultIfEmpty("No repositories")
        .Aggregate((a, b) => $"{a}, {b}") ?? "No group selected";

    partial void OnSelectedGroupChanged(RepositoryGroup? value)
    {
        OnPropertyChanged(nameof(SelectedGroupReposSummary));
    }

    /// <summary>
    /// Gets all commits from the current report, flattened across all sections.
    /// </summary>
    public IReadOnlyList<CommitInfo> AllCommits => GroupedReport?.Sections
        .SelectMany(s => s.Commits)
        .OrderByDescending(c => c.CommittedAt)
        .ToList() ?? [];

    /// <summary>
    /// Gets all pull requests from the current report, flattened across all sections.
    /// </summary>
    public IReadOnlyList<PullRequestInfo> AllPullRequests => GroupedReport?.Sections
        .SelectMany(s => s.PullRequests)
        .ToList() ?? [];

    /// <summary>
    /// Gets all work items from the current report, flattened across all sections.
    /// </summary>
    public IReadOnlyList<WorkItemInfo> AllWorkItems => GroupedReport?.Sections
        .SelectMany(s => s.WorkItems)
        .ToList() ?? [];

    [ObservableProperty]
    private ObservableCollection<ReportHistory> _reportHistory = new();

    [ObservableProperty]
    private ReportHistory? _selectedHistoryItem;

    public StandupViewModel(
        IProjectService projectService,
        IStandupApiClient apiClient,
        ILocalStandupService localStandupService,
        IClipboardService clipboardService,
        GroupService groupService,
        ReportHistoryService reportHistoryService)
    {
        _projectService = projectService;
        _apiClient = apiClient;
        _localStandupService = localStandupService;
        _clipboardService = clipboardService;
        _groupService = groupService;
        _reportHistoryService = reportHistoryService;
    }

    /// <summary>
    /// Gets which summary types are available in the current report.
    /// </summary>
    /// <returns>Available summary types in the current report.</returns>
    public IEnumerable<SummaryType> GetAvailableSummaryTypesInReport()
    {
        if (GroupedReport == null)
        {
            return [];
        }

        return GroupedReport.Sections
            .Where(s => s.AllSummaries != null)
            .SelectMany(s => s.AllSummaries!.Keys)
            .Distinct()
            .OrderBy(t => t);
    }

    /// <summary>
    /// Called when a repository's IncludeInGeneration property changes (from Switch binding).
    /// Saves the group and triggers on-the-fly generation if needed.
    /// </summary>
    /// <param name="repo">The repository whose inclusion changed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnRepoInclusionChangedAsync(GroupedRepository? repo)
    {
        if (SelectedGroup == null || repo == null)
        {
            return;
        }

        await _groupService.UpdateGroupAsync(SelectedGroup);
        OnPropertyChanged(nameof(SelectedGroup));
        OnPropertyChanged(nameof(SelectedGroupReposSummary));
        if (repo.IncludeInGeneration && HasReport && GroupedReport != null)
        {
            await GenerateRepoOnTheFlyAsync(repo);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // Load groups
            var groups = await _groupService.GetGroupsAsync();
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            // Set selected group to default
            SelectedGroup = await _groupService.GetDefaultGroupAsync() ?? Groups.FirstOrDefault();

            // Also load legacy project for non-group mode
            CurrentProject = await _projectService.GetCurrentProjectAsync();

            if (CurrentProject != null && !CurrentProject.UseLocalGeneration)
            {
                _apiClient.SetProject(CurrentProject.ApiEndpoint, CurrentProject.AccessToken);
            }

            // Default to group mode if we have groups
            UseGroupMode = Groups.Any();

            // Load report history
            await LoadHistoryAsync();

            // Set smart date default based on last report for selected group
            await SetSmartDateDefaultsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _reportHistoryService.GetAllAsync();
        ReportHistory.Clear();
        foreach (var item in history.OrderByDescending(h => h.GeneratedAt))
        {
            ReportHistory.Add(item);
        }
    }

    private async Task SetSmartDateDefaultsAsync()
    {
        if (SelectedGroup == null)
        {
            return;
        }

        var lastReport = await _reportHistoryService.GetLatestByGroupIdAsync(SelectedGroup.Id);
        if (lastReport != null)
        {
            // Start from where the last report ended
            PeriodStart = lastReport.PeriodEnd;
            PeriodEnd = DateTimeOffset.UtcNow;
        }
    }

    [RelayCommand]
    private void SetPeriodYesterday() => (PeriodStart, PeriodEnd, SelectedPeriod) = (DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, 0);

    [RelayCommand]
    private void SetPeriodToday() => (PeriodStart, PeriodEnd, SelectedPeriod) = (DateTimeOffset.UtcNow.Date, DateTimeOffset.UtcNow, 1);

    [RelayCommand]
    private void SetPeriodWeek() => (PeriodStart, PeriodEnd, SelectedPeriod) = (DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, 2);

    [RelayCommand]
    private void SelectGroup(RepositoryGroup group) => SelectedGroup = group;

    [RelayCommand]
    private async Task ToggleRepoInclusionAsync(GroupedRepository? repo)
    {
        if (SelectedGroup == null || repo == null)
        {
            return;
        }

        repo.IncludeInGeneration = !repo.IncludeInGeneration;
        await OnRepoInclusionChangedAsync(repo);
    }

    private async Task GenerateRepoOnTheFlyAsync(GroupedRepository repo)
    {
        IsGenerating = true;
        StatusMessage = $"Generating for {repo.ClientCode}/{repo.Repository}...";

        try
        {
            var tempGroup = new RepositoryGroup
            {
                Id = "temp-single-repo",
                Name = "Temp",
                Repositories = new List<GroupedRepository> { repo },
            };

            var summaryTypes = GetAvailableSummaryTypesInReport().ToList();
            if (summaryTypes.Count == 0)
            {
                summaryTypes = GetSelectedSummaryTypes().ToList();
            }

            var singleRepoReport = await _localStandupService.GenerateGroupedStandupAsync(
                tempGroup,
                async r => await _groupService.GetDecryptedPatForRepositoryAsync(r),
                PeriodStart,
                PeriodEnd,
                summaryTypes.FirstOrDefault());

            foreach (var summaryType in summaryTypes.Skip(1))
            {
                var additionalReport = await _localStandupService.GenerateGroupedStandupAsync(
                    tempGroup,
                    async r => await _groupService.GetDecryptedPatForRepositoryAsync(r),
                    PeriodStart,
                    PeriodEnd,
                    summaryType);

                var mergedSections = singleRepoReport.Sections.Select(s =>
                {
                    var newSection = additionalReport.Sections.FirstOrDefault(n => n.ClientCode == s.ClientCode);
                    return newSection == null ? s : ReportMergeService.MergeSummaries(s, newSection);
                }).ToList();

                singleRepoReport = singleRepoReport with { Sections = mergedSections };
            }

            GroupedReport = ReportMergeService.MergeReports(GroupedReport!, singleRepoReport);
            UpdateReportDisplay(GroupedReport);
            OnPropertyChanged(nameof(AllCommits));
            OnPropertyChanged(nameof(AllPullRequests));
            OnPropertyChanged(nameof(AllWorkItems));
            StatusMessage = $"Added {repo.ClientCode}/{repo.Repository} to report";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void SwitchView(int viewMode) => CurrentViewMode = viewMode;

    [RelayCommand]
    private void BackToConfig() => CurrentViewMode = 0;

    [RelayCommand]
    private async Task GenerateStandupAsync()
    {
        IsGenerating = true;
        StatusMessage = "Generating standup report...";

        try
        {
            if (UseGroupMode && SelectedGroup != null)
            {
                await GenerateGroupedStandupAsync();
            }
            else if (CurrentProject != null)
            {
                if (CurrentProject.UseLocalGeneration)
                {
                    await GenerateLocalStandupAsync();
                }
                else
                {
                    await GenerateApiStandupAsync();
                }
            }
            else
            {
                StatusMessage = "Please select a group or project.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private async Task GenerateGroupedStandupAsync()
    {
        if (SelectedGroup == null || SelectedGroup.Repositories.Count == 0)
        {
            StatusMessage = "Please add repositories to the group first.";
            return;
        }

        // Get the list of summary types to generate based on toggles
        var typesToGenerate = GetSelectedSummaryTypes().ToList();
        if (typesToGenerate.Count == 0)
        {
            StatusMessage = "Please select at least one summary type.";
            return;
        }

        // Reset progress
        GenerationProgress = 0;

        // Calculate progress weight per summary type
        var typeCount = typesToGenerate.Count;
        var progressPerType = 1.0 / typeCount;
        var currentTypeIndex = 0;

        // Create progress handler that updates both progress bar and status message
        var progress = new Progress<(double Progress, string Message)>(update =>
        {
            // Scale progress based on which summary type we're generating
            GenerationProgress = (currentTypeIndex * progressPerType) + (update.Progress * progressPerType);
            StatusMessage = update.Message;
        });

        // Generate first type to get the raw data
        var firstType = typesToGenerate[0];
        StatusMessage = $"Starting {firstType} summary generation...";

        GroupedReport = await _localStandupService.GenerateGroupedStandupAsync(
            SelectedGroup,
            async repo => await _groupService.GetDecryptedPatForRepositoryAsync(repo),
            PeriodStart,
            PeriodEnd,
            firstType,
            progress);

        // Generate additional types if selected
        foreach (var summaryType in typesToGenerate.Skip(1))
        {
            currentTypeIndex++;
            StatusMessage = $"Starting {summaryType} summary generation...";

            var additionalReport = await _localStandupService.GenerateGroupedStandupAsync(
                SelectedGroup,
                async repo => await _groupService.GetDecryptedPatForRepositoryAsync(repo),
                PeriodStart,
                PeriodEnd,
                summaryType,
                progress);

            // Merge the new summaries into existing sections
            var mergedSections = GroupedReport.Sections.Select(s =>
            {
                var newSection = additionalReport.Sections.FirstOrDefault(n => n.ClientCode == s.ClientCode);
                return newSection == null ? s : ReportMergeService.MergeSummaries(s, newSection);
            }).ToList();

            GroupedReport = GroupedReport with { Sections = mergedSections };
        }

        // Clear legacy report
        LatestReport = null;

        // Mark progress as complete
        GenerationProgress = 1.0;

        // Update the display content (both markdown and HTML)
        UpdateReportDisplay(GroupedReport);

        // Check for AI failures and show appropriate status
        var typeNames = string.Join(", ", typesToGenerate);
        var aiFailureCount = GroupedReport.Sections
            .Where(s => s.AllSummaries != null)
            .SelectMany(s => s.AllSummaries!.Values)
            .Count(v => v.Contains("⚠️ AI Summary Unavailable"));

        if (aiFailureCount > 0)
        {
            StatusMessage = $"⚠️ Generated with {aiFailureCount} AI failures - {GroupedReport.Sections.Count} clients, {GroupedReport.TotalCommits} commits ({typeNames})";
        }
        else
        {
            StatusMessage = $"Generated at {GroupedReport.GeneratedAt:HH:mm} - {GroupedReport.Sections.Count} clients, {GroupedReport.TotalCommits} commits ({typeNames})";
        }

        // Switch to report view and notify property changes
        CurrentViewMode = 1;
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(AllCommits));
        OnPropertyChanged(nameof(AllPullRequests));
        OnPropertyChanged(nameof(AllWorkItems));
    }

    /// <summary>
    /// Gets the summary types selected via toggles.
    /// </summary>
    private IEnumerable<SummaryType> GetSelectedSummaryTypes()
    {
        if (GenerateExecutive)
        {
            yield return SummaryType.Executive;
        }

        if (GenerateTechnical)
        {
            yield return SummaryType.Technical;
        }

        if (GenerateCodeReview)
        {
            yield return SummaryType.CodeReview;
        }
    }

    private async Task GenerateLocalStandupAsync()
    {
        if (string.IsNullOrEmpty(CurrentProject!.SourceOrganization) ||
            string.IsNullOrEmpty(CurrentProject.SourceProject) ||
            string.IsNullOrEmpty(CurrentProject.SourceRepository) ||
            string.IsNullOrEmpty(CurrentProject.SourcePat))
        {
            StatusMessage = "Please configure your source repository in settings.";
            return;
        }

        LatestReport = await _localStandupService.GenerateStandupAsync(
            CurrentProject.SourceType,
            CurrentProject.SourceOrganization,
            CurrentProject.SourceProject,
            CurrentProject.SourceRepository,
            CurrentProject.SourcePat,
            CurrentProject.AuthorIdentifier);

        // Clear grouped report
        GroupedReport = null;

        StatusMessage = $"Generated at {LatestReport.GeneratedAt:HH:mm}";
    }

    private async Task GenerateApiStandupAsync()
    {
        if (string.IsNullOrEmpty(CurrentProject!.UserId) || string.IsNullOrEmpty(CurrentProject.TenantId))
        {
            StatusMessage = "Please configure your user credentials in settings.";
            return;
        }

        LatestReport = await _apiClient.GenerateStandupAsync(
            CurrentProject.UserId,
            CurrentProject.TenantId);

        // Clear grouped report
        GroupedReport = null;

        StatusMessage = $"Generated at {LatestReport.GeneratedAt:HH:mm}";
    }

    /// <summary>
    /// Switches the display to show a different summary type (if available).
    /// </summary>
    [RelayCommand]
    private void SwitchSummaryType(SummaryType summaryType)
    {
        if (GroupedReport == null)
        {
            return;
        }

        var hasType = GroupedReport.Sections.Any(s => s.AllSummaries?.ContainsKey(summaryType) == true);
        if (!hasType)
        {
            StatusMessage = $"{summaryType} summary not generated. Enable it and regenerate.";
            return;
        }

        // Update sections to show the selected summary type
        var updatedSections = GroupedReport.Sections.Select(section =>
        {
            var summary = section.GetSummary(summaryType);
            return section with { Summary = summary };
        }).ToList();

        GroupedReport = GroupedReport with
        {
            Sections = updatedSections,
            CurrentSummaryType = summaryType
        };

        UpdateReportDisplay(GroupedReport);
        StatusMessage = $"Switched to {summaryType} summary.";
    }

    [RelayCommand]
    private async Task CopyAsMarkdownAsync()
    {
        var content = GroupedReport != null
            ? StandupReportFormatter.BuildGroupedReportMarkdown(GroupedReport)
            : LatestReport?.Summary;

        if (!string.IsNullOrEmpty(content))
        {
            await _clipboardService.SetTextAsync(content);
            StatusMessage = "Copied as Markdown!";
        }
    }

    [RelayCommand]
    private async Task CopyAsHtmlAsync()
    {
        var content = GroupedReport != null
            ? StandupReportFormatter.BuildGroupedReportHtml(GroupedReport)
            : LatestReport != null ? StandupReportFormatter.ConvertMarkdownToHtml(LatestReport.Summary) : null;

        if (!string.IsNullOrEmpty(content))
        {
            await _clipboardService.SetTextAsync(content);
            StatusMessage = "Copied as HTML!";
        }
    }

    /// <summary>
    /// Copy report to clipboard based on current display mode (HTML or Markdown).
    /// </summary>
    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        if (ShowAsHtml)
        {
            await CopyAsHtmlAsync();
        }
        else
        {
            await CopyAsMarkdownAsync();
        }
    }

    [RelayCommand]
    private async Task SaveReportAsync()
    {
        if (GroupedReport == null)
        {
            StatusMessage = "No report to save.";
            return;
        }

        try
        {
            var saved = await _reportHistoryService.SaveReportAsync(GroupedReport, ReportContent);
            ReportHistory.Insert(0, saved);
            StatusMessage = $"Report saved ({saved.DisplaySummary})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving report: {ex.Message}";
        }
    }

    [RelayCommand]
    private void LoadHistoryItem(ReportHistory? item)
    {
        if (item == null)
        {
            return;
        }

        SelectedHistoryItem = item;
        UpdateReportDisplayFromMarkdown(item.ReportContent);
        StatusMessage = $"Loaded report: {item.DisplaySummary}";
    }

    [RelayCommand]
    private async Task DeleteHistoryItemAsync(ReportHistory? item)
    {
        if (item == null)
        {
            return;
        }

        try
        {
            await _reportHistoryService.DeleteAsync(item.Id);
            ReportHistory.Remove(item);

            if (SelectedHistoryItem?.Id == item.Id)
            {
                SelectedHistoryItem = null;
                ReportContent = string.Empty;
            }

            StatusMessage = "Report deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting report: {ex.Message}";
        }
    }

    /// <summary>
    /// Clears all report history.
    /// </summary>
    [RelayCommand]
    private async Task ClearAllHistoryAsync()
    {
        try
        {
            await _reportHistoryService.ClearAllAsync();
            ReportHistory.Clear();
            SelectedHistoryItem = null;
            ReportContent = string.Empty;
            HtmlReportContent = string.Empty;
            StatusMessage = "All reports cleared.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error clearing reports: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates both markdown and HTML report content from a grouped report.
    /// </summary>
    /// <param name="report">The grouped report to display.</param>
    private void UpdateReportDisplay(GroupedStandupReportDto report)
    {
        ReportContent = StandupReportFormatter.BuildGroupedReportMarkdown(report);
        HtmlReportContent = StandupReportFormatter.BuildGroupedReportHtml(report);
    }

    /// <summary>
    /// Updates both markdown and HTML report content from raw markdown.
    /// </summary>
    /// <param name="markdown">The markdown content to display.</param>
    private void UpdateReportDisplayFromMarkdown(string markdown)
    {
        ReportContent = markdown;
        HtmlReportContent = StandupReportFormatter.ConvertMarkdownToHtml(markdown);
    }
}
