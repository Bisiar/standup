using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

/// <summary>
/// Represents a source code repository configuration for a user.
/// </summary>
public class SourceRepository
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }

    /// <summary>
    /// Gets or sets the organization name (GitHub org or Azure DevOps org).
    /// </summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project name (Azure DevOps only, null for GitHub).
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string Repository { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the repository.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the username to filter commits by (e.g., GitHub username or Azure DevOps email).
    /// </summary>
    public string AuthorIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted Personal Access Token.
    /// </summary>
    public string? EncryptedPat { get; set; }

    /// <summary>
    /// Gets or sets the default branch to track.
    /// </summary>
    public string DefaultBranch { get; set; } = "main";

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User? User { get; set; }

    /// <summary>
    /// Gets the full repository path identifier.
    /// </summary>
    public string FullPath => SourceType == SourceType.AzureDevOps
        ? $"{Organization}/{Project}/{Repository}"
        : $"{Organization}/{Repository}";
}
