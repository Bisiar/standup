namespace Standup.Domain.Entities;

/// <summary>
/// Represents a project milestone from the CRM system.
/// </summary>
public record CrmMilestone(
    string MilestoneId,
    string ProjectId,
    string Name,
    DateTime DueDate)
{
    /// <summary>
    /// Gets the milestone status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the milestone description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the milestone completion percentage.
    /// </summary>
    public decimal? PercentComplete { get; init; }

    /// <summary>
    /// Gets the estimated effort in hours.
    /// </summary>
    public decimal? EffortEstimated { get; init; }

    /// <summary>
    /// Gets the completed effort in hours.
    /// </summary>
    public decimal? EffortCompleted { get; init; }

    /// <summary>
    /// Gets the remaining effort in hours.
    /// </summary>
    public decimal? EffortRemaining { get; init; }

    /// <summary>
    /// Gets a value indicating whether the milestone is overdue.
    /// </summary>
    public bool IsOverdue => DateTime.UtcNow > DueDate && Status != "Completed";

    /// <summary>
    /// Gets the number of days until the milestone is due (negative if overdue).
    /// </summary>
    public int DaysUntilDue => (DueDate - DateTime.UtcNow).Days;
}
