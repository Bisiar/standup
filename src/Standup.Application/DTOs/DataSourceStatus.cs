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
    public static DataSourceStatus AllSuccess() => new(
        FetchStatus.Success,
        FetchStatus.Success,
        FetchStatus.Success);

    /// <summary>
    /// Creates a status for local-only mode (commits from git, no PAT for PRs/work items).
    /// </summary>
    public static DataSourceStatus LocalOnly() => new(
        FetchStatus.Success,
        FetchStatus.NoPat,
        FetchStatus.NoPat);

    /// <summary>
    /// Returns true if any data source had an error.
    /// </summary>
    public bool HasErrors =>
        CommitsStatus == FetchStatus.Error ||
        PullRequestsStatus == FetchStatus.Error ||
        WorkItemsStatus == FetchStatus.Error;

    /// <summary>
    /// Returns true if any data source is missing due to no PAT.
    /// </summary>
    public bool HasMissingPat =>
        CommitsStatus == FetchStatus.NoPat ||
        PullRequestsStatus == FetchStatus.NoPat ||
        WorkItemsStatus == FetchStatus.NoPat;
}

/// <summary>
/// Status of a data fetch operation.
/// </summary>
public enum FetchStatus
{
    /// <summary>
    /// Data was fetched successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Data fetch failed with an error.
    /// </summary>
    Error,

    /// <summary>
    /// Data not fetched because no PAT was provided.
    /// </summary>
    NoPat,

    /// <summary>
    /// Data source not applicable for this repository type.
    /// </summary>
    NotApplicable
}
