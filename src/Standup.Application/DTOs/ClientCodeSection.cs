using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.DTOs;

/// <summary>
/// A section of the report for a single client code.
/// Contains multiple summaries indexed by type - preserves previous summaries when regenerating.
/// </summary>
public record ClientCodeSection(
    string ClientCode,
    string? Summary,
    List<CommitInfo> Commits,
    List<PullRequestInfo> PullRequests,
    List<WorkItemInfo> WorkItems,
    Dictionary<SummaryType, string>? AllSummaries = null,
    DataSourceStatus? SourceStatus = null)
{
    public int CommitCount => Commits.Count;
    public int PullRequestCount => PullRequests.Count;
    public int WorkItemCount => WorkItems.Count;

    /// <summary>
    /// Gets summary for a specific type, or falls back to current Summary.
    /// </summary>
    /// <param name="type">The summary type to retrieve.</param>
    /// <returns>The typed summary if available, otherwise the default summary.</returns>
    public string? GetSummary(SummaryType type)
    {
        if (AllSummaries?.TryGetValue(type, out var typedSummary) == true)
        {
            return typedSummary;
        }

        return Summary;
    }

    /// <summary>
    /// Returns a new section with the summary added to AllSummaries dictionary.
    /// </summary>
    /// <param name="type">The summary type.</param>
    /// <param name="summary">The summary text.</param>
    /// <returns>A new section with the summary added.</returns>
    public ClientCodeSection WithTypedSummary(SummaryType type, string summary)
    {
        var summaries = AllSummaries != null
            ? new Dictionary<SummaryType, string>(AllSummaries)
            : new Dictionary<SummaryType, string>();
        summaries[type] = summary;
        return this with { Summary = summary, AllSummaries = summaries };
    }
}
