using System.Text;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Git;
using Standup.Infrastructure.SourceProviders;
using SummaryOptions = Standup.Domain.Interfaces.SummaryOptions;

namespace Standup.Infrastructure.Services;

/// <summary>
/// Local standup generation using Infrastructure source providers directly.
/// Supports both remote API access and local git log reading.
/// </summary>
public sealed class LocalStandupService : ILocalStandupService
{
    private readonly IEncryptionService _encryptionService;
    private readonly IAISummaryService? _aiSummaryService;
    private readonly LocalGitService _localGitService;

    public LocalStandupService(
        IEncryptionService encryptionService,
        LocalGitService localGitService,
        IAISummaryService? aiSummaryService = null)
    {
        _encryptionService = encryptionService;
        _localGitService = localGitService;
        _aiSummaryService = aiSummaryService;

        // Log whether AI summary service was injected - critical for debugging missing summaries
        if (_aiSummaryService != null)
        {
            Log.Information(
                "LocalStandupService initialized WITH AI summary service ({ServiceType})",
                _aiSummaryService.GetType().Name);
        }
        else
        {
            Log.Warning("LocalStandupService initialized WITHOUT AI summary service - summaries will be empty!");
        }
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
        Log.Information("Generating remote standup for {Org}/{Project}/{Repo}", organization, project, repository);

        var sourceRepo = new SourceRepository
        {
            Organization = organization,
            Project = project,
            Repository = repository,
            SourceType = sourceType,
            AuthorIdentifier = authorIdentifier ?? string.Empty,
            EncryptedPat = _encryptionService.Encrypt(pat)
        };

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        try
        {
            var provider = sourceType == SourceType.AzureDevOps
                ? (ISourceProvider)new AzureDevOpsSourceProvider(_encryptionService)
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

            var summary = await GenerateSummaryAsync(commits, allPrs, allWorkItems, since, until, cancellationToken);

            Log.Information(
                "Standup generated - Commits: {Commits}, PRs: {PRs}, WorkItems: {WorkItems}",
                commits.Count,
                allPrs.Count,
                allWorkItems.Count);

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
            Log.Error(ex, "Failed to generate remote standup");
            return CreateErrorReport(ex, since, until);
        }
    }

