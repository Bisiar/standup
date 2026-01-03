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
    private readonly ICrmTenantConfigService? _crmTenantConfigService;
    private List<CrmTask> _pendingCrmTasks = new();

    /// <summary>
    /// Event for loading CRM data from a specific tenant.
    /// The MAUI layer subscribes to create a tenant-specific CRM service.
    /// </summary>
    public event Func<CrmTenantConfig, string, Task<CrmDataResult>>? OnLoadCrmDataFromTenant;

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

    [ObservableProperty]
    private string loadingStatus = string.Empty;

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

    // Project Card - JTP Fields

    /// <summary>
    /// Gets or sets the practice area (e.g., "Dynamics 365", "Power Platform").
    /// </summary>
    [ObservableProperty]
    private string practice = string.Empty;

    /// <summary>
    /// Gets or sets the project type (e.g., "Implementation", "Support").
    /// </summary>
    [ObservableProperty]
    private string projectType = string.Empty;

    /// <summary>
    /// Gets or sets the practice director name.
    /// </summary>
    [ObservableProperty]
    private string practiceDirector = string.Empty;

    /// <summary>
    /// Gets or sets the account manager name.
    /// </summary>
    [ObservableProperty]
    private string accountManager = string.Empty;

    /// <summary>
    /// Gets or sets the scheduling engine type (e.g., "Sprints", "Fixed Schedule").
    /// </summary>
    [ObservableProperty]
    private string schedulingEngine = string.Empty;

    /// <summary>
    /// Gets or sets the sprint commitments description.
    /// </summary>
    [ObservableProperty]
    private string sprintCommitments = string.Empty;

    /// <summary>
    /// Gets or sets the next milestone name.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NextMilestoneDisplay))]
    private string nextMilestone = string.Empty;

    /// <summary>
    /// Gets or sets the next milestone due date.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NextMilestoneDisplay))]
    private DateTime? nextMilestoneDate;

    /// <summary>
    /// Gets or sets the estimated labor cost.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedLaborDisplay))]
    private decimal estimatedLabor;

    /// <summary>
    /// Gets or sets the cost percentage used.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CostPercentDisplay))]
    private decimal costPercent;

    /// <summary>
    /// Gets or sets the SOW document URL.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSowDocument))]
    [NotifyPropertyChangedFor(nameof(SowDocumentName))]
    private string sowDocumentUrl = string.Empty;

    /// <summary>
    /// Gets or sets the customer name.
    /// </summary>
    [ObservableProperty]
    private string customerName = string.Empty;

    /// <summary>
    /// Gets or sets the project status color (Green, Yellow, Red).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusBackgroundColor))]
    [NotifyPropertyChangedFor(nameof(StatusDotColor))]
    private string projectStatusColor = "Green";

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
    /// Gets the estimated labor display text.
    /// </summary>
    public string EstimatedLaborDisplay => EstimatedLabor > 0 ? $"${EstimatedLabor:N0}" : "—";

    /// <summary>
    /// Gets the cost percent display text.
    /// </summary>
    public string CostPercentDisplay => CostPercent > 0 ? $"{CostPercent:N0}%" : "—";

    /// <summary>
    /// Gets a value indicating whether a SOW document is available.
    /// </summary>
    public bool HasSowDocument => !string.IsNullOrEmpty(SowDocumentUrl);

    /// <summary>
    /// Gets the SOW document file name.
    /// </summary>
    public string SowDocumentName
    {
        get
        {
            if (string.IsNullOrEmpty(SowDocumentUrl))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFileName(new Uri(SowDocumentUrl).LocalPath);
            }
            catch
            {
                return SowDocumentUrl;
            }
        }
    }

    /// <summary>
    /// Gets the next milestone display text with date.
    /// </summary>
    public string NextMilestoneDisplay => string.IsNullOrEmpty(NextMilestone)
        ? "—"
        : NextMilestoneDate.HasValue
            ? $"{NextMilestone} - {NextMilestoneDate:MMM dd, yyyy}"
            : NextMilestone;

    /// <summary>
    /// Gets the status background color hex value based on project status.
    /// </summary>
    public string StatusBackgroundColor => ProjectStatusColor switch
    {
        "Green" => "#22C55E33",
        "Yellow" => "#EAB30833",
        "Red" => "#EF444433",
        _ => "#22C55E33",
    };

    /// <summary>
    /// Gets the status dot color hex value based on project status.
    /// </summary>
    public string StatusDotColor => ProjectStatusColor switch
    {
        "Green" => "#22C55E",
        "Yellow" => "#EAB308",
        "Red" => "#EF4444",
        _ => "#22C55E",
    };

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
    /// <param name="crmTenantConfigService">The CRM tenant config service for multi-tenant CRM access.</param>
    public ProjectDashboardViewModel(
        ISourceProviderFactory sourceProviderFactory,
        IEncryptionService encryptionService,
        ILocalStandupService localStandupService,
        ICrmTenantConfigService? crmTenantConfigService = null)
    {
        _sourceProviderFactory = sourceProviderFactory;
        _encryptionService = encryptionService;
        _localStandupService = localStandupService;
        _crmTenantConfigService = crmTenantConfigService;
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
        // Try to discover local path if not explicitly set
        var localPath = projectInstance.LocalPath;
        if (string.IsNullOrEmpty(localPath))
        {
            localPath = TryDiscoverLocalPath(projectInstance);
            if (!string.IsNullOrEmpty(localPath))
            {
                Log.Information("ProjectDashboard: Discovered local path {LocalPath} for {ProjectName}", localPath, projectInstance.Name);
                projectInstance = projectInstance with { LocalPath = localPath };
            }
        }

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
        LoadingStatus = "Loading project data...";

        try
        {
            Log.Information(
                "ProjectDashboard: Loading data for {ProjectName} (LocalPath: {HasLocal}, PAT: {HasPat}, CRM: {HasCrm})",
                projectInstance.Name,
                hasLocalPath,
                hasPat,
                hasCrmProject);

            // Fetch CRM project data for timeline, milestones, budget, progress
            if (hasCrmProject)
            {
                LoadingStatus = "Loading CRM project data...";
            }

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
                LoadingStatus = "Loading commits from local git...";
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
                LoadingStatus = "Loading Azure DevOps data...";
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

            // Only calculate progress from commits/tasks if CRM didn't provide the progress
            if (!HasCrmData)
            {
                OverallProgressPercent = commits.Count > 0 ? Math.Min(100, commits.Count * 2) : TaskCompletionPercent;
            }

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

            // Add CRM tasks (stored earlier from LoadCrmDataAsync)
            foreach (var crmTask in _pendingCrmTasks)
            {
                var dueDate = crmTask.ScheduledEnd.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(crmTask.ScheduledEnd.Value, DateTimeKind.Utc))
                    : DateTimeOffset.UtcNow.AddDays(7);

                var initials = !string.IsNullOrEmpty(crmTask.AssignedTo)
                    ? string.Concat(crmTask.AssignedTo.Split(' ').Where(s => !string.IsNullOrEmpty(s)).Select(n => n[0]))
                    : "?";

                InProgressTasks.Add(new ProjectTask(
                    crmTask.TaskId,
                    crmTask.Name,
                    initials,
                    crmTask.AssignedTo ?? "Unassigned",
                    TaskPriority.Medium,
                    dueDate));
            }

            _pendingCrmTasks.Clear();

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
            LoadingStatus = string.Empty;
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

        // Remote source connection - only show if connected successfully (don't show error)
        if (hasPat && !AzureDevOpsConnectionFailed)
        {
            if (projectSourceType == Domain.Enums.SourceType.AzureDevOps)
            {
                ConnectedServices.Add(new ConnectedService("Azure DevOps", "🔷", ServiceStatus.Connected));
            }
            else
            {
                ConnectedServices.Add(new ConnectedService("GitHub", "🐙", ServiceStatus.Connected));
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
        if (string.IsNullOrEmpty(projectInstance.CrmProjectId))
        {
            Log.Debug("ProjectDashboard: No CRM project ID configured for {ProjectName}", projectInstance.Name);
            return;
        }

        if (!projectInstance.CrmTenantConfigId.HasValue)
        {
            Log.Warning("ProjectDashboard: Project has CrmProjectId but no CrmTenantConfigId for {ProjectName}", projectInstance.Name);
            CrmConnectionFailed = true;
            return;
        }

        if (_crmTenantConfigService == null)
        {
            Log.Warning("ProjectDashboard: CRM tenant config service not available");
            CrmConnectionFailed = true;
            return;
        }

        try
        {
            // Get the tenant configuration for this project
            var tenantConfig = await _crmTenantConfigService.GetByIdAsync(projectInstance.CrmTenantConfigId.Value);
            if (tenantConfig == null)
            {
                Log.Warning("ProjectDashboard: CRM tenant config {TenantId} not found", projectInstance.CrmTenantConfigId);
                CrmConnectionFailed = true;
                return;
            }

            Log.Information(
                "ProjectDashboard: Loading CRM data from tenant {TenantName} for project {CrmProjectId}",
                tenantConfig.Name,
                projectInstance.CrmProjectId);

            // Use the event to load CRM data from the specific tenant
            if (OnLoadCrmDataFromTenant == null)
            {
                Log.Warning("ProjectDashboard: No handler for OnLoadCrmDataFromTenant event");
                CrmConnectionFailed = true;
                return;
            }

            var crmData = await OnLoadCrmDataFromTenant.Invoke(tenantConfig, projectInstance.CrmProjectId);
            var crmProject = crmData.Project;
            var milestones = crmData.Milestones;

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
                    var dt = DateTime.SpecifyKind(crmProject.StartDate.Value, DateTimeKind.Utc);
                    StartDate = new DateTimeOffset(dt);
                }

                if (crmProject.EndDate.HasValue)
                {
                    var dt = DateTime.SpecifyKind(crmProject.EndDate.Value, DateTimeKind.Utc);
                    TargetDate = new DateTimeOffset(dt);
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

                // Map customer name from CRM
                CustomerName = crmProject.ClientName;

                // TODO: Map these JTP-specific fields when CrmProject entity is extended
                // These are placeholders - actual field names will be discovered from CRM schema
                // Practice = crmProject.Practice;
                // ProjectType = crmProject.ProjectType;
                // PracticeDirector = crmProject.PracticeDirector;
                // AccountManager = crmProject.AccountManager;
                // SchedulingEngine = crmProject.SchedulingEngine;
                // SprintCommitments = crmProject.SprintCommitments;
                // EstimatedLabor = crmProject.EstimatedLabor ?? 0;
                // CostPercent = crmProject.CostPercent ?? 0;
                // SowDocumentUrl = crmProject.SowDocumentUrl;
                // ProjectStatusColor = crmProject.StatusColor ?? "Green";
            }
            else
            {
                CrmConnectionFailed = true;
            }

            // Map milestones to phases
            if (milestones.Count > 0)
            {
                Log.Information("ProjectDashboard: Retrieved {Count} milestones from CRM", milestones.Count);

                Phases.Clear();
                TotalMilestones = milestones.Count;
                MilestonesCompleted = milestones.Count(m => m.PercentComplete >= 100);

                // Find the next upcoming milestone (first incomplete milestone)
                var nextUpcomingMilestone = milestones
                    .Where(m => (m.PercentComplete ?? 0) < 100)
                    .OrderBy(m => m.DueDate)
                    .FirstOrDefault();

                if (nextUpcomingMilestone != null)
                {
                    NextMilestone = nextUpcomingMilestone.Name;
                    NextMilestoneDate = nextUpcomingMilestone.DueDate;
                }

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

            // Store CRM tasks for later - they'll be added in LoadProjectDataAsync after Clear()
            _pendingCrmTasks = crmData.Tasks.ToList();
            Log.Information("ProjectDashboard: Retrieved {Count} in-progress tasks from CRM", _pendingCrmTasks.Count);
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

    /// <summary>
    /// Event raised when a URL should be opened in the default browser.
    /// The MAUI layer subscribes to this to handle the platform-specific launch.
    /// </summary>
    public event Func<string, Task>? OnOpenUrl;

    /// <summary>
    /// Opens the SOW document in the default browser.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task OpenSowDocumentAsync()
    {
        if (!string.IsNullOrEmpty(SowDocumentUrl))
        {
            Log.Debug("Opening SOW document URL: {Url}", SowDocumentUrl);
            if (OnOpenUrl != null)
            {
                await OnOpenUrl.Invoke(SowDocumentUrl);
            }
        }
    }

    /// <summary>
    /// Opens the project in Dynamics 365.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task OpenInD365Async()
    {
        // TODO: Implement when we have CRM instance URL from tenant config
        Log.Debug("OpenInD365 requested for project {ProjectName}", ProjectName);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Tries to discover the local path for a project based on common conventions.
    /// Checks patterns like ~/Source/{org}.visualstudio.com/{project} for Azure DevOps
    /// and ~/Source/github.com.{user}/{repo} for GitHub.
    /// </summary>
    /// <param name="project">The project instance.</param>
    /// <returns>The discovered local path, or null if not found.</returns>
    private static string? TryDiscoverLocalPath(ProjectInstance project)
    {
        if (string.IsNullOrEmpty(project.SourceOrganization))
        {
            return null;
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sourceDir = Path.Combine(homeDir, "Source");

        if (!Directory.Exists(sourceDir))
        {
            return null;
        }

        // Try common path patterns based on source type
        var pathsToTry = new List<string>();

        if (project.SourceType == Domain.Enums.SourceType.AzureDevOps)
        {
            // Azure DevOps patterns: {org}.visualstudio.com/{project}, dev.azure.com.{org}/{project}
            var org = project.SourceOrganization;
            var proj = project.SourceProject ?? project.SourceRepository ?? string.Empty;

            pathsToTry.Add(Path.Combine(sourceDir, $"{org.ToLowerInvariant()}.visualstudio.com", proj));
            pathsToTry.Add(Path.Combine(sourceDir, $"dev.azure.com.{org.ToLowerInvariant()}", proj));
            pathsToTry.Add(Path.Combine(sourceDir, org, proj));
        }
        else if (project.SourceType == Domain.Enums.SourceType.GitHub)
        {
            // GitHub patterns: github.com.{user}/{repo}, github.com/{user}/{repo}
            var org = project.SourceOrganization;
            var repo = project.SourceRepository ?? string.Empty;

            pathsToTry.Add(Path.Combine(sourceDir, $"github.com.{org.ToLowerInvariant()}", repo));
            pathsToTry.Add(Path.Combine(sourceDir, "github.com", org, repo));
            pathsToTry.Add(Path.Combine(sourceDir, org, repo));
        }

        foreach (var path in pathsToTry)
        {
            // Check if path exists and contains a .git directory
            if (Directory.Exists(path) && Directory.Exists(Path.Combine(path, ".git")))
            {
                Log.Debug("ProjectDashboard: Found local repo at {Path}", path);
                return path;
            }
        }

        return null;
    }
}
