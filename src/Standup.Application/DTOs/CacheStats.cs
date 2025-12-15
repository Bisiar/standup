namespace Standup.Application.DTOs;

/// <summary>
/// Statistics about the report cache.
/// </summary>
/// <param name="CachedReports">Number of reports currently cached.</param>
/// <param name="CacheHits">Number of successful cache retrievals.</param>
/// <param name="CacheMisses">Number of cache misses.</param>
/// <param name="ApproximateSizeBytes">Approximate memory usage in bytes.</param>
public record CacheStats(
    int CachedReports,
    int CacheHits,
    int CacheMisses,
    long ApproximateSizeBytes);
