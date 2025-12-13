using Standup.Domain.Entities;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for report history persistence.
/// </summary>
public interface IReportHistoryRepository
{
    Task<IEnumerable<ReportHistory>> GetByClientCodeAsync(string clientCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
    Task<ReportHistory?> GetLatestByClientCodeAsync(string clientCode, CancellationToken cancellationToken = default);
    Task<ReportHistory> AddAsync(ReportHistory history, CancellationToken cancellationToken = default);
    Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);
}