    public async Task<StandupReportDto> GenerateStandupFromLocalAsync(
        string localPath,
        SourceType sourceType,
        string organization,
        string project,
        string repository,
        string? pat = null,
        string? authorIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        Log.Information(
            "Generating local standup for {LocalPath} ({Org}/{Project}/{Repo})",
            localPath,
            organization,
            project,
            repository);

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        try
        {
            // Get commits from local git log - no PAT required!
            Log.Information("Reading local git log for commits from {Since} to {Until}", since, until);
            var localCommits = await _localGitService.GetCommitsAsync(
                localPath,
                authorIdentifier,
                since,
                until,
                100);

            // Convert local commits to CommitInfo format
            var commits = localCommits.Select(c => new CommitInfo(
                Sha: c.Sha,
                Message: c.Subject,
                Repository: repository,
                SourceType: sourceType,
                CommittedAt: c.CommitDate)).ToList();

            Log.Information("Found {Count} local commits", commits.Count);

            // PRs and work items require API access (optional)
            var allPrs = new List<PullRequestInfo>();
            var allWorkItems = new List<WorkItemInfo>();

            if (!string.IsNullOrEmpty(pat) && !string.IsNullOrEmpty(organization))
            {
                Log.Information("PAT provided - fetching PRs and work items from remote");

                var sourceRepo = new SourceRepository
                {
                    Organization = organization,
                    Project = project,
                    Repository = repository,
                    SourceType = sourceType,
                    AuthorIdentifier = authorIdentifier ?? string.Empty,
                    EncryptedPat = _encryptionService.Encrypt(pat)
                };

                var provider = sourceType == SourceType.AzureDevOps
                    ? (ISourceProvider)new AzureDevOpsSourceProvider(_encryptionService)
                    : new GitHubSourceProvider(_encryptionService);

                try
                {
                    var openPrs = (await provider.GetOpenPullRequestsAsync(sourceRepo, cancellationToken)).ToList();
                    var mergedPrs = (await provider.GetMergedPullRequestsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                    allPrs = openPrs.Concat(mergedPrs).ToList();
                    Log.Information("Found {Count} PRs from remote", allPrs.Count);

                    var inProgressItems = (await provider.GetInProgressWorkItemsAsync(sourceRepo, cancellationToken)).ToList();
                    var completedItems = (await provider.GetCompletedWorkItemsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                    allWorkItems = inProgressItems.Concat(completedItems).DistinctBy(w => w.Id).ToList();
                    Log.Information("Found {Count} work items from remote", allWorkItems.Count);
                }
                catch (Exception apiEx)
                {
                    Log.Warning(apiEx, "Failed to fetch PRs/work items from remote - continuing with local commits only");
                }
            }
            else
            {
                Log.Information("No PAT provided - using local commits only (PRs and work items unavailable)");
            }

            var summary = await GenerateSummaryAsync(commits, allPrs, allWorkItems, since, until, cancellationToken);

            Log.Information(
                "Local standup generated - Commits: {Commits}, PRs: {PRs}, WorkItems: {WorkItems}",
                commits.Count,
                allPrs.Count,
                allWorkItems.Count);

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
            return CreateErrorReport(ex, since, until);
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
                ? (ISourceProvider)new AzureDevOpsSourceProvider(_encryptionService)
                : new GitHubSourceProvider(_encryptionService);

            return await provider.ValidateConnectionAsync(sourceRepo, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection validation failed");
            return false;
        }
    }

    public async Task<GroupedStandupReportDto> GenerateGroupedStandupAsync(
        RepositoryGroup group,
        Func<GroupedRepository, Task<string?>> getPatForRepo,
        DateTimeOffset since,
        DateTimeOffset until,
        SummaryType summaryType = SummaryType.Technical,
        CancellationToken cancellationToken = default)
    {
        Log.Information(
            "Generating grouped standup for {GroupName} with {RepoCount} repositories, SummaryType: {SummaryType}",
            group.Name,
            group.Repositories.Count,
            summaryType);

        var activeRepos = group.Repositories.Where(r => r.IsActive).ToList();

        // Fetch data from all repos in parallel
        var repoDataTasks = activeRepos.Select(async repo =>
        {
            try
            {
                return await FetchRepositoryDataAsync(repo, getPatForRepo, since, until, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch data for repository {Repo}", repo.Repository);
                return new RepositoryStandupData(
                    repo.ClientCode,
                    [],
                    [],
                    []);
            }
        });

        var repoData = await Task.WhenAll(repoDataTasks);

        // Group results by client code
        var sections = repoData
            .GroupBy(r => r.ClientCode)
            .Select(g => new ClientCodeSection(
                ClientCode: g.Key,
                Summary: null,
                Commits: g.SelectMany(r => r.Commits).OrderByDescending(c => c.CommittedAt).ToList(),
                PullRequests: g.SelectMany(r => r.PullRequests).DistinctBy(p => p.Id).ToList(),
                WorkItems: g.SelectMany(r => r.WorkItems).DistinctBy(w => w.Id).ToList()))
            .OrderBy(s => s.ClientCode)
            .ToList();

        // Generate AI summaries for each section if available
        if (_aiSummaryService != null)
        {
            sections = await GenerateSectionsWithSummariesAsync(sections, summaryType, cancellationToken);
        }

        var totalCommits = sections.Sum(s => s.CommitCount);
        var totalPrs = sections.Sum(s => s.PullRequestCount);
        var totalWorkItems = sections.Sum(s => s.WorkItemCount);

        Log.Information(
            "Grouped standup generated - {SectionCount} client codes, {Commits} commits, {PRs} PRs, {WorkItems} work items",
            sections.Count,
            totalCommits,
            totalPrs,
            totalWorkItems);

        return new GroupedStandupReportDto(
            Id: Guid.NewGuid().ToString(),
            GroupName: group.Name,
            Sections: sections,
            PeriodStart: since,
            PeriodEnd: until,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: totalCommits,
            TotalPullRequests: totalPrs,
            TotalWorkItems: totalWorkItems,
            CurrentSummaryType: summaryType);
    }

    private static StandupReportDto CreateErrorReport(
        Exception ex,
        DateTimeOffset since,
        DateTimeOffset until)
    {
        return new StandupReportDto(
            Id: Guid.NewGuid().ToString(),
            Summary: $"Error generating standup: {ex.Message}",
            PeriodStart: since,
            PeriodEnd: until,
            GeneratedAt: DateTimeOffset.UtcNow,
            CommitCount: 0,
            PullRequestCount: 0,
            WorkItemCount: 0,
            SentTo: new List<NotificationChannel>());
    }

    private static string BuildSimpleSummary(
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
                if (message.Length > 80)
                {
                    message = message[..77] + "...";
                }

                sb.AppendLine($"- {message}");
            }

            if (commits.Count > 10)
            {
                sb.AppendLine($"- ... and {commits.Count - 10} more");
            }

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

    private async Task<string> GenerateSummaryAsync(
        List<CommitInfo> commits,
        List<PullRequestInfo> prs,
        List<WorkItemInfo> workItems,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken)
    {
        // Try AI summary first, fall back to simple text summary
        if (_aiSummaryService != null && (commits.Count > 0 || prs.Count > 0 || workItems.Count > 0))
        {
            Log.Information("Generating AI summary");
            try
            {
                var standupData = new StandupData
                {
                    Commits = commits,
                    PullRequests = prs,
                    WorkItems = workItems
                };
                var summary = await _aiSummaryService.GenerateSummaryAsync(standupData, null, cancellationToken);
                Log.Information("AI summary generated successfully");
                return summary;
            }
            catch (Exception aiEx)
            {
                Log.Warning(aiEx, "AI summary failed, falling back to simple summary");
            }
        }

        return BuildSimpleSummary(commits, prs, workItems, since, until);
    }

    private async Task<RepositoryStandupData> FetchRepositoryDataAsync(
        GroupedRepository repo,
        Func<GroupedRepository, Task<string?>> getPatForRepo,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken)
    {
        var commits = new List<CommitInfo>();
        var prs = new List<PullRequestInfo>();
        var workItems = new List<WorkItemInfo>();

        // Try local git first if we have a local path
        if (!string.IsNullOrEmpty(repo.LocalPath))
        {
            Log.Information("Fetching commits from local path: {LocalPath}", repo.LocalPath);
            var localCommits = await _localGitService.GetCommitsAsync(
                repo.LocalPath,
                repo.AuthorIdentifier,
                since,
                until,
                100);

            commits = localCommits.Select(c => new CommitInfo(
                Sha: c.Sha,
                Message: c.Subject,
                Repository: repo.Repository,
                SourceType: repo.SourceType,
                CommittedAt: c.CommitDate)).ToList();
        }

        // Fetch PRs and work items from remote if PAT is available
        var pat = await getPatForRepo(repo);
        if (!string.IsNullOrEmpty(pat) && !string.IsNullOrEmpty(repo.Organization))
        {
            var sourceRepo = new SourceRepository
            {
                Organization = repo.Organization,
                Project = repo.Project,
                Repository = repo.Repository,
                SourceType = repo.SourceType,
                AuthorIdentifier = repo.AuthorIdentifier ?? string.Empty,
                EncryptedPat = _encryptionService.Encrypt(pat)
            };

            var provider = repo.SourceType == SourceType.AzureDevOps
                ? (ISourceProvider)new AzureDevOpsSourceProvider(_encryptionService)
                : new GitHubSourceProvider(_encryptionService);

            try
            {
                // If no local commits, get from remote
                if (commits.Count == 0)
                {
                    commits = (await provider.GetCommitsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                }

                var openPrs = (await provider.GetOpenPullRequestsAsync(sourceRepo, cancellationToken)).ToList();
                var mergedPrs = (await provider.GetMergedPullRequestsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                prs = openPrs.Concat(mergedPrs).ToList();

                var inProgressItems = (await provider.GetInProgressWorkItemsAsync(sourceRepo, cancellationToken)).ToList();
                var completedItems = (await provider.GetCompletedWorkItemsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                workItems = inProgressItems.Concat(completedItems).DistinctBy(w => w.Id).ToList();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch remote data for {Repo}", repo.Repository);
            }
        }

        return new RepositoryStandupData(repo.ClientCode, commits, prs, workItems);
    }

    private async Task<List<ClientCodeSection>> GenerateSectionsWithSummariesAsync(
        List<ClientCodeSection> sections,
        SummaryType summaryType,
        CancellationToken cancellationToken)
    {
        var result = new List<ClientCodeSection>();

        foreach (var section in sections)
        {
            if (section.Commits.Count == 0 && section.PullRequests.Count == 0 && section.WorkItems.Count == 0)
            {
                result.Add(section);
                continue;
            }

            try
            {
                var standupData = new StandupData
                {
                    Commits = section.Commits,
                    PullRequests = section.PullRequests,
                    WorkItems = section.WorkItems
                };

                var options = new SummaryOptions(Type: summaryType);
                var summary = await _aiSummaryService!.GenerateSummaryAsync(standupData, options, cancellationToken);

                var allSummaries = new Dictionary<SummaryType, string> { { summaryType, summary } };
                result.Add(section with { Summary = summary, AllSummaries = allSummaries });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to generate AI summary for client code {ClientCode}", section.ClientCode);
                result.Add(section);
            }
        }

        return result;
    }

    private record RepositoryStandupData(
        string ClientCode,
        List<CommitInfo> Commits,
        List<PullRequestInfo> PullRequests,
        List<WorkItemInfo> WorkItems);
}
