// <copyright file="ProjectDashboardViewModel.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.ValueObjects;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for the Project Dashboard page showing detailed project metrics and status.
/// </summary>
public partial class ProjectDashboardViewModel : ObservableObject
{
    private readonly ISourceProviderFactory? _sourceProviderFactory;
    private readonly IEncryptionService? _encryptionService;
    private readonly ILocalStandupService? _localStandupService;

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
    private int overallProgressPercent = 68;

    [ObservableProperty]
    private int milestonesCompleted = 8;

    [ObservableProperty]
    private int totalMilestones = 12;

    [ObservableProperty]
    private int currentSprintNumber = 8;

    [ObservableProperty]
    private int totalSprints = 12;

    [ObservableProperty]
    private int sprintDaysRemaining = 5;

    [ObservableProperty]
    private int tasksCompleted = 147;

    [ObservableProperty]
    private int totalTasks = 186;

    [ObservableProperty]
    private int taskCompletionPercent = 79;

    [ObservableProperty]
    private decimal budgetUsed = 124.5m;

    [ObservableProperty]
    private decimal budgetTotal = 185m;

    [ObservableProperty]
    private int budgetPercent = 67;

    [ObservableProperty]
    private int daysRemaining = 42;

    // Sprint Progress
    [ObservableProperty]
    private string sprintDates = "Dec 16 - Dec 30, 2025";

    [ObservableProperty]
    private int sprintDoneCount = 18;

    [ObservableProperty]
    private int sprintReviewCount = 6;

    [ObservableProperty]
    private int sprintProgressCount = 8;

    [ObservableProperty]
    private int sprintTodoCount = 8;

    [ObservableProperty]
    private double sprintDonePercent = 45;

    [ObservableProperty]
    private double sprintReviewPercent = 15;

    [ObservableProperty]
    private double sprintProgressPercent = 20;

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
    public ProjectDashboardViewModel(
        ISourceProviderFactory? sourceProviderFactory = null,
        IEncryptionService? encryptionService = null,
        ILocalStandupService? localStandupService = null)
    {
        _sourceProviderFactory = sourceProviderFactory;
        _encryptionService = encryptionService;
        _localStandupService = localStandupService;
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

    private static string GetInitials(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "??";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
        }

        return name.Length >= 2 ? name[..2].ToUpperInvariant() : name.ToUpperInvariant();
    }

