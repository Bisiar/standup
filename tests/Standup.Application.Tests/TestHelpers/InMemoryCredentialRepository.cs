using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.Tests.TestHelpers;

/// <summary>
/// In-memory implementation of ICredentialRepository for testing.
/// NO MOCKS - real test implementation.
/// </summary>
public sealed class InMemoryCredentialRepository : ICredentialRepository
{
    private readonly List<OrgCredential> _credentials = new();

    public Task<IEnumerable<OrgCredential>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<OrgCredential>>(_credentials.ToList());
    }

    public Task<OrgCredential?> GetByOrgAsync(
        SourceType sourceType,
        string organization,
        CancellationToken cancellationToken = default)
    {
        var credential = _credentials.FirstOrDefault(c =>
            c.SourceType == sourceType &&
            c.Organization.Equals(organization, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(credential);
    }

    public Task<OrgCredential> SaveAsync(
        OrgCredential credential,
        CancellationToken cancellationToken = default)
    {
        var existing = _credentials.FirstOrDefault(c => c.Id == credential.Id);
        if (existing != null)
        {
            _credentials.Remove(existing);
        }

        _credentials.Add(credential);
        return Task.FromResult(credential);
    }

    public Task DeleteAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        var credential = _credentials.FirstOrDefault(c => c.Id == credentialId);
        if (credential != null)
        {
            _credentials.Remove(credential);
        }

        return Task.CompletedTask;
    }
}
