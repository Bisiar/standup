using System.Text.Json;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Maui.Repositories;

/// <summary>
/// MAUI-specific implementation of IReportHistoryRepository using Preferences for persistence.
/// </summary>
public sealed class PreferencesReportHistoryRepository : IReportHistoryRepository
{
    private const string HistoryKey = "standup_report_history";
    private List<ReportHistory>? _cachedHistory;

    public Task<IEnumerable<ReportHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return LoadAllAsync(cancellationToken);
    }

    public async Task<ReportHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var all = await LoadAllAsync(cancellationToken);
        return all.FirstOrDefault(h => h.Id == id);
    }

    public async Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        var all = await LoadAllAsync(cancellationToken);
        return all.Where(h => h.GroupId == groupId)
                  .OrderByDescending(h => h.GeneratedAt);
    }

    public async Task<ReportHistory?> GetLatestByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        var byGroup = await GetByGroupIdAsync(groupId, cancellationToken);
        return byGroup.FirstOrDefault();
    }

    public async Task<ReportHistory> AddAsync(ReportHistory history, CancellationToken cancellationToken = default)
    {
        var all = (await LoadAllAsync(cancellationToken)).ToList();

        history.Id = Guid.NewGuid().ToString();
        history.GeneratedAt = DateTimeOffset.UtcNow;
        all.Add(history);

        _cachedHistory = all;
        SaveHistory();

        Log.Information("Added report history for group {GroupName}", history.GroupName);
        return history;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var all = (await LoadAllAsync(cancellationToken)).ToList();
        var removed = all.RemoveAll(h => h.Id == id);
        _cachedHistory = all;
        SaveHistory();

        Log.Information("Deleted report history: {Id} (removed {Count})", id, removed);
    }

    public async Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var all = (await LoadAllAsync(cancellationToken)).ToList();
        all.RemoveAll(h => h.GroupId == groupId);
        _cachedHistory = all;
        SaveHistory();

        Log.Information("Deleted report history for group: {GroupId}", groupId);
    }

    private Task<IEnumerable<ReportHistory>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedHistory != null)
        {
            return Task.FromResult<IEnumerable<ReportHistory>>(_cachedHistory);
        }

        try
        {
            var json = Preferences.Default.Get<string?>(HistoryKey, null);

            if (string.IsNullOrEmpty(json))
            {
                _cachedHistory = new List<ReportHistory>();
            }
            else
            {
                _cachedHistory = JsonSerializer.Deserialize<List<ReportHistory>>(json) ?? new();
                Log.Debug("Loaded {Count} history entries from Preferences", _cachedHistory.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load report history from Preferences");
            _cachedHistory = new List<ReportHistory>();
        }

        return Task.FromResult<IEnumerable<ReportHistory>>(_cachedHistory);
    }

    private void SaveHistory()
    {
        if (_cachedHistory == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(_cachedHistory);
        Preferences.Default.Set(HistoryKey, json);
        Log.Debug("Saved {Count} history entries to Preferences", _cachedHistory.Count);
    }
}
