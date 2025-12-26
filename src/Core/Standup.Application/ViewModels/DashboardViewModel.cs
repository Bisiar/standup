using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Domain.Entities;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for the Dashboard tab that displays code metrics charts.
/// Shows bar chart for single day views and line chart for week view.
/// Auto-fetches commits from local repositories when no report is loaded.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IGroupRepository _groupRepository;
    private readonly ILocalStandupService _standupService;
    private readonly IProjectService _projectService;

    /// <summary>
    /// Mapping from SourceRepository name to Project display name.
    /// </summary>
    private Dictionary<string, string> _repoToProjectName = new();

    /// <summary>
    /// Commits loaded directly from local repos (independent of Report).
    /// </summary>
    private List<CommitInfo> _localCommits = new();

    /// <summary>
    /// Gets or sets the available groups.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RepositoryGroup> _groups = new();

    /// <summary>
    /// Gets or sets the selected group for filtering.
    /// </summary>
    [ObservableProperty]
    private RepositoryGroup? _selectedGroup;

    /// <summary>
    /// Gets or sets the current report data.
    /// </summary>
    [ObservableProperty]
    private GroupedStandupReportDto? _report;

    /// <summary>
    /// Gets or sets the daily code metrics for line charts (week view).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DailyCodeMetrics> _dailyMetrics = new();

    /// <summary>
    /// Gets or sets the per-project daily activity data for multi-line trend chart.
    /// Key is project name, value is the daily activity data for that project.
    /// </summary>
    [ObservableProperty]
    private Dictionary<string, ObservableCollection<ProjectDailyActivity>> _projectDailyActivities = new();

    /// <summary>
    /// Gets or sets the repository metrics for bar charts (day view).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<RepositoryMetrics> _repositoryMetrics = new();

    /// <summary>
    /// Gets or sets the client metrics for additional context.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ClientMetrics> _clientMetrics = new();

    /// <summary>
    /// Gets or sets the selected time period index (0=Yesterday, 1=Today, 2=Week).
    /// </summary>
    [ObservableProperty]
    private int _selectedPeriodIndex = 2;

    /// <summary>
    /// Gets or sets the period start date.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset _periodStart;

    /// <summary>
    /// Gets or sets the period end date.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset _periodEnd;

    /// <summary>
    /// Gets or sets the period display string.
    /// </summary>
    [ObservableProperty]
    private string _periodDisplay = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is a week view (line chart).
    /// </summary>
    [ObservableProperty]
    private bool _isWeekView;

    /// <summary>
    /// Gets or sets a value indicating whether data is available.
    /// </summary>
    [ObservableProperty]
    private bool _hasData;

    /// <summary>
    /// Gets or sets the total additions across all data.
    /// </summary>
    [ObservableProperty]
    private int _totalAdditions;

    /// <summary>
    /// Gets or sets the total deletions across all data.
    /// </summary>
    [ObservableProperty]
    private int _totalDeletions;

    /// <summary>
    /// Gets or sets the total commits across all data.
    /// </summary>
    [ObservableProperty]
    private int _totalCommits;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this view model is loading.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the client code metrics for horizontal bar chart and donut chart.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ClientCodeMetrics> _clientCodeMetrics = new();

    /// <summary>
    /// Gets or sets the critical warnings (fetch errors).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GroupWarning> _criticalWarnings = new();

    /// <summary>
    /// Gets or sets the configuration warnings (missing PAT, etc.).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GroupWarning> _warnings = new();

    /// <summary>
    /// Gets or sets the informational warnings (no activity, etc.).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<GroupWarning> _infoWarnings = new();

    /// <summary>
    /// Gets or sets the recent commits for the activity timeline.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CommitInfo> _recentCommits = new();

    /// <summary>
    /// Gets the net change (additions - deletions).
    /// </summary>
    public int NetChange => TotalAdditions - TotalDeletions;

    /// <summary>
    /// Gets the number of active projects with commits in the period.
    /// </summary>
    public int ActiveProjectCount => ClientCodeMetrics.Count;

    /// <summary>
    /// Gets a value indicating whether "All" is selected.
    /// </summary>
    public bool IsAllSelected => SelectedGroup?.Id == AllGroupId;

    /// <summary>
    /// The special ID for the "All" group.
    /// </summary>
    public const string AllGroupId = "__all__";

    /// <summary>
    /// Gets the groups with "All" option prepended.
    /// </summary>
    public ObservableCollection<RepositoryGroup> GroupsWithAll
    {
        get
        {
            var result = new ObservableCollection<RepositoryGroup>();

            // Add "All" option first
            var allGroup = new RepositoryGroup { Id = AllGroupId, Name = "All" };

            // Populate "All" with repos from all groups
            foreach (var group in Groups)
            {
                foreach (var repo in group.Repositories)
                {
                    if (!allGroup.Repositories.Any(r => r.Id == repo.Id))
                    {
                        allGroup.Repositories.Add(repo);
                    }
                }
            }

            result.Add(allGroup);

            // Add all regular groups
            foreach (var group in Groups)
            {
                result.Add(group);
            }

            return result;
        }
    }

    /// <summary>
    /// Gets the total warning count (critical + warnings, excluding info).
    /// </summary>
    public int TotalWarningCount => CriticalWarnings.Count + Warnings.Count;

    /// <summary>
    /// Gets a value indicating whether there are any critical warnings.
    /// </summary>
    public bool HasCriticalWarnings => CriticalWarnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether there are any warnings to display.
    /// </summary>
    public bool HasAnyWarnings => CriticalWarnings.Count > 0 || Warnings.Count > 0 || InfoWarnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the group is healthy (no critical or warning issues).
    /// </summary>
    public bool IsGroupHealthy => CriticalWarnings.Count == 0 && Warnings.Count == 0;

    /// <summary>
    /// Gets all commits - from Report if available, otherwise from local repos.
    /// </summary>
    public IReadOnlyList<CommitInfo> AllCommits
    {
        get
        {
            // Prefer report data if available
            if (Report?.Sections != null && Report.Sections.Any(s => s.Commits.Count > 0))
            {
                return Report.Sections
                    .SelectMany(s => s.Commits)
                    .OrderByDescending(c => c.CommittedAt)
                    .ToList();
            }

            // Fall back to locally fetched commits
            return _localCommits;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
    /// </summary>
    /// <param name="groupRepository">Repository for accessing groups.</param>
    /// <param name="standupService">Service for fetching local commits.</param>
    /// <param name="projectService">Service for accessing projects.</param>
    public DashboardViewModel(IGroupRepository groupRepository, ILocalStandupService standupService, IProjectService projectService)
    {
        _groupRepository = groupRepository;
        _standupService = standupService;
        _projectService = projectService;

        // Default to week view (most likely to have data)
        SetPeriodWeek();
    }

    /// <summary>
    /// Sets the report data and refreshes charts.
    /// </summary>
    /// <param name="report">The grouped standup report.</param>
    public void SetReport(GroupedStandupReportDto? report)
    {
        Report = report;
        RefreshChartData();
        OnPropertyChanged(nameof(AllCommits));
    }

    /// <summary>
    /// Called when Groups collection changes - notify GroupsWithAll.
    /// </summary>
    /// <param name="value">The new groups collection.</param>
    partial void OnGroupsChanged(ObservableCollection<RepositoryGroup> value)
    {
        OnPropertyChanged(nameof(GroupsWithAll));
    }

    /// <summary>
    /// Called when SelectedGroup changes - triggers a reload.
    /// </summary>
    /// <param name="value">The new selected group.</param>
    partial void OnSelectedGroupChanged(RepositoryGroup? value)
    {
        if (value != null)
        {
            // Clear report data so we use local fetch
            Report = null;
            OnPropertyChanged(nameof(IsAllSelected));
            _localCommits.Clear();

            // Trigger reload with new group
            _ = LoadAsync();
        }
    }

    /// <summary>
    /// Loads commits from the selected group's local repositories.
    /// Auto-fetches data for dashboard display without requiring a full standup generation.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task LoadAsync()
    {
        // Skip if we already have report data
        if (Report?.Sections != null && Report.Sections.Any(s => s.Commits.Count > 0))
        {
            Log.Debug("Dashboard: Skipping auto-fetch - report data already loaded");
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading...";

        try
        {
            // Load groups if not already loaded
            if (Groups.Count == 0)
            {
                var groups = (await _groupRepository.GetAllAsync()).ToList();
                Groups = new ObservableCollection<RepositoryGroup>(groups);

                // Load project name mapping (repo name -> project display name)
                var projects = await _projectService.GetProjectsAsync();
                _repoToProjectName = projects
                    .Where(p => !string.IsNullOrEmpty(p.SourceRepository))
                    .ToDictionary(
                        p => p.SourceRepository!,
                        p => p.Name,
                        StringComparer.OrdinalIgnoreCase);

                // Set default group if none selected
                if (SelectedGroup == null && groups.Count > 0)
                {
                    var defaultGroup = await _groupRepository.GetDefaultAsync();
                    SelectedGroup = defaultGroup ?? groups.First();
                    return; // OnSelectedGroupChanged will trigger another load
                }
            }

            if (SelectedGroup == null)
            {
                StatusMessage = "No group selected. Create a group in the Groups tab.";
                HasData = false;
                return;
            }

            var repos = SelectedGroup.Repositories.ToList();
            if (repos.Count == 0)
            {
                StatusMessage = "No repositories in selected group.";
                HasData = false;
                return;
            }

            // Fetch for a wider date range (last 14 days) to ensure we have data
            var since = DateTimeOffset.Now.Date.AddDays(-14);
            var until = DateTimeOffset.Now.Date.AddDays(1);

            Log.Information(
                "Dashboard: Fetching commits from group '{GroupName}' ({RepoCount} repos)",
                SelectedGroup.Name,
                repos.Count);

            _localCommits = (await _standupService.GetLocalCommitsAsync(repos, since, until)).ToList();

            Log.Information(
                "Dashboard: Loaded {Count} commits from group '{GroupName}'",
                _localCommits.Count,
                SelectedGroup.Name);

            OnPropertyChanged(nameof(AllCommits));
            RefreshChartData();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dashboard: Failed to auto-fetch commits");
            StatusMessage = $"Failed to load: {ex.Message}";
            HasData = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Selects yesterday as the time period.
    /// </summary>
    [RelayCommand]
    private void SelectYesterday()
    {
        SelectedPeriodIndex = 0;
        SetPeriodYesterday();
        RefreshChartData();
    }

    /// <summary>
    /// Selects today as the time period.
    /// </summary>
    [RelayCommand]
    private void SelectToday()
    {
        SelectedPeriodIndex = 1;
        SetPeriodToday();
        RefreshChartData();
    }

    /// <summary>
    /// Selects last week as the time period.
    /// </summary>
    [RelayCommand]
    private void SelectWeek()
    {
        SelectedPeriodIndex = 2;
        SetPeriodWeek();
        RefreshChartData();
    }

    private void SetPeriodYesterday()
    {
        var yesterday = DateTimeOffset.Now.Date.AddDays(-1);
        PeriodStart = yesterday;
        PeriodEnd = yesterday.AddDays(1).AddTicks(-1);
        PeriodDisplay = $"Yesterday ({yesterday:MMM dd})";
        IsWeekView = false;
    }

    private void SetPeriodToday()
    {
        var today = DateTimeOffset.Now.Date;
        PeriodStart = today;
        PeriodEnd = today.AddDays(1).AddTicks(-1);
        PeriodDisplay = $"Today ({today:MMM dd})";
        IsWeekView = false;
    }

    private void SetPeriodWeek()
    {
        var today = DateTimeOffset.Now.Date;
        var weekStart = today.AddDays(-6);
        PeriodStart = weekStart;
        PeriodEnd = today.AddDays(1).AddTicks(-1);
        PeriodDisplay = $"Last 7 Days ({weekStart:MMM dd} - {today:MMM dd})";
        IsWeekView = true;
    }

    private void RefreshChartData()
    {
        if (AllCommits.Count == 0)
        {
            HasData = false;
            DailyMetrics.Clear();
            RepositoryMetrics.Clear();
            ClientMetrics.Clear();
            ClientCodeMetrics.Clear();
            RecentCommits.Clear();
            TotalAdditions = 0;
            TotalDeletions = 0;
            TotalCommits = 0;
            StatusMessage = "No data available";
            RefreshWarnings();
            NotifyCalculatedPropertiesChanged();
            return;
        }

        // Filter commits to the selected period
        var filteredCommits = AllCommits
            .Where(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd)
            .ToList();

        if (filteredCommits.Count == 0)
        {
            HasData = false;
            DailyMetrics.Clear();
            RepositoryMetrics.Clear();
            ClientCodeMetrics.Clear();
            RecentCommits.Clear();
            StatusMessage = $"No commits found for {PeriodDisplay}";
            RefreshWarnings();
            NotifyCalculatedPropertiesChanged();
            return;
        }

        HasData = true;

        // Calculate totals
        TotalAdditions = filteredCommits.Sum(c => c.Additions);
        TotalDeletions = filteredCommits.Sum(c => c.Deletions);
        TotalCommits = filteredCommits.Count;

        // Generate daily metrics (for line chart in week view)
        var dailyData = filteredCommits
            .GroupBy(c => c.CommittedAt.Date)
            .Select(g => new DailyCodeMetrics
            {
                Date = g.Key,
                Additions = g.Sum(c => c.Additions),
                Deletions = g.Sum(c => c.Deletions),
                CommitCount = g.Count(),
            })
            .OrderBy(d => d.Date)
            .ToList();

        DailyMetrics = new ObservableCollection<DailyCodeMetrics>(dailyData);

        // Generate per-project daily activity (for multi-line trend chart)
        GenerateProjectDailyActivities(filteredCommits);

        // Generate repository metrics (for bar chart in day view)
        var repoData = filteredCommits
            .GroupBy(c => c.Repository)
            .Select(g => new RepositoryMetrics
            {
                Repository = g.Key,
                Additions = g.Sum(c => c.Additions),
                Deletions = g.Sum(c => c.Deletions),
                CommitCount = g.Count(),
            })
            .OrderByDescending(r => r.TotalChanges)
            .Take(10) // Top 10 repos
            .ToList();

        RepositoryMetrics = new ObservableCollection<RepositoryMetrics>(repoData);

        // Generate client code metrics (grouped by client code from repo config or report sections)
        RefreshClientCodeMetrics(filteredCommits);

        // Populate recent commits (last 5 for timeline)
        var recentCommitData = filteredCommits
            .OrderByDescending(c => c.CommittedAt)
            .Take(5)
            .ToList();

        RecentCommits = new ObservableCollection<CommitInfo>(recentCommitData);

        // Generate client metrics (only if we have report data with sections)
        if (Report?.Sections != null && Report.Sections.Count > 0)
        {
            var clientData = Report.Sections
                .Select(s => new ClientMetrics
                {
                    ClientCode = string.IsNullOrWhiteSpace(s.ClientCode) ? "General" : s.ClientCode,
                    Additions = s.Commits
                        .Where(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd)
                        .Sum(c => c.Additions),
                    Deletions = s.Commits
                        .Where(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd)
                        .Sum(c => c.Deletions),
                    CommitCount = s.Commits
                        .Count(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd),
                })
                .Where(c => c.CommitCount > 0)
                .OrderByDescending(c => c.TotalChanges)
                .ToList();

            ClientMetrics = new ObservableCollection<ClientMetrics>(clientData);
        }
        else
        {
            // Clear client metrics when using local commits (no client grouping available)
            ClientMetrics.Clear();
        }

        StatusMessage = $"{TotalCommits} commits, +{TotalAdditions}/-{TotalDeletions} lines";

        // Refresh warnings and notify UI
        RefreshWarnings();
        NotifyCalculatedPropertiesChanged();
    }
}
