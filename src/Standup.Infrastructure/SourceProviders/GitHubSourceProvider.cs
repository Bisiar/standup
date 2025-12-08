using Octokit;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Infrastructure.SourceProviders;

public class GitHubSourceProvider : ISourceProvider
{
    private readonly IEncryptionService _encryptionService;

    public GitHubSourceProvider(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public async Task<IEnumerable<CommitInfo>> GetCommitsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(repository);

        var commits = await client.Repository.Commit.GetAll(
            repository.Organization,
            repository.Repository,
            new CommitRequest
            {
                Author = repository.AuthorIdentifier,
                Since = since,
                Until = until
            });

        var results = new List<CommitInfo>();

        foreach (var commit in commits)
        {
            var detailed = await client.Repository.Commit.Get(
                repository.Organization,
                repository.Repository,
                commit.Sha);

            results.Add(new CommitInfo(
                Sha: commit.Sha,
                Message: commit.Commit.Message,
                Repository: repository.FullPath,
                SourceType: SourceType.GitHub,
                CommittedAt: commit.Commit.Author.Date,
                FilesChanged: detailed.Files?.Select(f => f.Filename).ToList(),
                Additions: detailed.Stats?.Additions ?? 0,
                Deletions: detailed.Stats?.Deletions ?? 0));
        }

        return results;
    }

    public async Task<IEnumerable<PullRequestInfo>> GetOpenPullRequestsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(repository);

        var prs = await client.PullRequest.GetAllForRepository(
            repository.Organization,
            repository.Repository,
            new PullRequestRequest
            {
                State = ItemStateFilter.Open
            });

        return prs
            .Where(pr => pr.User.Login.Equals(repository.AuthorIdentifier, StringComparison.OrdinalIgnoreCase))
            .Select(pr => new PullRequestInfo(
                Id: pr.Number.ToString(),
                Title: pr.Title,
                Repository: repository.FullPath,
                SourceType: SourceType.GitHub,
                Status: pr.State.StringValue,
                Url: pr.HtmlUrl,
                CreatedAt: pr.CreatedAt,
                Description: pr.Body,
                IsDraft: pr.Draft,
                ReviewerCount: pr.RequestedReviewers?.Count ?? 0));
    }

    public async Task<IEnumerable<PullRequestInfo>> GetMergedPullRequestsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var client = await CreateClientAsync(repository);

        var prs = await client.PullRequest.GetAllForRepository(
            repository.Organization,
            repository.Repository,
            new PullRequestRequest
            {
                State = ItemStateFilter.Closed
            });

        return prs
            .Where(pr => pr.User.Login.Equals(repository.AuthorIdentifier, StringComparison.OrdinalIgnoreCase)
                         && pr.MergedAt.HasValue
                         && pr.MergedAt.Value >= since
                         && pr.MergedAt.Value <= until)
            .Select(pr => new PullRequestInfo(
                Id: pr.Number.ToString(),
                Title: pr.Title,
                Repository: repository.FullPath,
                SourceType: SourceType.GitHub,
                Status: "Merged",
                Url: pr.HtmlUrl,
                CreatedAt: pr.CreatedAt,
                Description: pr.Body,
                IsDraft: false,
                ReviewerCount: pr.RequestedReviewers?.Count ?? 0));
    }

    public Task<IEnumerable<WorkItemInfo>> GetInProgressWorkItemsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default)
    {
        // GitHub doesn't have native work items, return empty
        // Could potentially integrate with GitHub Issues in the future
        return Task.FromResult(Enumerable.Empty<WorkItemInfo>());
    }

    public Task<IEnumerable<WorkItemInfo>> GetCompletedWorkItemsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<WorkItemInfo>());
    }

    public async Task<bool> ValidateConnectionAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await CreateClientAsync(repository);
            await client.Repository.Get(repository.Organization, repository.Repository);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<GitHubClient> CreateClientAsync(SourceRepository repository)
    {
        var client = new GitHubClient(new ProductHeaderValue("Standup-App"));

        if (!string.IsNullOrEmpty(repository.EncryptedPat))
        {
            var pat = await _encryptionService.DecryptAsync(repository.EncryptedPat);
            client.Credentials = new Credentials(pat);
        }

        return client;
    }
}
