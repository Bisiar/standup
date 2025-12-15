using System.Text.Json.Serialization;

using Standup.Domain.Enums;

namespace Standup.Application.Models;

public record ProjectInstance(
    string Id,
    string Name,
    string TenantName,
    string ApiEndpoint,
    string? UserId = null,
    string? TenantId = null,
    string? AccessToken = null,
    bool IsDefault = false,
    SourceType SourceType = SourceType.AzureDevOps,
    string? SourceOrganization = null,
    string? SourceProject = null,
    string? SourceRepository = null,
    string? SourcePat = null,
    string? AuthorIdentifier = null,
    bool UseLocalGeneration = true,
    string? ApiEndpointOverride = null)
{
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the latest commit hash (first 8 characters). Transient, not persisted.
    /// </summary>
    [JsonIgnore]
    public string? LastCommitHash { get; set; }

    /// <summary>
    /// Gets or sets the latest commit date. Transient, not persisted.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? LastCommitDate { get; set; }

    /// <summary>
    /// Gets or sets validation error message. Transient, not persisted.
    /// </summary>
    [JsonIgnore]
    public string? ValidationError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether validation is in progress. Transient, not persisted.
    /// </summary>
    [JsonIgnore]
    public bool IsValidating { get; set; }

    /// <summary>
    /// Gets the formatted commit info display string.
    /// </summary>
    [JsonIgnore]
    public string CommitInfoDisplay => LastCommitHash != null && LastCommitDate != null
        ? $"{LastCommitHash} • {LastCommitDate:MMM dd}"
        : ValidationError ?? string.Empty;
}
