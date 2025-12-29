namespace Standup.Application.Models;

/// <summary>
/// Metrics aggregated by client code for dashboard visualization.
/// </summary>
public class ClientCodeMetrics
{
    /// <summary>
    /// Gets or sets the client code (e.g., "ELGP", "MERT", "General").
    /// </summary>
    public string ClientCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repository name for navigation.
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Gets or sets the total lines added across all commits for this client.
    /// </summary>
    public int Additions { get; set; }

    /// <summary>
    /// Gets or sets the total lines deleted across all commits for this client.
    /// </summary>
    public int Deletions { get; set; }

    /// <summary>
    /// Gets or sets the number of commits for this client.
    /// </summary>
    public int CommitCount { get; set; }

    /// <summary>
    /// Gets the total lines changed (additions + deletions).
    /// </summary>
    public int TotalChanges => Additions + Deletions;

    /// <summary>
    /// Gets the net change (additions - deletions).
    /// </summary>
    public int NetChange => Additions - Deletions;

    /// <summary>
    /// Gets the additions as a percentage of total changes (0-100).
    /// </summary>
    public double AdditionsPercent => TotalChanges > 0 ? (double)Additions / TotalChanges * 100 : 0;

    /// <summary>
    /// Gets the deletions as a percentage of total changes (0-100).
    /// </summary>
    public double DeletionsPercent => TotalChanges > 0 ? (double)Deletions / TotalChanges * 100 : 0;

    /// <summary>
    /// Gets a display color for this client code (for charts).
    /// </summary>
    public string Color => ClientCode.ToUpperInvariant() switch
    {
        "ELGP" => "#3B82F6", // Blue
        "LDSC" => "#8B5CF6", // Purple
        "MERT" => "#EF4444", // Red
        "JTOP" => "#F59E0B", // Amber
        "GENERAL" => "#64748B", // Slate
        _ => "#10B981", // Green (default)
    };
}
