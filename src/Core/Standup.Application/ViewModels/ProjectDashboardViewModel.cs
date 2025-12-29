// <copyright file="ProjectDashboardViewModel.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Mappers;
using Standup.Application.Models;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Standup.Domain.ValueObjects;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for the Project Dashboard page showing detailed project metrics and status.
/// </summary>
public partial class ProjectDashboardViewModel : ObservableObject
{
    private readonly ISourceProviderFactory _sourceProviderFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly ILocalStandupService _localStandupService;
    private readonly ICrmProjectService _crmProjectService;

    [ObservableProperty]
    private ProjectInstance? project;

    [ObservableProperty]
    private string projectName = string.Empty;

    [ObservableProperty]
    private string organization = string.Empty;

    [ObservableProperty]
    private DateTimeOffset startDate = DateTimeOffset.Now.AddMonths(-3);

    [ObservableProperty]
    private DateTimeOffset targetDate = DateTimeOffset.Now.AddMonths(3);

    [ObservableProperty]
    private string sourceType = "Azure DevOps";

    [ObservableProperty]
    private bool isOnTrack = true;

    [ObservableProperty]
    private string healthStatus = "On Track";

    [ObservableProperty]
    private string lastSyncText = "Just now";

    [ObservableProperty]
    private bool isLoading;

