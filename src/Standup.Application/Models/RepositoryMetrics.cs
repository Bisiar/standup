namespace Standup.Application.Models;

/// <summary>
/// Represents code metrics grouped by repository for bar charts.
/// </summary>
public class RepositoryMetrics
{
    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string Repository { get; set; } = string.Empty;

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
    /// Gets the total lines changed.
    /// </summary>
    public int TotalChanges => Additions + Deletions;

    /// <summary>
    /// Gets the short repository name for display.
    /// </summary>
    public string ShortName => Repository.Length > 20 ? Repository[..17] + "..." : Repository;
}
