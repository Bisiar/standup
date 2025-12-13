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
    public bool IsActive { get; set; } = true;
}
