using Standup.Domain.Entities;

namespace Standup.Application.Interfaces;

/// <summary>
/// Repository interface for report history persistence.
/// </summary>
public interface IReportHistoryRepository
{
    /// <summary>
    /// Gets all report history entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all report history entries.</returns>
    Task<IEnumerable<ReportHistory>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a report history entry by ID.
    /// </summary>
    /// <param name="id">The report history ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The report history entry, or null if not found.</returns>
    Task<ReportHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all report history entries for a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of report history entries for the group.</returns>
    Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest report history entry for a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest report history entry, or null if none exists.</returns>
    Task<ReportHistory?> GetLatestByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new report history entry.
    /// </summary>
    /// <param name="history">The report history entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added report history entry.</returns>
    Task<ReportHistory> AddAsync(ReportHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a report history entry by ID.
    /// </summary>
    /// <param name="id">The report history ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all report history entries for a group.
    /// </summary>
    /// <param name="groupId">The group ID to delete history for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all report history entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
