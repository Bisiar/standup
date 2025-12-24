using Serilog;
using Standup.Domain.Entities;

namespace Standup.Infrastructure.Services;

/// <summary>
/// Dashboard-specific methods for LocalStandupService.
/// Provides lightweight commit fetching without full report generation.
/// </summary>
public sealed partial class LocalStandupService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<CommitInfo>> GetLocalCommitsAsync(
        IEnumerable<GroupedRepository> repositories,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var allCommits = new List<CommitInfo>();

        foreach (var repo in repositories.Where(r => r.IsActive && r.IncludeInGeneration && !string.IsNullOrEmpty(r.LocalPath)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Log.Debug("Dashboard: Fetching commits from local path: {LocalPath}", repo.LocalPath);

                var localCommits = await _localGitService.GetCommitsAsync(
                    repo.LocalPath!,
                    repo.AuthorIdentifier,
                    since,
                    until,
                    100);

                var currentBranch = await _localGitService.GetCurrentBranchAsync(repo.LocalPath!);

                var commitInfos = localCommits.Select(c => new CommitInfo(
                    Sha: c.Sha,
                    Message: c.Subject,
                    Repository: repo.Repository,
                    SourceType: repo.SourceType,
                    CommittedAt: c.CommitDate,
                    Additions: c.Additions,
                    Deletions: c.Deletions,
                    Branch: currentBranch,
                    Author: c.AuthorName,
                    AuthorEmail: c.AuthorEmail));

                allCommits.AddRange(commitInfos);
                Log.Debug("Dashboard: Found {Count} commits in {Repo}", localCommits.Count, repo.Repository);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dashboard: Failed to fetch local commits for {Repo}", repo.Repository);
            }
        }

        Log.Information("Dashboard: Total {Count} commits fetched from {RepoCount} repositories", allCommits.Count, repositories.Count(r => r.IsActive && r.IncludeInGeneration && !string.IsNullOrEmpty(r.LocalPath)));

        return allCommits.OrderByDescending(c => c.CommittedAt).ToList();
    }
}
