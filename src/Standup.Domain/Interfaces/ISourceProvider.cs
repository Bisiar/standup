using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Abstraction for source control providers (GitHub, Azure DevOps).
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    /// Gets commits for the specified repository and time range.
    /// </summary>
    /// <param name="repository">The source repository configuration.</param>
    /// <param name="since">The start of the time range.</param>
    /// <param name="until">The end of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of commits in the time range.</returns>
    Task<IEnumerable<CommitInfo>> GetCommitsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets open pull requests authored by the user.
    /// </summary>
    /// <param name="repository">The source repository configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of open pull requests.</returns>
    Task<IEnumerable<PullRequestInfo>> GetOpenPullRequestsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pull requests merged in the specified time range.
    /// </summary>
    /// <param name="repository">The source repository configuration.</param>
    /// <param name="since">The start of the time range.</param>
    /// <param name="until">The end of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of merged pull requests in the time range.</returns>
    Task<IEnumerable<PullRequestInfo>> GetMergedPullRequestsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items in progress for the user.
    /// </summary>
    /// <param name="repository">The source repository configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of work items in progress.</returns>
    Task<IEnumerable<WorkItemInfo>> GetInProgressWorkItemsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets work items completed in the specified time range.
    /// </summary>
    /// <param name="repository">The source repository configuration.</param>
    /// <param name="since">The start of the time range.</param>
    /// <param name="until">The end of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of completed work items in the time range.</returns>
    Task<IEnumerable<WorkItemInfo>> GetCompletedWorkItemsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the repository configuration and credentials.
    /// </summary>
    /// <param name="repository">The source repository configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection is valid, false otherwise.</returns>
    Task<bool> ValidateConnectionAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default);
}
