using Standup.Application.DTOs;

namespace Standup.Application.Interfaces;

/// <summary>
/// Service for caching standup reports to avoid regeneration.
/// </summary>
public interface IReportCacheService
{
    /// <summary>
    /// Gets a cached report if available and still valid.
    /// </summary>
    /// <param name="groupId">The repository group ID.</param>
    /// <param name="since">Start of the report period.</param>
    /// <param name="until">End of the report period.</param>
    /// <returns>The cached report, or null if not found or expired.</returns>
    Task<GroupedStandupReportDto?> GetCachedReportAsync(
        string groupId,
        DateTimeOffset since,
        DateTimeOffset until);

    /// <summary>
    /// Caches a generated report.
    /// </summary>
    /// <param name="groupId">The repository group ID.</param>
    /// <param name="report">The report to cache.</param>
    /// <param name="expiration">Optional expiration time. Defaults to 1 hour.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CacheReportAsync(
        string groupId,
        GroupedStandupReportDto report,
        TimeSpan? expiration = null);

    /// <summary>
    /// Invalidates cached reports for a specific group.
    /// </summary>
    /// <param name="groupId">The repository group ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvalidateCacheAsync(string groupId);

    /// <summary>
    /// Invalidates all cached reports.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvalidateAllAsync();

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Current cache statistics.</returns>
    CacheStats GetStats();
}
