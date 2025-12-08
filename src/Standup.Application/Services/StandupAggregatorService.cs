using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Application.Services;

public class StandupAggregatorService : IStandupAggregatorService
{
    private readonly ISourceProviderFactory _providerFactory;

    public StandupAggregatorService(ISourceProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task<StandupData> AggregateDataAsync(
        IEnumerable<SourceRepository> repositories,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var commits = new List<CommitInfo>();
        var pullRequests = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        var tasks = repositories.Select(async repo =>
        {
            var provider = _providerFactory.GetProvider(repo.SourceType);

            var repoCommits = await provider.GetCommitsAsync(repo, since, until, cancellationToken);
            var repoPrs = await provider.GetOpenPullRequestsAsync(repo, cancellationToken);
            var repoWorkItems = await provider.GetInProgressWorkItemsAsync(repo, cancellationToken);
            var completedItems = await provider.GetCompletedWorkItemsAsync(repo, since, until, cancellationToken);

            return (
                Commits: repoCommits,
                PullRequests: repoPrs,
                WorkItems: repoWorkItems.Concat(completedItems)
            );
        });

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            commits.AddRange(result.Commits);
            pullRequests.AddRange(result.PullRequests);
            workItems.AddRange(result.WorkItems);
        }

        return new StandupData(
            Commits: commits.OrderByDescending(c => c.CommittedAt).ToList(),
            PullRequests: pullRequests.OrderByDescending(p => p.CreatedAt).ToList(),
            WorkItems: workItems.DistinctBy(w => w.Id).ToList());
    }
}
