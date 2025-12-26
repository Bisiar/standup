namespace Standup.Domain.Entities;

/// <summary>
/// Result of an OAuth2 authentication operation.
/// </summary>
public record AuthenticationResult(
    bool IsSuccess,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTimeOffset? ExpiresAt = null,
    string? UserEmail = null,
    string? UserDisplayName = null,
    string? ErrorMessage = null)
{
    /// <summary>
    /// Gets a value indicating whether the token is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the authentication failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Creates a successful authentication result.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="expiresAt">Token expiration time.</param>
    /// <param name="userEmail">User's email address.</param>
    /// <param name="userDisplayName">User's display name.</param>
    /// <returns>A successful authentication result.</returns>
    public static AuthenticationResult Success(
        string accessToken,
        string? refreshToken,
        DateTimeOffset expiresAt,
        string? userEmail = null,
        string? userDisplayName = null)
    {
        return new AuthenticationResult(
            true,
            accessToken,
            refreshToken,
            expiresAt,
            userEmail,
            userDisplayName);
    }

    /// <summary>
    /// Creates a failed authentication result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed authentication result.</returns>
    public static AuthenticationResult Failure(string errorMessage)
    {
        return new AuthenticationResult(false, ErrorMessage: errorMessage);
    }
}
