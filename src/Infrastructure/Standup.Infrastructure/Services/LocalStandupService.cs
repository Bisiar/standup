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
public sealed partial class LocalStandupService : ILocalStandupService
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

        var since = DateTimeOffset.UtcNow.AddDays(-1);
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

        var since = DateTimeOffset.UtcNow.AddDays(-1);
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

            // Get current branch for context
            var currentBranch = await _localGitService.GetCurrentBranchAsync(localPath);

            // Convert local commits to CommitInfo format
            var commits = localCommits.Select(c => new CommitInfo(
                Sha: c.Sha,
                Message: c.Subject,
                Repository: repository,
                SourceType: sourceType,
                CommittedAt: c.CommitDate,
                Branch: currentBranch,
                Author: c.AuthorName,
                AuthorEmail: c.AuthorEmail)).ToList();

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
        IProgress<(double Progress, string Message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Log.Information(
            "Generating grouped standup for {GroupName} with {RepoCount} repositories, SummaryType: {SummaryType}",
            group.Name,
            group.Repositories.Count,
            summaryType);

        var activeRepos = group.Repositories.Where(r => r.IsActive && r.IncludeInGeneration).ToList();

        // Progress allocation:
        // - Fetching repos: 0% - 50%
        // - Processing: 50% - 55%
        // - Generating summaries: 55% - 100%
        const double fetchWeight = 0.50;
        const double processWeight = 0.05;
        const double summaryWeight = 0.45;

        // Fetch data from all repos sequentially to report progress
        var repoData = new List<RepositoryStandupData>();
        for (int i = 0; i < activeRepos.Count; i++)
        {
            var repo = activeRepos[i];
            var fetchProgress = (double)i / activeRepos.Count * fetchWeight;
            progress?.Report((fetchProgress, $"Fetching data from {repo.Repository} ({i + 1}/{activeRepos.Count})..."));

            try
            {
                var data = await FetchRepositoryDataAsync(repo, getPatForRepo, since, until, cancellationToken);
                repoData.Add(data);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch data for repository {Repo}", repo.Repository);
                var errorStatus = new DataSourceStatus(
                    FetchStatus.Error,
                    FetchStatus.Error,
                    FetchStatus.Error,
                    ex.Message,
                    ex.Message,
                    ex.Message);
                var groupKey = string.IsNullOrWhiteSpace(repo.ClientCode) ? repo.Repository : repo.ClientCode;
                repoData.Add(new RepositoryStandupData(groupKey, [], [], [], errorStatus));
            }
        }

        progress?.Report((fetchWeight, "Processing commits and work items..."));

        // Group results by client code, aggregating source status
        // Skip sections with no commits for the date range
        var sections = repoData
            .GroupBy(r => r.ClientCode)
            .Select(g => new ClientCodeSection(
                ClientCode: g.Key,
                Summary: null,
                Commits: g.SelectMany(r => r.Commits).OrderByDescending(c => c.CommittedAt).ToList(),
                PullRequests: g.SelectMany(r => r.PullRequests).DistinctBy(p => p.Id).ToList(),
                WorkItems: g.SelectMany(r => r.WorkItems).DistinctBy(w => w.Id).ToList(),
                AllSummaries: null,
                SourceStatus: DataSourceStatusAggregator.Aggregate(g.Select(r => r.SourceStatus).ToList())))
            .Where(s => s.Commits.Count > 0)
            .OrderBy(s => s.ClientCode)
            .ToList();

        progress?.Report((fetchWeight + processWeight, "Starting AI summary generation..."));

        // Generate AI summaries for each section if available
        if (_aiSummaryService != null)
        {
            sections = await GenerateSectionsWithSummariesAsync(
                sections,
                summaryType,
                progress,
                fetchWeight + processWeight,
                summaryWeight,
                cancellationToken);
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

    public async Task<GroupedStandupReportDto> GenerateAllSummaryTypesAsync(
        GroupedStandupReportDto existingReport,
        IEnumerable<SummaryType>? summaryTypes = null,
        IProgress<(double Progress, string Message)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var typesToGenerate = summaryTypes?.ToList() ??
            [SummaryType.Technical, SummaryType.Executive, SummaryType.CodeReview];

        Log.Information(
            "Generating all summary types ({Types}) for report {ReportId}",
            string.Join(", ", typesToGenerate),
            existingReport.Id);

        if (_aiSummaryService == null)
        {
            Log.Warning("No AI summary service available - returning original report");
            return existingReport;
        }

        var updatedSections = new List<ClientCodeSection>();
        var sectionsWithData = existingReport.Sections
            .Where(s => s.Commits.Count > 0 || s.PullRequests.Count > 0 || s.WorkItems.Count > 0)
            .ToList();

        var totalOperations = sectionsWithData.Count * typesToGenerate.Count;
        var currentOperation = 0;

        foreach (var section in existingReport.Sections)
        {
            if (section.Commits.Count == 0 && section.PullRequests.Count == 0 && section.WorkItems.Count == 0)
            {
                updatedSections.Add(section);
                continue;
            }

            // Start with existing summaries
            var allSummaries = section.AllSummaries != null
                ? new Dictionary<SummaryType, string>(section.AllSummaries)
                : new Dictionary<SummaryType, string>();

            var standupData = new StandupData
            {
                Commits = section.Commits,
                PullRequests = section.PullRequests,
                WorkItems = section.WorkItems
            };

            foreach (var summaryType in typesToGenerate)
            {
                currentOperation++;
                var progressValue = (double)currentOperation / totalOperations;
                progress?.Report((progressValue, $"Generating {summaryType} summary for {section.ClientCode}..."));

                // Skip if we already have this summary type
                if (allSummaries.ContainsKey(summaryType))
                {
                    Log.Debug("Skipping {SummaryType} for {ClientCode} - already exists", summaryType, section.ClientCode);
                    continue;
                }

                try
                {
                    var options = new SummaryOptions(Type: summaryType);
                    var summary = await _aiSummaryService.GenerateSummaryAsync(standupData, options, cancellationToken);
                    allSummaries[summaryType] = summary;
                    Log.Debug("Generated {SummaryType} summary for {ClientCode}", summaryType, section.ClientCode);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to generate {SummaryType} summary for {ClientCode}", summaryType, section.ClientCode);
                }
            }

            // Use the first available summary as the default
            var defaultSummary = allSummaries.TryGetValue(SummaryType.Technical, out var tech) ? tech :
                allSummaries.TryGetValue(SummaryType.Executive, out var exec) ? exec :
                allSummaries.Values.FirstOrDefault() ?? section.Summary;

            updatedSections.Add(section with { Summary = defaultSummary, AllSummaries = allSummaries });
        }

        progress?.Report((1.0, "All summaries generated"));

        Log.Information(
            "Generated {TypeCount} summary types for {SectionCount} sections",
            typesToGenerate.Count,
            sectionsWithData.Count);

        return existingReport with { Sections = updatedSections };
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

        return SimpleSummaryBuilder.Build(commits, prs, workItems, since, until);
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

        // Track fetch status for each data source
        var commitsStatus = FetchStatus.Success;
        var prsStatus = FetchStatus.NoPat;
        var workItemsStatus = FetchStatus.NoPat;
        string? commitsError = null;
        string? prsError = null;
        string? workItemsError = null;

        // Try local git first if we have a local path
        if (!string.IsNullOrEmpty(repo.LocalPath))
        {
            Log.Information("Fetching commits from local path: {LocalPath}", repo.LocalPath);
            try
            {
                var localCommits = await _localGitService.GetCommitsAsync(
                    repo.LocalPath,
                    repo.AuthorIdentifier,
                    since,
                    until,
                    100);

                // Get current branch for context
                var currentBranch = await _localGitService.GetCurrentBranchAsync(repo.LocalPath);

                commits = localCommits.Select(c => new CommitInfo(
                    Sha: c.Sha,
                    Message: c.Subject,
                    Repository: repo.Repository,
                    SourceType: repo.SourceType,
                    CommittedAt: c.CommitDate,
                    Additions: c.Additions,
                    Deletions: c.Deletions,
                    Branch: currentBranch,
                    Author: c.AuthorName,
                    AuthorEmail: c.AuthorEmail)).ToList();
                commitsStatus = FetchStatus.Success;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch local commits for {Repo}", repo.Repository);
                commitsStatus = FetchStatus.Error;
                commitsError = ex.Message;
            }
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

            // Fetch commits from remote if no local commits
            if (commits.Count == 0)
            {
                try
                {
                    commits = (await provider.GetCommitsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                    commitsStatus = FetchStatus.Success;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to fetch remote commits for {Repo}", repo.Repository);
                    commitsStatus = FetchStatus.Error;
                    commitsError = ex.Message;
                }
            }

            // Fetch PRs
            try
            {
                var openPrs = (await provider.GetOpenPullRequestsAsync(sourceRepo, cancellationToken)).ToList();
                var mergedPrs = (await provider.GetMergedPullRequestsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                prs = openPrs.Concat(mergedPrs).ToList();
                prsStatus = FetchStatus.Success;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch PRs for {Repo}", repo.Repository);
                prsStatus = FetchStatus.Error;
                prsError = ex.Message;
            }

            // Fetch work items
            try
            {
                var inProgressItems = (await provider.GetInProgressWorkItemsAsync(sourceRepo, cancellationToken)).ToList();
                var completedItems = (await provider.GetCompletedWorkItemsAsync(sourceRepo, since, until, cancellationToken)).ToList();
                workItems = inProgressItems.Concat(completedItems).DistinctBy(w => w.Id).ToList();
                workItemsStatus = FetchStatus.Success;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch work items for {Repo}", repo.Repository);
                workItemsStatus = FetchStatus.Error;
                workItemsError = ex.Message;
            }
        }

        var sourceStatus = new DataSourceStatus(
            commitsStatus,
            prsStatus,
            workItemsStatus,
            commitsError,
            prsError,
            workItemsError);

        var groupKey = string.IsNullOrWhiteSpace(repo.ClientCode) ? repo.Repository : repo.ClientCode;
        return new RepositoryStandupData(groupKey, commits, prs, workItems, sourceStatus);
    }

    private async Task<List<ClientCodeSection>> GenerateSectionsWithSummariesAsync(
        List<ClientCodeSection> sections,
        SummaryType summaryType,
        IProgress<(double Progress, string Message)>? progress,
        double progressBase,
        double progressWeight,
        CancellationToken cancellationToken)
    {
        var result = new List<ClientCodeSection>();
        var sectionsWithData = sections.Where(s => s.Commits.Count > 0 || s.PullRequests.Count > 0 || s.WorkItems.Count > 0).ToList();
        var currentIndex = 0;

        foreach (var section in sections)
        {
            if (section.Commits.Count == 0 && section.PullRequests.Count == 0 && section.WorkItems.Count == 0)
            {
                result.Add(section);
                continue;
            }

            currentIndex++;
            var sectionProgress = progressBase + ((double)currentIndex / sectionsWithData.Count * progressWeight);
            progress?.Report((sectionProgress, $"Generating {summaryType} summary for {section.ClientCode} ({currentIndex}/{sectionsWithData.Count})..."));

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
        List<WorkItemInfo> WorkItems,
        DataSourceStatus SourceStatus);
}
