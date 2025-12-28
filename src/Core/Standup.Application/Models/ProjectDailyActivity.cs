namespace Standup.Application.Models;

/// <summary>
/// Represents daily activity for a specific project for multi-line charts.
/// </summary>
public class ProjectDailyActivity
{
    /// <summary>
    /// Gets or sets the project name for grouping.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the total activity (additions + deletions).
    /// </summary>
    public int TotalActivity { get; set; }

    /// <summary>
    /// Gets or sets the number of commits.
    /// </summary>
    public int CommitCount { get; set; }

    /// <summary>
    /// Gets the display label for the date.
    /// </summary>
    public string DateLabel => Date.ToString("MMM dd");
}
