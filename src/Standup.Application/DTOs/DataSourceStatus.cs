namespace Standup.Application.DTOs;

/// <summary>
/// Tracks the status of data fetching from various sources.
/// </summary>
public record DataSourceStatus(
    FetchStatus CommitsStatus,
    FetchStatus PullRequestsStatus,
    FetchStatus WorkItemsStatus,
    string? CommitsError = null,
    string? PullRequestsError = null,
    string? WorkItemsError = null)
{
    /// <summary>
    /// Creates a status indicating all sources were successful.
    /// </summary>
    /// <returns>A new DataSourceStatus with all sources set to Success.</returns>
    public static DataSourceStatus AllSuccess() => new(
        FetchStatus.Success,
        FetchStatus.Success,
        FetchStatus.Success);

    /// <summary>
    /// Creates a status for local-only mode (commits from git, no PAT for PRs/work items).
    /// </summary>
    /// <returns>A new DataSourceStatus with commits Success and PRs/work items NoPat.</returns>
    public static DataSourceStatus LocalOnly() => new(
        FetchStatus.Success,
        FetchStatus.NoPat,
        FetchStatus.NoPat);

    /// <summary>
    /// Gets a value indicating whether any data source had an error.
    /// </summary>
    public bool HasErrors =>
        CommitsStatus == FetchStatus.Error ||
        PullRequestsStatus == FetchStatus.Error ||
        WorkItemsStatus == FetchStatus.Error;

    /// <summary>
    /// Gets a value indicating whether any data source is missing due to no PAT.
    /// </summary>
    public bool HasMissingPat =>
        CommitsStatus == FetchStatus.NoPat ||
        PullRequestsStatus == FetchStatus.NoPat ||
        WorkItemsStatus == FetchStatus.NoPat;
}
