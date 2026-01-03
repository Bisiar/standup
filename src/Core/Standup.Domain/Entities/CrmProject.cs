namespace Standup.Domain.Entities;

/// <summary>
/// Represents a project from the CRM system (Dynamics 365).
/// </summary>
public record CrmProject(
    string CrmProjectId,
    string ClientCode,
    string ProjectName,
    string ClientName,
    string Status)
{
    /// <summary>
    /// Gets the project start date.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Gets the project end date.
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Gets the current project phase or stage.
    /// </summary>
    public string? CurrentPhase { get; init; }

    /// <summary>
    /// Gets the project manager name.
    /// </summary>
    public string? ProjectManager { get; init; }

    /// <summary>
    /// Gets the project completion percentage.
    /// </summary>
    public decimal? PercentComplete { get; init; }

    /// <summary>
    /// Gets the total budgeted hours for the project.
    /// </summary>
    public decimal? BudgetHours { get; init; }

    /// <summary>
    /// Gets the hours consumed on the project.
    /// </summary>
    public decimal? HoursUsed { get; init; }

    /// <summary>
    /// Gets the practice area (e.g., ERP, Azure, Power Platform, M365).
    /// </summary>
    public string? Practice { get; init; }

    /// <summary>
    /// Gets the project type (e.g., Customer, Internal).
    /// </summary>
    public string? ProjectType { get; init; }

    /// <summary>
    /// Gets the practice director name.
    /// </summary>
    public string? PracticeDirector { get; init; }

    /// <summary>
    /// Gets the account manager name.
    /// </summary>
    public string? AccountManager { get; init; }

    /// <summary>
    /// Gets the scheduling engine type (e.g., "Project for the Web").
    /// </summary>
    public string? SchedulingEngine { get; init; }

    /// <summary>
    /// Gets the sprint commitments description.
    /// </summary>
    public string? SprintCommitments { get; init; }

    /// <summary>
    /// Gets the next milestone name.
    /// </summary>
    public string? NextMilestone { get; init; }

    /// <summary>
    /// Gets the next milestone due date.
    /// </summary>
    public DateTime? NextMilestoneDate { get; init; }

    /// <summary>
    /// Gets the estimated labor cost in dollars.
    /// </summary>
    public decimal? EstimatedLabor { get; init; }

    /// <summary>
    /// Gets the cost percentage (actual/budget).
    /// </summary>
    public decimal? CostPercent { get; init; }

    /// <summary>
    /// Gets the SharePoint URL to the Statement of Work document.
    /// </summary>
    public string? SowDocumentUrl { get; init; }

    /// <summary>
    /// Gets the full customer/account name.
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Gets the project status indicator color (Green, Yellow, Red).
    /// </summary>
    public string? ProjectStatusColor { get; init; }

    /// <summary>
    /// Gets a value indicating whether the project is on track (budget vs used).
    /// </summary>
    public bool IsOnTrack => BudgetHours.HasValue && HoursUsed.HasValue
        ? HoursUsed.Value <= BudgetHours.Value
        : true;

    /// <summary>
    /// Gets the budget utilization percentage.
    /// </summary>
    public decimal? BudgetUtilization => BudgetHours.HasValue && BudgetHours.Value > 0
        ? (HoursUsed ?? 0) / BudgetHours.Value * 100
        : null;
}
