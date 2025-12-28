// <copyright file="ProjectDashboardViewModel.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.Models;

namespace Standup.Application.ViewModels;

/// <summary>
/// ViewModel for the Project Dashboard page showing detailed project metrics and status.
/// </summary>
public partial class ProjectDashboardViewModel : ObservableObject
{
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
    private string lastSyncText = "2m ago";

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
    public ProjectDashboardViewModel()
    {
        LoadSampleData();
    }

    /// <summary>
    /// Loads the project data.
    /// </summary>
    /// <param name="projectInstance">The project instance to display.</param>
    public void LoadProject(ProjectInstance projectInstance)
    {
        Project = projectInstance;
        ProjectName = projectInstance.Name;
        Organization = projectInstance.SourceOrganization ?? "Unknown";
        SourceType = projectInstance.SourceType.ToString();

        // In a real app, we'd fetch this data from the API
        LoadSampleData();
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
