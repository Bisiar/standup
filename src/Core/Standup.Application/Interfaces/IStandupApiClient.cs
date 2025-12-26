using Standup.Application.DTOs;

namespace Standup.Application.Interfaces;

/// <summary>
/// Client interface for communicating with the Standup API.
/// </summary>
public interface IStandupApiClient
{
    /// <summary>
    /// Sets the project endpoint and access token for API calls.
    /// </summary>
    /// <param name="apiEndpoint">The API endpoint URL.</param>
    /// <param name="accessToken">Optional access token for authentication.</param>
    void SetProject(string apiEndpoint, string? accessToken);

    /// <summary>
    /// Generates a standup report for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated standup report.</returns>
    Task<StandupReportDto> GenerateStandupAsync(string userId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured repositories for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of source repositories.</returns>
    Task<IEnumerable<SourceRepositoryDto>> GetRepositoriesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new repository configuration for the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="repository">The repository to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created repository.</returns>
    Task<SourceRepositoryDto> AddRepositoryAsync(string userId, CreateSourceRepositoryDto repository, CancellationToken cancellationToken = default);
}
