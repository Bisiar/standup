using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

/// <summary>
/// Organization-level credential for PAT inheritance.
/// Repos in this org use this PAT unless they have their own.
/// </summary>
public class OrgCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SourceType SourceType { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string EncryptedPat { get; set; } = string.Empty;
}
