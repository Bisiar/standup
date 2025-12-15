using Standup.Domain.Enums;

namespace Standup.Application.Interfaces;

/// <summary>
/// Provides repository discovery for source control providers.
/// </summary>
public interface IRepositoryDiscoveryService
{
    /// <summary>
    /// Gets available repositories for the given organization and PAT.
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="project">The project name (Azure DevOps only).</param>
    /// <param name="pat">The personal access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of discovered repositories.</returns>
    Task<IEnumerable<DiscoveredRepository>> GetRepositoriesAsync(
        SourceType sourceType,
        string organization,
        string? project,
        string pat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available projects for Azure DevOps organizations.
    /// Returns empty for GitHub (no project concept).
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="pat">The personal access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of project names.</returns>
    Task<IEnumerable<string>> GetProjectsAsync(
        SourceType sourceType,
        string organization,
        string pat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the PAT can authenticate to the organization.
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="pat">The personal access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if PAT is valid, false otherwise.</returns>
    Task<bool> ValidatePatAsync(
        SourceType sourceType,
        string organization,
        string pat,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a repository discovered from a source provider.
/// </summary>
/// <param name="Name">The repository name.</param>
/// <param name="Project">The project name (Azure DevOps only).</param>
/// <param name="Organization">The organization name.</param>
/// <param name="SourceType">The type of source control system.</param>
/// <param name="Description">Optional repository description.</param>
/// <param name="DefaultBranch">Optional default branch name.</param>
public record DiscoveredRepository(
    string Name,
    string? Project,
    string Organization,
    SourceType SourceType,
    string? Description = null,
    string? DefaultBranch = null);
