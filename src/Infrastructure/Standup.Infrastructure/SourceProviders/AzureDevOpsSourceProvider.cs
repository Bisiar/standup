using Azure.Core;
using Azure.Identity;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Infrastructure.SourceProviders;

public class AzureDevOpsSourceProvider : ISourceProvider
{
    private static readonly string[] WorkItemFields =
    [
        "System.Id",
        "System.Title",
        "System.WorkItemType",
        "System.State",
        "System.AssignedTo",
        "System.Tags",
        "System.Parent",
    ];

    private readonly IEncryptionService _encryptionService;

    public AzureDevOpsSourceProvider(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public async Task<IEnumerable<CommitInfo>> GetCommitsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(repository);
        var gitClient = await connection.GetClientAsync<GitHttpClient>();

        var repos = await gitClient.GetRepositoriesAsync(repository.Project, cancellationToken: cancellationToken);
        var repo = repos.FirstOrDefault(r => r.Name.Equals(repository.Repository, StringComparison.OrdinalIgnoreCase));

        if (repo == null)
        {
            return Enumerable.Empty<CommitInfo>();
        }

        // Only filter by author if specified, no date range - get most recent commits
        var searchCriteria = new GitQueryCommitsCriteria
        {
            Author = string.IsNullOrEmpty(repository.AuthorIdentifier) ? null : repository.AuthorIdentifier
        };

        // Get the 20 most recent commits (no date filtering needed for standup)
        var commits = await gitClient.GetCommitsAsync(
            repository.Project,
            repo.Id,
            searchCriteria,
            skip: null,
            top: 20,
            cancellationToken: cancellationToken);

        var results = new List<CommitInfo>();

        foreach (var commit in commits)
        {
            var changes = await gitClient.GetChangesAsync(
                repository.Project,
                commit.CommitId,
                repo.Id,
                cancellationToken: cancellationToken);

            results.Add(new CommitInfo(
                Sha: commit.CommitId,
                Message: commit.Comment,
                Repository: repository.FullPath,
                SourceType: SourceType.AzureDevOps,
                CommittedAt: commit.Author.Date,
                FilesChanged: changes.Changes?.Select(c => c.Item.Path).ToList(),
                Additions: changes.ChangeCounts?.TryGetValue(VersionControlChangeType.Add, out var adds) == true ? adds : 0,
                Deletions: changes.ChangeCounts?.TryGetValue(VersionControlChangeType.Delete, out var dels) == true ? dels : 0));
        }

        return results;
    }

    public async Task<IEnumerable<PullRequestInfo>> GetOpenPullRequestsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(repository);
        var gitClient = await connection.GetClientAsync<GitHttpClient>();

        var repos = await gitClient.GetRepositoriesAsync(repository.Project, cancellationToken: cancellationToken);
        var repo = repos.FirstOrDefault(r => r.Name.Equals(repository.Repository, StringComparison.OrdinalIgnoreCase));

        if (repo == null)
        {
            return Enumerable.Empty<PullRequestInfo>();
        }

        var searchCriteria = new GitPullRequestSearchCriteria
        {
            Status = PullRequestStatus.Active,
            CreatorId = Guid.TryParse(repository.AuthorIdentifier, out var creatorId) ? creatorId : null
        };

        var prs = await gitClient.GetPullRequestsAsync(
            repository.Project,
            repo.Id,
            searchCriteria,
            cancellationToken: cancellationToken);

