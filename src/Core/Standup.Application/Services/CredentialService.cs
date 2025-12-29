using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.Services;

/// <summary>
/// Application service for managing organization credentials.
/// </summary>
public sealed class CredentialService
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;

    public CredentialService(
        ICredentialRepository credentialRepository,
        IEncryptionService encryptionService)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
    }

    public Task<IEnumerable<OrgCredential>> GetCredentialsAsync(CancellationToken cancellationToken = default)
        => _credentialRepository.GetAllAsync(cancellationToken);

    public Task<OrgCredential?> GetCredentialAsync(
        SourceType sourceType,
        string organization,
        CancellationToken cancellationToken = default)
        => _credentialRepository.GetByOrgAsync(sourceType, organization, cancellationToken);

    public async Task<OrgCredential> SaveCredentialAsync(
        SourceType sourceType,
        string organization,
        string plainTextPat,
        CancellationToken cancellationToken = default)
    {
        var encryptedPat = await _encryptionService.EncryptAsync(plainTextPat);

        var existing = await _credentialRepository.GetByOrgAsync(sourceType, organization, cancellationToken);

        var credential = new OrgCredential
        {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            SourceType = sourceType,
            Organization = organization,
            EncryptedPat = encryptedPat
        };

        return await _credentialRepository.SaveAsync(credential, cancellationToken);
    }

    public Task DeleteCredentialAsync(string credentialId, CancellationToken cancellationToken = default)
        => _credentialRepository.DeleteAsync(credentialId, cancellationToken);
}
