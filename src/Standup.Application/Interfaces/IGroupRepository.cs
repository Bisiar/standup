using Standup.Domain.Entities;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for repository groups persistence.
/// </summary>
public interface IGroupRepository
{
    Task<IEnumerable<RepositoryGroup>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RepositoryGroup?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<RepositoryGroup?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<RepositoryGroup> AddAsync(RepositoryGroup group, CancellationToken cancellationToken = default);
    Task<RepositoryGroup> UpdateAsync(RepositoryGroup group, CancellationToken cancellationToken = default);
    Task DeleteAsync(string groupId, CancellationToken cancellationToken = default);
    Task SetDefaultAsync(string groupId, CancellationToken cancellationToken = default);
}
