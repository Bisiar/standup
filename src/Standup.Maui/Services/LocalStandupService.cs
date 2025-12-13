using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Infrastructure.SourceProviders;
using System.Text;

namespace Standup.Maui.Services;

/// <summary>
/// Local standup generation using Infrastructure source providers directly.
/// </summary>
public sealed class LocalStandupService : ILocalStandupService
{
    private readonly IEncryptionService _encryptionService;

    public LocalStandupService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public async Task<StandupReportDto> GenerateStandupAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        string? authorIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Generating local standup for {Org}/{Project}/{Repo}", organization, project, repository);

        var sourceRepo = new SourceRepository
        {
            Organization = organization,
            Project = project,
            Repository = repository,
            SourceType = sourceType,
            AuthorIdentifier = authorIdentifier ?? string.Empty,
            EncryptedPat = _encryptionService.Encrypt(pat)
        };

        // Date range for display purposes only - commits are fetched by recency, not date
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        try
        {
            var provider = sourceType == SourceType.AzureDevOps
                ? (Domain.Interfaces.ISourceProvider)new AzureDevOpsSourceProvider(_encryptionService)
                : new GitHubSourceProvider(_encryptionService);

            Log.Information("Fetching commits from {Since} to {Until}", since, until);
            var commits = (await provider.GetCommitsAsync(sourceRepo, since, until, cancellationToken)).ToList();
            Log.Information("Found {Count} commits", commits.Count);

            Log.Information("Fetching open PRs");
            var openPrs = (await provider.GetOpenPullRequestsAsync(sourceRepo, cancellationToken)).ToList();
            Log.Information("Found {Count} open PRs", openPrs.Count);

            Log.Information("Fetching merged PRs");
            var mergedPrs = (await provider.GetMergedPullRequestsAsync(sourceRepo, since, until, cancellationToken)).ToList();
            Log.Information("Found {Count} merged PRs", mergedPrs.Count);

            Log.Information("Fetching in-progress work items");
            var inProgressItems = (await provider.GetInProgressWorkItemsAsync(sourceRepo, cancellationToken)).ToList();
            Log.Information("Found {Count} in-progress items", inProgressItems.Count);

            Log.Information("Fetching completed work items");
            var completedItems = (await provider.GetCompletedWorkItemsAsync(sourceRepo, since, until, cancellationToken)).ToList();
            Log.Information("Found {Count} completed items", completedItems.Count);

            var allPrs = openPrs.Concat(mergedPrs).ToList();
            var allWorkItems = inProgressItems.Concat(completedItems).DistinctBy(w => w.Id).ToList();

            var summary = BuildSummary(commits, allPrs, allWorkItems, since, until);

            return new StandupReportDto(
                Id: Guid.NewGuid().ToString(),
                Summary: summary,
                PeriodStart: since,
                PeriodEnd: until,
                GeneratedAt: DateTimeOffset.UtcNow,
                CommitCount: commits.Count,
                PullRequestCount: allPrs.Count,
                WorkItemCount: allWorkItems.Count,
                SentTo: new List<NotificationChannel>());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate local standup");
            return new StandupReportDto(
                Id: Guid.NewGuid().ToString(),
                Summary: $"## Error\n\nFailed to generate standup: {ex.Message}",
                PeriodStart: since,
                PeriodEnd: until,
                GeneratedAt: DateTimeOffset.UtcNow,
                CommitCount: 0,
                PullRequestCount: 0,
                WorkItemCount: 0,
                SentTo: new List<NotificationChannel>());
        }
    }

    public async Task<bool> ValidateConnectionAsync(
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string pat,
        CancellationToken cancellationToken = default)
    {
        var sourceRepo = new SourceRepository
        {
            Organization = organization,
            Project = project,
            Repository = repository,
            SourceType = sourceType,
            EncryptedPat = _encryptionService.Encrypt(pat)
        };

        try
        {
            var provider = sourceType == SourceType.AzureDevOps
                ? (Domain.Interfaces.ISourceProvider)new AzureDevOpsSourceProvider(_encryptionService)
                : new GitHubSourceProvider(_encryptionService);

            return await provider.ValidateConnectionAsync(sourceRepo, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection validation failed");
            return false;
        }
    }

    private static string BuildSummary(
        List<CommitInfo> commits,
        List<PullRequestInfo> prs,
        List<WorkItemInfo> workItems,
        DateTimeOffset since,
        DateTimeOffset until)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Standup Report");
        sb.AppendLine($"**Period:** {since:MMM dd} - {until:MMM dd, yyyy}");
        sb.AppendLine();

        if (commits.Count > 0)
        {
            sb.AppendLine("### Commits");
            foreach (var commit in commits.Take(10))
            {
                var message = commit.Message.Split('\n')[0];
                if (message.Length > 80) message = message[..77] + "...";
                sb.AppendLine($"- {message}");
            }
            if (commits.Count > 10)
                sb.AppendLine($"- ... and {commits.Count - 10} more");
            sb.AppendLine();
        }

        if (prs.Count > 0)
        {
            sb.AppendLine("### Pull Requests");
            foreach (var pr in prs)
            {
                sb.AppendLine($"- [{pr.Status}] {pr.Title}");
            }
            sb.AppendLine();
        }

        if (workItems.Count > 0)
        {
            sb.AppendLine("### Work Items");
            foreach (var item in workItems)
            {
                sb.AppendLine($"- [{item.Status}] {item.Title} ({item.Type})");
            }
            sb.AppendLine();
        }

        if (commits.Count == 0 && prs.Count == 0 && workItems.Count == 0)
        {
            sb.AppendLine("*No activity found for this period.*");
        }

        return sb.ToString();
    }
}
