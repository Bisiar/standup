using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for organization credentials persistence.
/// </summary>
public interface ICredentialRepository
{
    Task<IEnumerable<OrgCredential>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrgCredential?> GetByOrgAsync(SourceType sourceType, string organization, CancellationToken cancellationToken = default);
    Task<OrgCredential> SaveAsync(OrgCredential credential, CancellationToken cancellationToken = default);
    Task DeleteAsync(string credentialId, CancellationToken cancellationToken = default);
}