    // Metrics
    [ObservableProperty]
    private int overallProgressPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MilestonesDisplay))]
    private int milestonesCompleted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MilestonesDisplay))]
    private int totalMilestones;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SprintDisplay))]
    private int currentSprintNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SprintDisplay))]
    private int totalSprints;

    [ObservableProperty]
    private int sprintDaysRemaining;

    [ObservableProperty]
    private int tasksCompleted;

    [ObservableProperty]
    private int totalTasks;

    [ObservableProperty]
    private int taskCompletionPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BudgetUsedDisplay))]
    [NotifyPropertyChangedFor(nameof(BudgetTotalDisplay))]
    private decimal budgetUsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BudgetUsedDisplay))]
    [NotifyPropertyChangedFor(nameof(BudgetTotalDisplay))]
    private decimal budgetTotal;

    [ObservableProperty]
    private int budgetPercent;

    [ObservableProperty]
    private int daysRemaining;

    // Sprint Progress
    [ObservableProperty]
    private string sprintDates = string.Empty;

    [ObservableProperty]
    private int sprintDoneCount;

    [ObservableProperty]
    private int sprintReviewCount;

    [ObservableProperty]
    private int sprintProgressCount;

    [ObservableProperty]
    private int sprintTodoCount;

    [ObservableProperty]
    private double sprintDonePercent;

    [ObservableProperty]
    private double sprintReviewPercent;

    [ObservableProperty]
    private double sprintProgressPercent;

    /// <summary>
    /// Gets or sets a value indicating whether CRM data is available (for budget, progress, milestones).
    /// </summary>
    [ObservableProperty]
    private bool hasCrmData;

    /// <summary>
    /// Gets or sets a value indicating whether sprint/iteration data is available.
    /// </summary>
    [ObservableProperty]
    private bool hasSprintData;

    /// <summary>
    /// Gets the budget display text.
    /// </summary>
    public string BudgetUsedDisplay => BudgetTotal > 0 ? $"${BudgetUsed:N1}K" : "—";

    /// <summary>
    /// Gets the budget total display text.
    /// </summary>
    public string BudgetTotalDisplay => BudgetTotal > 0 ? $"of ${BudgetTotal:N0}K allocated" : "Not configured";

    /// <summary>
    /// Gets the milestones display text.
    /// </summary>
    public string MilestonesDisplay => TotalMilestones > 0 ? $"{MilestonesCompleted} of {TotalMilestones} milestones" : "No milestones";

    /// <summary>
    /// Gets the sprint display text.
    /// </summary>
    public string SprintDisplay => TotalSprints > 0 ? $"{CurrentSprintNumber}/{TotalSprints}" : "—";

    /// <summary>
    /// Gets or sets a value indicating whether Azure DevOps connection failed.
    /// </summary>
    [ObservableProperty]
    private bool azureDevOpsConnectionFailed;

    /// <summary>
    /// Gets or sets a value indicating whether CRM connection failed.
    /// </summary>
    [ObservableProperty]
    private bool crmConnectionFailed;

    /// <summary>
    /// Gets the project phases/timeline.
    /// </summary>
    public ObservableCollection<ProjectPhase> Phases { get; } = new();

    /// <summary>
    /// Gets the team members.
    /// </summary>
    public ObservableCollection<TeamMember> TeamMembers { get; } = new();

    /// <summary>
    /// Gets the in-progress tasks.
    /// </summary>
    public ObservableCollection<ProjectTask> InProgressTasks { get; } = new();

    /// <summary>
    /// Gets the in-review tasks.
    /// </summary>
    public ObservableCollection<ProjectTask> InReviewTasks { get; } = new();

    /// <summary>
    /// Gets the recent activity items.
    /// </summary>
    public ObservableCollection<ProjectActivity> RecentActivity { get; } = new();

    /// <summary>
    /// Gets the connected services.
    /// </summary>
    public ObservableCollection<ConnectedService> ConnectedServices { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDashboardViewModel"/> class.
    /// </summary>
    /// <param name="sourceProviderFactory">The source provider factory for remote API access.</param>
    /// <param name="encryptionService">The encryption service for PAT handling.</param>
    /// <param name="localStandupService">The local standup service for local git access.</param>
    /// <param name="crmProjectService">The CRM project service for Dynamics 365 data.</param>
    public ProjectDashboardViewModel(
        ISourceProviderFactory sourceProviderFactory,
        IEncryptionService encryptionService,
        ILocalStandupService localStandupService,
        ICrmProjectService crmProjectService)
    {
        _sourceProviderFactory = sourceProviderFactory;
        _encryptionService = encryptionService;
        _localStandupService = localStandupService;
        _crmProjectService = crmProjectService;
    }

    /// <summary>
    /// Loads the project data synchronously (for backwards compatibility).
    /// </summary>
    /// <param name="projectInstance">The project instance to display.</param>
    public void LoadProject(ProjectInstance projectInstance)
    {
        Project = projectInstance;
        ProjectName = projectInstance.Name;
        Organization = projectInstance.SourceOrganization ?? "Unknown";
        SourceType = projectInstance.SourceType.ToString();

        // Fire and forget async load - UI will update via bindings
        _ = LoadProjectDataAsync(projectInstance);
    }

    /// <summary>
    /// Loads the project data asynchronously with real data from source providers.
    /// </summary>
    /// <param name="projectInstance">The project instance to display.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task LoadProjectAsync(ProjectInstance projectInstance)
    {
        Project = projectInstance;
        ProjectName = projectInstance.Name;
        Organization = projectInstance.SourceOrganization ?? "Unknown";
        SourceType = projectInstance.SourceType.ToString();

        await LoadProjectDataAsync(projectInstance);
    }

    private async Task LoadProjectDataAsync(ProjectInstance projectInstance)
    {
        // Check data sources: LocalPath for commits (no PAT), PAT for PRs/work items, CRM for project management
        bool hasLocalPath = !string.IsNullOrEmpty(projectInstance.LocalPath);
        bool hasPat = !string.IsNullOrEmpty(projectInstance.SourcePat);
        bool hasCrmProject = !string.IsNullOrEmpty(projectInstance.CrmProjectId);

        if (!hasLocalPath && !hasPat && !hasCrmProject)
        {
            Log.Warning("ProjectDashboard: No LocalPath, PAT, or CRM project configured for {ProjectName}", projectInstance.Name);
            LastSyncText = "No data sources configured";
            return;
        }

        IsLoading = true;

        try
        {
            Log.Information(
                "ProjectDashboard: Loading data for {ProjectName} (LocalPath: {HasLocal}, PAT: {HasPat}, CRM: {HasCrm})",
                projectInstance.Name,
                hasLocalPath,
                hasPat,
                hasCrmProject);

            // Fetch CRM project data for timeline, milestones, budget, progress
            await LoadCrmDataAsync(projectInstance);

            var since30d = DateTimeOffset.UtcNow.AddDays(-30);
            var until = DateTimeOffset.UtcNow;

            var commits = new List<CommitInfo>();
            var prs = new List<PullRequestInfo>();
            var inProgress = new List<WorkItemInfo>();
            var completed = new List<WorkItemInfo>();

            // Fetch commits from local git (no PAT required)
            if (hasLocalPath && _localStandupService != null)
            {
                Log.Information("ProjectDashboard: Fetching commits from local path {LocalPath}", projectInstance.LocalPath);

                var groupedRepo = new GroupedRepository
                {
                    LocalPath = projectInstance.LocalPath,
                    SourceType = projectInstance.SourceType,
                    Organization = projectInstance.SourceOrganization ?? string.Empty,
                    Project = projectInstance.SourceProject ?? string.Empty,
                    Repository = projectInstance.SourceRepository ?? string.Empty,
                };

                var localCommits = await _localStandupService.GetLocalCommitsAsync(
                    new[] { groupedRepo },
                    since30d,
                    until);

                commits = localCommits.ToList();
                Log.Information("ProjectDashboard: Fetched {Count} commits from local git", commits.Count);
            }

            // Fetch PRs and work items from remote API (PAT required)
            // Each fetch is wrapped in try-catch so failures don't affect other data
            if (hasPat && _sourceProviderFactory != null && _encryptionService != null)
            {
                Log.Information("ProjectDashboard: Fetching PRs and work items from remote API");
                bool apiCallFailed = false;

                var sourceRepo = new SourceRepository
                {
                    Id = projectInstance.Id,
                    SourceType = projectInstance.SourceType,
                    Organization = projectInstance.SourceOrganization ?? string.Empty,
                    Project = projectInstance.SourceProject,
                    Repository = projectInstance.SourceRepository ?? string.Empty,
                    AuthorIdentifier = projectInstance.AuthorIdentifier ?? string.Empty,
                    EncryptedPat = await _encryptionService.EncryptAsync(projectInstance.SourcePat!),
                };

                var provider = _sourceProviderFactory.GetProvider(projectInstance.SourceType);

                // If we don't have local commits, fetch from API
                if (commits.Count == 0)
                {
                    try
                    {
                        var apiCommits = await provider.GetCommitsAsync(sourceRepo, since30d, until);
                        commits = apiCommits.ToList();
                        Log.Information("ProjectDashboard: Fetched {Count} commits from API", commits.Count);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "ProjectDashboard: Failed to fetch commits from API");
                        apiCallFailed = true;
                    }
                }

                // Fetch PRs (failure doesn't affect other data)
                try
                {
                    var apiPrs = await provider.GetOpenPullRequestsAsync(sourceRepo);
                    prs = apiPrs.ToList();
                    Log.Information("ProjectDashboard: Fetched {Count} PRs from API", prs.Count);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "ProjectDashboard: Failed to fetch PRs from API");
                    apiCallFailed = true;
                }

                // Fetch in-progress work items (failure doesn't affect other data)
                try
                {
                    var apiInProgress = await provider.GetInProgressWorkItemsAsync(sourceRepo);
                    inProgress = apiInProgress.ToList();
                    Log.Information("ProjectDashboard: Fetched {Count} in-progress work items from API", inProgress.Count);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "ProjectDashboard: Failed to fetch in-progress work items from API");
                    apiCallFailed = true;
                }

                // Fetch completed work items (failure doesn't affect other data)
                try
                {
                    var apiCompleted = await provider.GetCompletedWorkItemsAsync(sourceRepo, since30d, until);
                    completed = apiCompleted.ToList();
                    Log.Information("ProjectDashboard: Fetched {Count} completed work items from API", completed.Count);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "ProjectDashboard: Failed to fetch completed work items from API");
                    apiCallFailed = true;
                }

                AzureDevOpsConnectionFailed = apiCallFailed;
            }

            Log.Information(
                "ProjectDashboard: Total data - {Commits} commits, {PRs} PRs, {InProgress} in-progress, {Completed} completed",
                commits.Count,
                prs.Count,
                inProgress.Count,
                completed.Count);

            // Update metrics
            TasksCompleted = completed.Count;
            TotalTasks = completed.Count + inProgress.Count;
            TaskCompletionPercent = TotalTasks > 0 ? (TasksCompleted * 100) / TotalTasks : 0;
            OverallProgressPercent = commits.Count > 0 ? Math.Min(100, commits.Count * 2) : TaskCompletionPercent;

            // Sprint/work item counts
            SprintDoneCount = completed.Count;
            SprintProgressCount = inProgress.Count;
            SprintReviewCount = prs.Count;
            SprintTodoCount = 0;

            // Calculate percentages
            var totalSprintItems = SprintDoneCount + SprintProgressCount + SprintReviewCount + SprintTodoCount;
            if (totalSprintItems > 0)
            {
                SprintDonePercent = (SprintDoneCount * 100.0) / totalSprintItems;
                SprintProgressPercent = (SprintProgressCount * 100.0) / totalSprintItems;
                SprintReviewPercent = (SprintReviewCount * 100.0) / totalSprintItems;
            }

            // Map data to UI collections using mapper
            RecentActivity.Clear();
            foreach (var activity in ProjectDashboardMapper.MapRecentActivity(commits, prs, completed))
            {
                RecentActivity.Add(activity);
            }

            InProgressTasks.Clear();
            foreach (var task in ProjectDashboardMapper.MapInProgressTasks(inProgress))
            {
                InProgressTasks.Add(task);
            }

            InReviewTasks.Clear();
            foreach (var task in ProjectDashboardMapper.MapInReviewTasks(prs))
            {
                InReviewTasks.Add(task);
            }

            TeamMembers.Clear();
            foreach (var member in ProjectDashboardMapper.MapTeamMembers(commits))
            {
                TeamMembers.Add(member);
            }

            UpdateConnectedServices(projectInstance.SourceType, hasLocalPath, hasPat, hasCrmProject);

            // Phases only come from CRM - never fabricate them.
            // If CRM didn't provide any, leave Phases empty.
            LastSyncText = "Just now";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProjectDashboard: Failed to load data for project {ProjectName}", projectInstance.Name);
            LastSyncText = "Error loading data";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateConnectedServices(
        Domain.Enums.SourceType projectSourceType,
        bool hasLocalPath,
        bool hasPat,
        bool hasCrmProject)
    {
        ConnectedServices.Clear();

        // Local Git connection
        if (hasLocalPath)
        {
            ConnectedServices.Add(new ConnectedService("Local Git", "📁", ServiceStatus.Connected));
        }

        // Remote source connection - show actual status
        if (hasPat)
        {
            var adoStatus = AzureDevOpsConnectionFailed ? ServiceStatus.Error : ServiceStatus.Connected;
            if (projectSourceType == Domain.Enums.SourceType.AzureDevOps)
            {
                ConnectedServices.Add(new ConnectedService("Azure DevOps", "🔷", adoStatus));
            }
            else
            {
                ConnectedServices.Add(new ConnectedService("GitHub", "🐙", adoStatus));
            }
        }

        // CRM/Project connection - show actual status
        if (hasCrmProject)
        {
            var crmStatus = CrmConnectionFailed ? ServiceStatus.Error : ServiceStatus.Connected;
            ConnectedServices.Add(new ConnectedService("Dynamics 365 CRM", "📊", crmStatus));
        }
    }

    private async Task LoadCrmDataAsync(ProjectInstance projectInstance)
    {
        if (_crmProjectService == null)
        {
            Log.Debug("ProjectDashboard: CRM service not available, skipping CRM data fetch");
            CrmConnectionFailed = !string.IsNullOrEmpty(projectInstance.CrmProjectId);
            return;
        }

        if (string.IsNullOrEmpty(projectInstance.CrmProjectId))
        {
            Log.Debug("ProjectDashboard: No CRM project ID configured for {ProjectName}", projectInstance.Name);
            return;
        }

        try
        {
            Log.Information("ProjectDashboard: Fetching CRM data for project ID {CrmProjectId}", projectInstance.CrmProjectId);

            // Fetch project details from CRM
            var crmProject = await _crmProjectService.GetProjectByIdAsync(projectInstance.CrmProjectId);

            if (crmProject != null)
            {
                HasCrmData = true;
                Log.Information(
                    "ProjectDashboard: Retrieved CRM project {ProjectName}, Progress: {Progress}%",
                    crmProject.ProjectName,
                    crmProject.PercentComplete);

                // Map CRM project data to dashboard properties
                if (crmProject.StartDate.HasValue)
                {
                    StartDate = new DateTimeOffset(crmProject.StartDate.Value, TimeSpan.Zero);
                }

                if (crmProject.EndDate.HasValue)
                {
                    TargetDate = new DateTimeOffset(crmProject.EndDate.Value, TimeSpan.Zero);
                }

                if (crmProject.PercentComplete.HasValue)
                {
                    OverallProgressPercent = (int)Math.Round(crmProject.PercentComplete.Value);
                }

                // Map budget data if available
                if (crmProject.BudgetHours.HasValue)
                {
                    BudgetTotal = crmProject.BudgetHours.Value;
                }

                if (crmProject.HoursUsed.HasValue)
                {
                    BudgetUsed = crmProject.HoursUsed.Value;
                }

                if (BudgetTotal > 0)
                {
                    BudgetPercent = (int)Math.Round((BudgetUsed / BudgetTotal) * 100);
                }

                // Map status to health
                HealthStatus = crmProject.Status == "Active" ? "On Track" : crmProject.Status;
                IsOnTrack = crmProject.Status == "Active";
            }
            else
            {
                CrmConnectionFailed = true;
            }

            // Fetch milestones from CRM for project timeline/phases
            var milestones = await _crmProjectService.GetUpcomingMilestonesAsync(projectInstance.CrmProjectId);

            if (milestones.Count > 0)
            {
                Log.Information("ProjectDashboard: Retrieved {Count} milestones from CRM", milestones.Count);

                Phases.Clear();
                TotalMilestones = milestones.Count;
                MilestonesCompleted = milestones.Count(m => m.PercentComplete >= 100);

                foreach (var milestone in milestones)
                {
                    var status = milestone.PercentComplete >= 100 ? PhaseStatus.Complete :
                                 milestone.PercentComplete > 0 ? PhaseStatus.InProgress :
                                 PhaseStatus.Pending;

                    Phases.Add(new ProjectPhase(
                        milestone.Name,
                        $"Due: {milestone.DueDate:MMM dd, yyyy}",
                        status)
                    {
                        EffortEstimated = milestone.EffortEstimated,
                        EffortCompleted = milestone.EffortCompleted,
                        EffortRemaining = milestone.EffortRemaining,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ProjectDashboard: Failed to fetch CRM data for project {ProjectId}", projectInstance.CrmProjectId);
            CrmConnectionFailed = true;
        }
    }

    /// <summary>
    /// Gets the command to go back to the projects list.
    /// </summary>
    [RelayCommand]
    private void GoBack() => Log.Debug("GoBack requested for project {ProjectName}", ProjectName);

    /// <summary>
    /// Gets the command to view the Gantt chart.
    /// </summary>
    [RelayCommand]
    private void ViewGantt() => Log.Debug("ViewGantt requested for project {ProjectName}", ProjectName);

    /// <summary>
    /// Gets the command to view the task board.
    /// </summary>
    [RelayCommand]
    private void ViewBoard() => Log.Debug("ViewBoard requested for project {ProjectName}", ProjectName);
}
