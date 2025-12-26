using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Service for integrating with email systems to fetch client communications.
/// </summary>
public interface IEmailIntegrationService : IIntegration
{
    /// <summary>
    /// Gets client emails for the specified time range.
    /// </summary>
    /// <param name="since">Start of the time range.</param>
    /// <param name="until">End of the time range.</param>
    /// <param name="clientDomains">List of client domains to filter emails.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of client emails.</returns>
    Task<List<ClientEmail>> GetClientEmailsAsync(
        DateTimeOffset since,
        DateTimeOffset until,
        IEnumerable<string> clientDomains,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates suggested tasks from email content using AI.
    /// </summary>
    /// <param name="emails">The emails to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of suggested tasks.</returns>
    Task<List<SuggestedTask>> GenerateSuggestedTasksFromEmailsAsync(
        IEnumerable<ClientEmail> emails,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user is authenticated with the email service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if authenticated, false otherwise.</returns>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates the OAuth2 authentication flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result containing access token and user info.</returns>
    Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the current authentication and clears stored tokens.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RevokeAuthenticationAsync(CancellationToken cancellationToken = default);
}
