using Standup.Application.DTOs;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.Interfaces;

/// <summary>
/// Service for generating standup reports from local or remote repositories.
/// Supports both API-based access (with PAT) and local git log reading (no PAT needed).
/// </summary>
public interface ILocalStandupService
{
    /// <summary>
    /// Generates a standup report from a remote repository using API access.
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="project">The project name.</param>
    /// <param name="repository">The repository name.</param>
    /// <param name="pat">The personal access token.</param>
    /// <param name="authorIdentifier">Optional author filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated standup report.</returns>
    Task<StandupReportDto> GenerateStandupAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        string? authorIdentifier = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a standup report from a local git repository.
    /// No PAT required for commits - reads directly from git log.
    /// Optional PAT enables PR and work item fetching from remote.
    /// </summary>
    /// <param name="localPath">The local path to the git repository.</param>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="project">The project name.</param>
    /// <param name="repository">The repository name.</param>
    /// <param name="pat">Optional personal access token for PR/work item access.</param>
    /// <param name="authorIdentifier">Optional author filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated standup report.</returns>
    Task<StandupReportDto> GenerateStandupFromLocalAsync(
        string localPath,
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string? pat = null,
        string? authorIdentifier = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates connection to a remote repository.
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="project">The project name.</param>
    /// <param name="repository">The repository name.</param>
    /// <param name="pat">The personal access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is valid, false otherwise.</returns>
    Task<bool> ValidateConnectionAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a grouped standup report for all repositories in a group.
    /// Results are grouped by client code.
    /// </summary>
    /// <param name="group">The repository group containing repos to process.</param>
    /// <param name="getPatForRepo">Function to get decrypted PAT for a repository.</param>
    /// <param name="since">Start date for the report period.</param>
    /// <param name="until">End date for the report period.</param>
    /// <param name="summaryType">The type of AI summary to generate (Technical, Executive, or CodeReview).</param>
    /// <param name="progress">Optional progress reporter for status updates (percentage 0-1, message).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A grouped standup report with sections per client code.</returns>
    Task<GroupedStandupReportDto> GenerateGroupedStandupAsync(
        RepositoryGroup group,
        Func<GroupedRepository, Task<string?>> getPatForRepo,
        DateTimeOffset since,
        DateTimeOffset until,
        SummaryType summaryType = SummaryType.Technical,
        IProgress<(double Progress, string Message)>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates summaries for all summary types on an existing report.
    /// Preserves all summaries in the AllSummaries dictionary.
    /// </summary>
    /// <param name="existingReport">The existing report with data to summarize.</param>
    /// <param name="summaryTypes">The summary types to generate. Defaults to all types.</param>
    /// <param name="progress">Optional progress reporter for status updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The report with all requested summary types generated.</returns>
    Task<GroupedStandupReportDto> GenerateAllSummaryTypesAsync(
        GroupedStandupReportDto existingReport,
        IEnumerable<SummaryType>? summaryTypes = null,
        IProgress<(double Progress, string Message)>? progress = null,
        CancellationToken cancellationToken = default);
}
