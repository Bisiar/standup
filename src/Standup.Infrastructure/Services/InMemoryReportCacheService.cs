using System.Collections.Concurrent;
using System.Text.Json;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;

namespace Standup.Infrastructure.Services;

/// <summary>
/// In-memory implementation of report caching.
/// Suitable for single-instance deployments.
/// For distributed scenarios, use a Redis or Cosmos DB implementation.
/// </summary>
public sealed class InMemoryReportCacheService : IReportCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);
    private int _cacheHits;
    private int _cacheMisses;

    public Task<GroupedStandupReportDto?> GetCachedReportAsync(
        string groupId,
        DateTimeOffset since,
        DateTimeOffset until)
    {
        var key = BuildCacheKey(groupId, since, until);

        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                Interlocked.Increment(ref _cacheHits);
                Log.Debug("Cache HIT for {GroupId} ({Since} - {Until})", groupId, since, until);
                return Task.FromResult<GroupedStandupReportDto?>(entry.Report);
            }

            // Expired - remove it
            _cache.TryRemove(key, out _);
            Log.Debug("Cache entry expired for {GroupId}", groupId);
        }

        Interlocked.Increment(ref _cacheMisses);
        Log.Debug("Cache MISS for {GroupId} ({Since} - {Until})", groupId, since, until);
        return Task.FromResult<GroupedStandupReportDto?>(null);
    }

    public Task CacheReportAsync(
        string groupId,
        GroupedStandupReportDto report,
        TimeSpan? expiration = null)
    {
        var key = BuildCacheKey(groupId, report.PeriodStart, report.PeriodEnd);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration);

        var entry = new CacheEntry(report, expiresAt);
        _cache[key] = entry;

        Log.Information(
            "Cached report for {GroupId} (expires {ExpiresAt})",
            groupId,
            expiresAt);

        // Cleanup old entries periodically
        CleanupExpiredEntries();

        return Task.CompletedTask;
    }

    public Task InvalidateCacheAsync(string groupId)
    {
        var keysToRemove = _cache.Keys
            .Where(k => k.StartsWith($"{groupId}:"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        Log.Information("Invalidated {Count} cache entries for {GroupId}", keysToRemove.Count, groupId);
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync()
    {
        var count = _cache.Count;
        _cache.Clear();
        Log.Information("Invalidated all {Count} cache entries", count);
        return Task.CompletedTask;
    }

    public CacheStats GetStats()
    {
        var approximateSize = _cache.Values.Sum(e => EstimateSize(e.Report));

        return new CacheStats(
            CachedReports: _cache.Count,
            CacheHits: _cacheHits,
            CacheMisses: _cacheMisses,
            ApproximateSizeBytes: approximateSize);
    }

    private static string BuildCacheKey(string groupId, DateTimeOffset since, DateTimeOffset until)
    {
        // Use date-only to allow some flexibility in time
        return $"{groupId}:{since:yyyy-MM-dd}:{until:yyyy-MM-dd}";
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            Log.Debug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
        }
    }

    private static long EstimateSize(GroupedStandupReportDto report)
    {
        // Rough estimation based on typical JSON serialization size
        try
        {
            var json = JsonSerializer.Serialize(report);
            return json.Length * 2; // UTF-16 chars
        }
        catch
        {
            // Fallback estimate
            return 10000 + (report.TotalCommits * 500) + (report.TotalPullRequests * 1000);
        }
    }

    private sealed record CacheEntry(
        GroupedStandupReportDto Report,
        DateTimeOffset ExpiresAt);
}
