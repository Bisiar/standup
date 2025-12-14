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
    Task<IEnumerable<string>> GetProjectsAsync(
        SourceType sourceType,
        string organization,
        string pat,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the PAT can authenticate to the organization.
    /// </summary>
    Task<bool> ValidatePatAsync(
        SourceType sourceType,
        string organization,
        string pat,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a repository discovered from a source provider.
/// </summary>
public record DiscoveredRepository(
    string Name,
    string? Project,
    string Organization,
    SourceType SourceType,
    string? Description = null,
    string? DefaultBranch = null);
