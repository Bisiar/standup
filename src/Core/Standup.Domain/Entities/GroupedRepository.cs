using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

/// <summary>
/// A repository within a group, tagged with a client code for grouping in reports.
/// </summary>
public class GroupedRepository
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ClientCode { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string? Project { get; set; }
    public string Repository { get; set; } = string.Empty;
    public string? EncryptedPat { get; set; }
    public string? AuthorIdentifier { get; set; }
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the API endpoint for GitHub Enterprise (e.g., https://github.company.com).
    /// Leave null for public github.com.
    /// </summary>
    public string? ApiEndpoint { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this repository is included in standup generation.
    /// Allows temporarily excluding repos without removing them from the group.
    /// </summary>
    public bool IncludeInGeneration { get; set; } = true;
}
