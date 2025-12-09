using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Abstraction for source control providers (GitHub, Azure DevOps)
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    /// Gets commits for the specified repository and time range
    /// </summary>
    Task<IEnumerable<CommitInfo>> GetCommitsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets open pull requests authored by the user
    /// </summary>
    Task<IEnumerable<PullRequestInfo>> GetOpenPullRequestsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pull requests merged in the specified time range
    /// </summary>
    Task<IEnumerable<PullRequestInfo>> GetMergedPullRequestsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items in progress for the user
    /// </summary>
    Task<IEnumerable<WorkItemInfo>> GetInProgressWorkItemsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items completed in the specified time range
    /// </summary>
    Task<IEnumerable<WorkItemInfo>> GetCompletedWorkItemsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the repository configuration and credentials
    /// </summary>
    Task<bool> ValidateConnectionAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default);
}
