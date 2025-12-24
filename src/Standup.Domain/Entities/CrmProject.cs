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
