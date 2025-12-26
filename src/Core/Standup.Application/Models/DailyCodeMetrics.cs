namespace Standup.Application.Models;

/// <summary>
/// Represents daily code metrics for dashboard line/bar charts.
/// </summary>
public class DailyCodeMetrics
{
    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the number of lines added.
    /// </summary>
    public int Additions { get; set; }

    /// <summary>
    /// Gets or sets the number of lines deleted.
    /// </summary>
    public int Deletions { get; set; }

    /// <summary>
    /// Gets or sets the number of commits.
    /// </summary>
    public int CommitCount { get; set; }

    /// <summary>
    /// Gets the total lines changed (additions + deletions).
    /// </summary>
    public int TotalChanges => Additions + Deletions;

    /// <summary>
    /// Gets the display label for the date.
    /// </summary>
    public string DateLabel => Date.ToString("MMM dd");
}
