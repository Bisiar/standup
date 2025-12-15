using Standup.Application.DTOs;

namespace Standup.Infrastructure.Services;

/// <summary>
/// Aggregates multiple data source statuses into a single combined status.
/// </summary>
public static class DataSourceStatusAggregator
{
    /// <summary>
    /// Aggregates multiple source statuses into a single status.
    /// Uses worst-case status for each source type.
    /// </summary>
    /// <param name="statuses">The list of statuses to aggregate.</param>
    /// <returns>A combined status representing the worst case across all inputs.</returns>
    public static DataSourceStatus Aggregate(List<DataSourceStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            return DataSourceStatus.AllSuccess();
        }

        if (statuses.Count == 1)
        {
            return statuses[0];
        }

        // Aggregate by taking worst status for each source type
        var commitsStatus = GetWorstStatus(statuses.Select(s => s.CommitsStatus));
        var prsStatus = GetWorstStatus(statuses.Select(s => s.PullRequestsStatus));
        var workItemsStatus = GetWorstStatus(statuses.Select(s => s.WorkItemsStatus));

        // Aggregate error messages
        var commitsErrors = statuses.Where(s => s.CommitsError != null).Select(s => s.CommitsError).Distinct().ToList();
        var prsErrors = statuses.Where(s => s.PullRequestsError != null).Select(s => s.PullRequestsError).Distinct().ToList();
        var workItemsErrors = statuses.Where(s => s.WorkItemsError != null).Select(s => s.WorkItemsError).Distinct().ToList();

        return new DataSourceStatus(
            commitsStatus,
            prsStatus,
            workItemsStatus,
            commitsErrors.Count > 0 ? string.Join("; ", commitsErrors) : null,
            prsErrors.Count > 0 ? string.Join("; ", prsErrors) : null,
            workItemsErrors.Count > 0 ? string.Join("; ", workItemsErrors) : null);
    }

    /// <summary>
    /// Gets the worst status from a collection (Error > NoPat > NotApplicable > Success).
    /// </summary>
    /// <param name="statuses">The statuses to evaluate.</param>
    /// <returns>The worst status found in the collection.</returns>
    private static FetchStatus GetWorstStatus(IEnumerable<FetchStatus> statuses)
    {
        var statusList = statuses.ToList();
        if (statusList.Any(s => s == FetchStatus.Error))
        {
            return FetchStatus.Error;
        }

        if (statusList.Any(s => s == FetchStatus.NoPat))
        {
            return FetchStatus.NoPat;
        }

        if (statusList.Any(s => s == FetchStatus.NotApplicable))
        {
            return FetchStatus.NotApplicable;
        }

        return FetchStatus.Success;
    }
}
