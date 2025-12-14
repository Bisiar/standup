using Standup.Domain.Entities;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for report history persistence.
/// </summary>
public interface IReportHistoryRepository
{
    Task<IEnumerable<ReportHistory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReportHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<ReportHistory?> GetLatestByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<ReportHistory> AddAsync(ReportHistory history, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
}