        return prs
            .Where(pr => pr.CreatedBy.UniqueName.Contains(repository.AuthorIdentifier, StringComparison.OrdinalIgnoreCase)
                         || pr.CreatedBy.DisplayName.Contains(repository.AuthorIdentifier, StringComparison.OrdinalIgnoreCase))
            .Select(pr => new PullRequestInfo(
                Id: pr.PullRequestId.ToString(),
                Title: pr.Title,
                Repository: repository.FullPath,
                SourceType: SourceType.AzureDevOps,
                Status: pr.Status.ToString(),
                Url: $"https://dev.azure.com/{repository.Organization}/{repository.Project}/_git/{repository.Repository}/pullrequest/{pr.PullRequestId}",
                CreatedAt: pr.CreationDate,
                Description: pr.Description,
                IsDraft: pr.IsDraft ?? false,
                ReviewerCount: pr.Reviewers?.Length ?? 0));
    }

    public async Task<IEnumerable<PullRequestInfo>> GetMergedPullRequestsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(repository);
        var gitClient = await connection.GetClientAsync<GitHttpClient>();

        var repos = await gitClient.GetRepositoriesAsync(repository.Project, cancellationToken: cancellationToken);
        var repo = repos.FirstOrDefault(r => r.Name.Equals(repository.Repository, StringComparison.OrdinalIgnoreCase));

        if (repo == null)
        {
            return Enumerable.Empty<PullRequestInfo>();
        }

        var searchCriteria = new GitPullRequestSearchCriteria
        {
            Status = PullRequestStatus.Completed
        };

        var prs = await gitClient.GetPullRequestsAsync(
            repository.Project,
            repo.Id,
            searchCriteria,
            cancellationToken: cancellationToken);

        return prs
            .Where(pr => (pr.CreatedBy.UniqueName.Contains(repository.AuthorIdentifier, StringComparison.OrdinalIgnoreCase)
                          || pr.CreatedBy.DisplayName.Contains(repository.AuthorIdentifier, StringComparison.OrdinalIgnoreCase))
                         && pr.ClosedDate >= since
                         && pr.ClosedDate <= until)
            .Select(pr => new PullRequestInfo(
                Id: pr.PullRequestId.ToString(),
                Title: pr.Title,
                Repository: repository.FullPath,
                SourceType: SourceType.AzureDevOps,
                Status: "Completed",
                Url: $"https://dev.azure.com/{repository.Organization}/{repository.Project}/_git/{repository.Repository}/pullrequest/{pr.PullRequestId}",
                CreatedAt: pr.CreationDate,
                Description: pr.Description,
                IsDraft: false,
                ReviewerCount: pr.Reviewers?.Length ?? 0));
    }

    public async Task<IEnumerable<WorkItemInfo>> GetInProgressWorkItemsAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(repository);
        var witClient = await connection.GetClientAsync<WorkItemTrackingHttpClient>();

        var query = $@"
            SELECT [System.Id], [System.Title], [System.WorkItemType], [System.State], [System.AssignedTo], [System.Tags], [System.Parent]
            FROM WorkItems
            WHERE [System.TeamProject] = '{repository.Project}'
              AND [System.AssignedTo] = '{repository.AuthorIdentifier}'
              AND [System.State] IN ('Active', 'In Progress', 'Doing')
            ORDER BY [System.ChangedDate] DESC";

        return await ExecuteWorkItemQueryAsync(witClient, repository, query, cancellationToken);
    }

    public async Task<IEnumerable<WorkItemInfo>> GetCompletedWorkItemsAsync(
        SourceRepository repository,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var connection = await CreateConnectionAsync(repository);
        var witClient = await connection.GetClientAsync<WorkItemTrackingHttpClient>();

        var query = $@"
            SELECT [System.Id], [System.Title], [System.WorkItemType], [System.State], [System.AssignedTo], [System.Tags], [System.Parent]
            FROM WorkItems
            WHERE [System.TeamProject] = '{repository.Project}'
              AND [System.AssignedTo] = '{repository.AuthorIdentifier}'
              AND [System.State] IN ('Closed', 'Done', 'Resolved')
              AND [System.ChangedDate] >= '{since:yyyy-MM-dd}'
              AND [System.ChangedDate] <= '{until:yyyy-MM-dd}'
            ORDER BY [System.ChangedDate] DESC";

        return await ExecuteWorkItemQueryAsync(witClient, repository, query, cancellationToken);
    }

    public async Task<bool> ValidateConnectionAsync(
        SourceRepository repository,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await CreateConnectionAsync(repository);
            var gitClient = await connection.GetClientAsync<GitHttpClient>();
            await gitClient.GetRepositoriesAsync(repository.Project, cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static WorkItemStatus MapWorkItemStatus(string? state)
    {
        return state?.ToLowerInvariant() switch
        {
            "new" => WorkItemStatus.New,
            "active" or "in progress" or "doing" => WorkItemStatus.InProgress,
            "resolved" => WorkItemStatus.Resolved,
            "closed" or "done" => WorkItemStatus.Closed,
            _ => WorkItemStatus.Active
        };
    }

    private async Task<VssConnection> CreateConnectionAsync(SourceRepository repository)
    {
        var orgUrl = new Uri($"https://dev.azure.com/{repository.Organization}");

        // Priority 1: Use PAT if provided
        if (!string.IsNullOrEmpty(repository.EncryptedPat))
        {
            var pat = await _encryptionService.DecryptAsync(repository.EncryptedPat);
            var credentials = new VssBasicCredential(string.Empty, pat);
            Log.Debug("Azure DevOps: Using PAT authentication for {Org}", repository.Organization);
            return new VssConnection(orgUrl, credentials);
        }

        // Priority 2: Use DefaultAzureCredential (Azure CLI, Managed Identity, etc.)
        try
        {
            Log.Debug("Azure DevOps: Attempting DefaultAzureCredential for {Org}", repository.Organization);
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeManagedIdentityCredential = false, // Allow in Azure
                ExcludeWorkloadIdentityCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeAzureCliCredential = false, // Primary local dev option
                ExcludeAzureDeveloperCliCredential = false,
                ExcludeInteractiveBrowserCredential = true,
            });

            // Azure DevOps scope for token acquisition
            const string azureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";
            var tokenRequestContext = new TokenRequestContext(new[] { azureDevOpsScope });
            var token = await credential.GetTokenAsync(tokenRequestContext);

            var vssCredentials = new VssOAuthAccessTokenCredential(token.Token);
            Log.Information("Azure DevOps: Connected using DefaultAzureCredential for {Org}", repository.Organization);
            return new VssConnection(orgUrl, vssCredentials);
        }
        catch (CredentialUnavailableException ex)
        {
            Log.Warning(
                "Azure DevOps: DefaultAzureCredential unavailable for {Org}: {Message}",
                repository.Organization,
                ex.Message);
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Azure DevOps: Failed to use DefaultAzureCredential for {Org}",
                repository.Organization);
        }

        // Fallback: Anonymous (will likely fail for most operations)
        Log.Warning("Azure DevOps: Using anonymous credentials for {Org} - operations may fail", repository.Organization);
        return new VssConnection(orgUrl, new VssCredentials());
    }

    private static async Task<IEnumerable<WorkItemInfo>> ExecuteWorkItemQueryAsync(
        WorkItemTrackingHttpClient client,
        SourceRepository repository,
        string query,
        CancellationToken cancellationToken)
    {
        var wiql = new Wiql { Query = query };
        var result = await client.QueryByWiqlAsync(wiql, cancellationToken: cancellationToken);

        if (result.WorkItems == null || !result.WorkItems.Any())
        {
            return Enumerable.Empty<WorkItemInfo>();
        }

        var ids = result.WorkItems.Select(wi => wi.Id).ToArray();
        var workItems = await client.GetWorkItemsAsync(
            ids,
            WorkItemFields,
            cancellationToken: cancellationToken);

        return workItems.Select(wi => new WorkItemInfo(
            Id: wi.Id?.ToString() ?? string.Empty,
            Title: wi.Fields.GetValueOrDefault("System.Title")?.ToString() ?? string.Empty,
            Type: wi.Fields.GetValueOrDefault("System.WorkItemType")?.ToString() ?? string.Empty,
            Status: MapWorkItemStatus(wi.Fields.GetValueOrDefault("System.State")?.ToString()),
            SourceType: SourceType.AzureDevOps,
            Url: $"https://dev.azure.com/{repository.Organization}/{repository.Project}/_workitems/edit/{wi.Id}",
            AssignedTo: wi.Fields.GetValueOrDefault("System.AssignedTo")?.ToString(),
            ParentId: wi.Fields.GetValueOrDefault("System.Parent")?.ToString(),
            Tags: wi.Fields.GetValueOrDefault("System.Tags")?.ToString()?.Split(';').ToList()));
    }
}
