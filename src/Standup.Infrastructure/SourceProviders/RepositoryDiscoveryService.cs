using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Octokit;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Domain.Enums;

namespace Standup.Infrastructure.SourceProviders;

/// <summary>
/// Discovers repositories from GitHub and Azure DevOps using PATs.
/// </summary>
public sealed class RepositoryDiscoveryService : IRepositoryDiscoveryService
{
    private readonly ILogger _logger;

    public RepositoryDiscoveryService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<DiscoveredRepository>> GetRepositoriesAsync(
        SourceType sourceType,
        string organization,
        string? project,
        string pat,
        CancellationToken cancellationToken = default)
    {
        return sourceType switch
        {
            SourceType.GitHub => await GetGitHubRepositoriesAsync(organization, pat, cancellationToken),
            SourceType.AzureDevOps => await GetAzureDevOpsRepositoriesAsync(organization, project, pat, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType))
        };
    }

    public async Task<IEnumerable<string>> GetProjectsAsync(
        SourceType sourceType,
        string organization,
        string pat,
        CancellationToken cancellationToken = default)
    {
        if (sourceType != SourceType.AzureDevOps)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var orgUrl = new Uri($"https://dev.azure.com/{organization}");
            var credentials = new VssBasicCredential(string.Empty, pat);
            using var connection = new VssConnection(orgUrl, credentials);

            var projectClient = connection.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();

            return projects.Select(p => p.Name).OrderBy(n => n);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get Azure DevOps projects for {Org}", organization);
            throw;
        }
    }

    public async Task<bool> ValidatePatAsync(
        SourceType sourceType,
        string organization,
        string pat,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return sourceType switch
            {
                SourceType.GitHub => await ValidateGitHubPatAsync(organization, pat),
                SourceType.AzureDevOps => await ValidateAzureDevOpsPatAsync(organization, pat, cancellationToken),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "PAT validation failed for {SourceType} {Org}", sourceType, organization);
            return false;
        }
    }

    private async Task<IEnumerable<DiscoveredRepository>> GetGitHubRepositoriesAsync(
        string organization,
        string pat,
        CancellationToken cancellationToken)
    {
        var client = CreateGitHubClient(pat);

        try
        {
            // Try organization repos first
            var repos = await client.Repository.GetAllForOrg(organization);

            return repos.Select(r => new DiscoveredRepository(
                Name: r.Name,
                Project: null,
                Organization: organization,
                SourceType: SourceType.GitHub,
                Description: r.Description,
                DefaultBranch: r.DefaultBranch));
        }
        catch (NotFoundException)
        {
            // Fall back to user repos if not an organization
            _logger.Information("GitHub org {Org} not found, trying as user", organization);

            var repos = await client.Repository.GetAllForUser(organization);

            return repos.Select(r => new DiscoveredRepository(
                Name: r.Name,
                Project: null,
                Organization: organization,
                SourceType: SourceType.GitHub,
                Description: r.Description,
                DefaultBranch: r.DefaultBranch));
        }
    }

    private async Task<IEnumerable<DiscoveredRepository>> GetAzureDevOpsRepositoriesAsync(
        string organization,
        string? project,
        string pat,
        CancellationToken cancellationToken)
    {
        var orgUrl = new Uri($"https://dev.azure.com/{organization}");
        var credentials = new VssBasicCredential(string.Empty, pat);
        using var connection = new VssConnection(orgUrl, credentials);

        var gitClient = connection.GetClient<GitHttpClient>();

        IEnumerable<GitRepository> repos;

        if (!string.IsNullOrEmpty(project))
        {
            repos = await gitClient.GetRepositoriesAsync(project, cancellationToken: cancellationToken);
        }
        else
        {
            // Get repos from all projects
            var projectClient = connection.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();

            var allRepos = new List<GitRepository>();
            foreach (var proj in projects)
            {
                try
                {
                    var projectRepos = await gitClient.GetRepositoriesAsync(proj.Name, cancellationToken: cancellationToken);
                    allRepos.AddRange(projectRepos);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to get repos for project {Project}", proj.Name);
                }
            }
            repos = allRepos;
        }

        return repos.Select(r => new DiscoveredRepository(
            Name: r.Name,
            Project: r.ProjectReference?.Name,
            Organization: organization,
            SourceType: SourceType.AzureDevOps,
            Description: null,
            DefaultBranch: r.DefaultBranch));
    }

    private async Task<bool> ValidateGitHubPatAsync(string organization, string pat)
    {
        var client = CreateGitHubClient(pat);

        try
        {
            // Try to get the current user to validate the PAT
            await client.User.Current();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> ValidateAzureDevOpsPatAsync(
        string organization,
        string pat,
        CancellationToken cancellationToken)
    {
        try
        {
            var orgUrl = new Uri($"https://dev.azure.com/{organization}");
            var credentials = new VssBasicCredential(string.Empty, pat);
            using var connection = new VssConnection(orgUrl, credentials);

            var projectClient = connection.GetClient<ProjectHttpClient>();
            // Just try to get projects - if it works, the PAT is valid
            await projectClient.GetProjects(top: 1);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static GitHubClient CreateGitHubClient(string pat)
    {
        return new GitHubClient(new ProductHeaderValue("Standup-App"))
        {
            Credentials = new Credentials(pat)
        };
    }
}
