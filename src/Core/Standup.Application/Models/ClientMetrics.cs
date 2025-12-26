namespace Standup.Application.Models;

/// <summary>
/// Represents code metrics grouped by client code for pie charts.
/// </summary>
public class ClientMetrics
{
    /// <summary>
    /// Gets or sets the client code.
    /// </summary>
    public string ClientCode { get; set; } = string.Empty;

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
}
