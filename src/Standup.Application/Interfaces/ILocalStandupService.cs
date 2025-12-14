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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A grouped standup report with sections per client code.</returns>
    Task<GroupedStandupReportDto> GenerateGroupedStandupAsync(
        RepositoryGroup group,
        Func<GroupedRepository, Task<string?>> getPatForRepo,
        DateTimeOffset since,
        DateTimeOffset until,
        SummaryType summaryType = SummaryType.Technical,
        CancellationToken cancellationToken = default);
}