    private static string GetRelativeTime(DateTimeOffset timestamp)
    {
        var diff = DateTimeOffset.UtcNow - timestamp;

        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}m ago";
        }

        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}h ago";
        }

        if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}d ago";
        }

        return timestamp.ToString("MMM dd");
    }

    private static TaskPriority MapPriority(List<string>? tags)
    {
        if (tags == null)
        {
            return TaskPriority.Medium;
        }

        if (tags.Any(t => t.Contains("high", StringComparison.OrdinalIgnoreCase) ||
                         t.Contains("critical", StringComparison.OrdinalIgnoreCase)))
        {
            return TaskPriority.High;
        }

        if (tags.Any(t => t.Contains("low", StringComparison.OrdinalIgnoreCase)))
        {
            return TaskPriority.Low;
        }

        return TaskPriority.Medium;
    }

    private async Task LoadProjectDataAsync(ProjectInstance projectInstance)
    {
        // Check data sources: LocalPath for commits (no PAT), PAT for PRs/work items
        bool hasLocalPath = !string.IsNullOrEmpty(projectInstance.LocalPath);
        bool hasPat = !string.IsNullOrEmpty(projectInstance.SourcePat);

        if (!hasLocalPath && !hasPat)
        {
            Log.Warning("ProjectDashboard: No LocalPath or PAT configured for {ProjectName}, using sample data", projectInstance.Name);
            LoadSampleData();
            return;
        }

        IsLoading = true;

        try
        {
            Log.Information(
                "ProjectDashboard: Loading data for {ProjectName} (LocalPath: {HasLocal}, PAT: {HasPat})",
                projectInstance.Name,
                hasLocalPath,
                hasPat);

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

                var sourceRepo = new SourceRepository
                {
                    Id = projectInstance.Id,
                    SourceType = projectInstance.SourceType,
                    Organization = projectInstance.SourceOrganization ?? string.Empty,
                    Project = projectInstance.SourceProject,
                    Repository = projectInstance.SourceRepository ?? string.Empty,
                    AuthorIdentifier = projectInstance.AuthorIdentifier ?? string.Empty,
                    EncryptedPat = _encryptionService.Encrypt(projectInstance.SourcePat!),
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
                }
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

            // Map to UI collections
            MapRecentActivity(commits, prs, completed);
            MapInProgressTasks(inProgress);
            MapInReviewTasks(prs);
            MapTeamMembers(commits);
            UpdateConnectedServices(projectInstance.SourceType, hasLocalPath, hasPat);

            LastSyncText = "Just now";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProjectDashboard: Failed to load data for project {ProjectName}", projectInstance.Name);
            LoadSampleData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void MapRecentActivity(List<CommitInfo> commits, List<PullRequestInfo> prs, List<WorkItemInfo> completed)
    {
        RecentActivity.Clear();

        // Add commits as activity
        foreach (var commit in commits.OrderByDescending(c => c.CommittedAt).Take(5))
        {
            RecentActivity.Add(new ProjectActivity(
                ActivityType.Commit,
                commit.Author ?? "Unknown",
                commit.Subject,
                GetRelativeTime(commit.CommittedAt)));
        }

        // Add completed work items
        foreach (var item in completed.Take(3))
        {
            RecentActivity.Add(new ProjectActivity(
                ActivityType.Task,
                item.AssignedTo ?? "Unknown",
                $"completed {item.Title}",
                "Recently"));
        }

        // Add PRs
        foreach (var pr in prs.OrderByDescending(p => p.CreatedAt).Take(2))
        {
            RecentActivity.Add(new ProjectActivity(
                ActivityType.PullRequest,
                "Developer",
                $"opened PR #{pr.Id}: {pr.Title}",
                GetRelativeTime(pr.CreatedAt)));
        }
    }

    private void MapInProgressTasks(List<WorkItemInfo> inProgress)
    {
        InProgressTasks.Clear();

        foreach (var item in inProgress.Take(5))
        {
            var initials = GetInitials(item.AssignedTo);
            InProgressTasks.Add(new ProjectTask(
                item.Id,
                item.Title,
                initials,
                item.AssignedTo ?? "Unassigned",
                MapPriority(item.Tags),
                DateTimeOffset.UtcNow.AddDays(7))); // Default due date
        }
    }

    private void MapInReviewTasks(List<PullRequestInfo> prs)
    {
        InReviewTasks.Clear();

        foreach (var pr in prs.Take(5))
        {
            InReviewTasks.Add(new ProjectTask(
                $"PR-{pr.Id}",
                pr.Title,
                "PR",
                "Reviewer",
                pr.IsDraft ? TaskPriority.Low : TaskPriority.Medium,
                pr.CreatedAt.AddDays(3)));
        }
    }

    private void MapTeamMembers(List<CommitInfo> commits)
    {
        TeamMembers.Clear();

        // Extract unique authors from commits
        var authorStats = commits
            .Where(c => !string.IsNullOrEmpty(c.Author))
            .GroupBy(c => c.Author!)
            .Select(g => new { Author = g.Key, CommitCount = g.Count() })
            .OrderByDescending(x => x.CommitCount)
            .Take(5);

        foreach (var author in authorStats)
        {
            var initials = GetInitials(author.Author);
            TeamMembers.Add(new TeamMember(
                author.Author,
                initials,
                "Developer",
                TeamRole.Developer,
                author.CommitCount,
                MemberStatus.Active));
        }
    }

    private void UpdateConnectedServices(Domain.Enums.SourceType projectSourceType, bool hasLocalPath, bool hasPat)
    {
        ConnectedServices.Clear();

        // Local Git connection
        if (hasLocalPath)
        {
            ConnectedServices.Add(new ConnectedService("Local Git", "📁", ServiceStatus.Connected));
        }

        // Remote source connection - only show if PAT is configured
        if (hasPat)
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
    }

    /// <summary>
    /// Gets the command to go back to the projects list.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        // Navigation will be handled by the page
    }

    /// <summary>
    /// Gets the command to view the Gantt chart.
    /// </summary>
    [RelayCommand]
    private void ViewGantt()
    {
        // Future feature
    }

    /// <summary>
    /// Gets the command to view the task board.
    /// </summary>
    [RelayCommand]
    private void ViewBoard()
    {
        // Future feature
    }

    private void LoadSampleData()
    {
        // Sample phases
        Phases.Clear();
        Phases.Add(new ProjectPhase("Phase 1: Foundation", "Jan 15 - Feb 28", PhaseStatus.Complete));
        Phases.Add(new ProjectPhase("Phase 2: Core Agents", "Mar 1 - Apr 15", PhaseStatus.Complete));
        Phases.Add(new ProjectPhase("Phase 3: Orchestration", "Apr 16 - May 31", PhaseStatus.InProgress));
        Phases.Add(new ProjectPhase("Phase 4: Production", "Jun 1 - Jun 30", PhaseStatus.Pending));

        // Sample team members
        TeamMembers.Clear();
        TeamMembers.Add(new TeamMember("James Mitchell", "JM", "Lead Architect", TeamRole.Lead, 12, MemberStatus.Active));
        TeamMembers.Add(new TeamMember("Sarah Chen", "SC", "Backend Developer", TeamRole.Developer, 8, MemberStatus.Active));
        TeamMembers.Add(new TeamMember("Marcus Johnson", "MJ", "Frontend Developer", TeamRole.Developer, 6, MemberStatus.Away));
        TeamMembers.Add(new TeamMember("Emily Rodriguez", "ER", "QA Engineer", TeamRole.QA, 15, MemberStatus.Active));
        TeamMembers.Add(new TeamMember("David Kim", "DK", "DevOps Engineer", TeamRole.DevOps, 4, MemberStatus.Active));

        // Sample tasks
        InProgressTasks.Clear();
        InProgressTasks.Add(new ProjectTask("SHERP-342", "Implement multi-agent orchestration with Azure AI Foundry", "JM", "James M.", TaskPriority.High, new DateTimeOffset(2025, 12, 28, 0, 0, 0, TimeSpan.Zero)));
        InProgressTasks.Add(new ProjectTask("SHERP-345", "Add storage queue integration for async processing", "SC", "Sarah C.", TaskPriority.Medium, new DateTimeOffset(2025, 12, 29, 0, 0, 0, TimeSpan.Zero)));
        InProgressTasks.Add(new ProjectTask("SHERP-348", "Create MCP server endpoint documentation", "MJ", "Marcus J.", TaskPriority.Low, new DateTimeOffset(2025, 12, 30, 0, 0, 0, TimeSpan.Zero)));

        InReviewTasks.Clear();
        InReviewTasks.Add(new ProjectTask("SHERP-338", "Azure Functions v4 isolated worker migration", "DK", "David K.", TaskPriority.High, new DateTimeOffset(2025, 12, 25, 0, 0, 0, TimeSpan.Zero)) { IsOverdue = true });
        InReviewTasks.Add(new ProjectTask("SHERP-340", "Integration tests for agent communication", "ER", "Emily R.", TaskPriority.Medium, new DateTimeOffset(2025, 12, 27, 0, 0, 0, TimeSpan.Zero)));

        // Sample activity
        RecentActivity.Clear();
        RecentActivity.Add(new ProjectActivity(ActivityType.Commit, "James M.", "pushed 3 commits to feature/agent-orchestration", "15 minutes ago"));
        RecentActivity.Add(new ProjectActivity(ActivityType.Task, "Sarah C.", "completed SHERP-336", "1 hour ago"));
        RecentActivity.Add(new ProjectActivity(ActivityType.PullRequest, "David K.", "opened PR #127 for Azure Functions migration", "2 hours ago"));
        RecentActivity.Add(new ProjectActivity(ActivityType.Deployment, string.Empty, "Deployed v0.8.4 to staging environment", "4 hours ago"));

        // Connected services
        ConnectedServices.Clear();
        ConnectedServices.Add(new ConnectedService("Azure DevOps", "🔷", ServiceStatus.Connected));
        ConnectedServices.Add(new ConnectedService("GitHub", "🐙", ServiceStatus.Syncing));
        ConnectedServices.Add(new ConnectedService("Azure Portal", "☁️", ServiceStatus.Connected));
    }
}
