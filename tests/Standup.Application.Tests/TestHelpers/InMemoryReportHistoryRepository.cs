using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Application.Tests.TestHelpers;

/// <summary>
/// In-memory implementation of IReportHistoryRepository for testing.
/// NO MOCKS - real test implementation.
/// </summary>
public sealed class InMemoryReportHistoryRepository : IReportHistoryRepository
{
    private readonly List<ReportHistory> _history = new();

    public Task<IEnumerable<ReportHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<ReportHistory>>(_history.ToList());
    }

    public Task<ReportHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var report = _history.FirstOrDefault(h => h.Id == id);
        return Task.FromResult(report);
    }

    public Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        var reports = _history
            .Where(h => h.GroupId == groupId)
            .OrderByDescending(h => h.GeneratedAt)
            .ToList();
        return Task.FromResult<IEnumerable<ReportHistory>>(reports);
    }

    public Task<ReportHistory?> GetLatestByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        var report = _history
            .Where(h => h.GroupId == groupId)
            .OrderByDescending(h => h.GeneratedAt)
            .FirstOrDefault();
        return Task.FromResult(report);
    }

    public Task<ReportHistory> AddAsync(
        ReportHistory history,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(history.Id))
        {
            history.Id = Guid.NewGuid().ToString();
        }

        if (history.GeneratedAt == default)
        {
            history.GeneratedAt = DateTimeOffset.UtcNow;
        }

        _history.Add(history);
        return Task.FromResult(history);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var report = _history.FirstOrDefault(h => h.Id == id);
        if (report != null)
        {
            _history.Remove(report);
        }

        return Task.CompletedTask;
    }

    public Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var reportsToDelete = _history.Where(h => h.GroupId == groupId).ToList();
        foreach (var report in reportsToDelete)
        {
            _history.Remove(report);
        }

        return Task.CompletedTask;
    }
}
