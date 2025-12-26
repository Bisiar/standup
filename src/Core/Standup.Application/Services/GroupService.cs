using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Application.Services;

/// <summary>
/// Application service for managing repository groups.
/// </summary>
public sealed class GroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;

    public GroupService(
        IGroupRepository groupRepository,
        ICredentialRepository credentialRepository,
        IEncryptionService encryptionService)
    {
        _groupRepository = groupRepository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
    }

    public Task<IEnumerable<RepositoryGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
        => _groupRepository.GetAllAsync(cancellationToken);

    public Task<RepositoryGroup?> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
        => _groupRepository.GetByIdAsync(groupId, cancellationToken);

    public Task<RepositoryGroup?> GetDefaultGroupAsync(CancellationToken cancellationToken = default)
        => _groupRepository.GetDefaultAsync(cancellationToken);

    public async Task<RepositoryGroup> AddGroupAsync(RepositoryGroup group, CancellationToken cancellationToken = default)
    {
        group.Id = Guid.NewGuid().ToString();
        group.CreatedAt = DateTimeOffset.UtcNow;
        return await _groupRepository.AddAsync(group, cancellationToken);
    }

    public Task<RepositoryGroup> UpdateGroupAsync(RepositoryGroup group, CancellationToken cancellationToken = default)
        => _groupRepository.UpdateAsync(group, cancellationToken);

    public Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
        => _groupRepository.DeleteAsync(groupId, cancellationToken);

    public Task SetDefaultGroupAsync(string groupId, CancellationToken cancellationToken = default)
        => _groupRepository.SetDefaultAsync(groupId, cancellationToken);

    public async Task<GroupedRepository> AddRepositoryToGroupAsync(
        string groupId,
        GroupedRepository repository,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new InvalidOperationException($"Group {groupId} not found");

        repository.Id = Guid.NewGuid().ToString();
        group.Repositories.Add(repository);

        await _groupRepository.UpdateAsync(group, cancellationToken);
        return repository;
    }

    /// <summary>
    /// Removes a repository from a group.
    /// </summary>
    /// <param name="groupId">The ID of the group.</param>
    /// <param name="repositoryId">The ID of the repository to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RemoveRepositoryFromGroupAsync(
        string groupId,
        string repositoryId,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new InvalidOperationException($"Group {groupId} not found");

        group.Repositories.RemoveAll(r => r.Id == repositoryId);
        await _groupRepository.UpdateAsync(group, cancellationToken);
    }

    /// <summary>
    /// Updates an existing repository within a group.
    /// </summary>
    /// <param name="groupId">The ID of the group containing the repository.</param>
    /// <param name="repository">The updated repository data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated repository.</returns>
    public async Task<GroupedRepository> UpdateRepositoryInGroupAsync(
        string groupId,
        GroupedRepository repository,
        CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken)
            ?? throw new InvalidOperationException($"Group {groupId} not found");

        var index = group.Repositories.FindIndex(r => r.Id == repository.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Repository {repository.Id} not found in group {groupId}");
        }

        group.Repositories[index] = repository;
        await _groupRepository.UpdateAsync(group, cancellationToken);
        return repository;
    }

    /// <summary>
    /// Gets the PAT for a repository, using repo-level PAT if available, otherwise org-level PAT.
    /// </summary>
    /// <param name="repository">The repository to get the PAT for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted PAT, or null if no PAT is configured.</returns>
    public async Task<string?> GetDecryptedPatForRepositoryAsync(
        GroupedRepository repository,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(repository.EncryptedPat))
        {
            return await _encryptionService.DecryptAsync(repository.EncryptedPat);
        }

        var orgCredential = await _credentialRepository.GetByOrgAsync(
            repository.SourceType, repository.Organization, cancellationToken);

        if (orgCredential != null && !string.IsNullOrEmpty(orgCredential.EncryptedPat))
        {
            return await _encryptionService.DecryptAsync(orgCredential.EncryptedPat);
        }

        return null;
    }
}
