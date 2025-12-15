using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Service for generating AI-powered summaries of standup data.
/// </summary>
public interface IAISummaryService
{
    /// <summary>
    /// Generates an AI summary from standup data.
    /// </summary>
    /// <param name="data">The standup data to summarize.</param>
    /// <param name="options">Optional summary configuration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated summary text.</returns>
    Task<string> GenerateSummaryAsync(
        StandupData data,
        SummaryOptions? options = null,
        CancellationToken cancellationToken = default);
}
