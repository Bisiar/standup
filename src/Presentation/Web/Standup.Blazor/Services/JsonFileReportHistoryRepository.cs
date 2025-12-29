// <copyright file="JsonFileReportHistoryRepository.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Text.Json;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Blazor.Services;

/// <summary>
/// JSON file-based implementation of IReportHistoryRepository for Blazor Server.
/// </summary>
public class JsonFileReportHistoryRepository : IReportHistoryRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<ReportHistory> _history = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileReportHistoryRepository"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public JsonFileReportHistoryRepository(IConfiguration configuration)
    {
        var dataPath = configuration["DataPath"] ?? "./data";
        Directory.CreateDirectory(dataPath);
        _filePath = Path.Combine(dataPath, "report-history.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        LoadAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReportHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _history.OrderByDescending(h => h.GeneratedAt).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ReportHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _history.FirstOrDefault(h => h.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _history
                .Where(h => h.GroupId == groupId)
                .OrderByDescending(h => h.GeneratedAt)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ReportHistory?> GetLatestByGroupIdAsync(
        string groupId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _history
                .Where(h => h.GroupId == groupId)
                .OrderByDescending(h => h.GeneratedAt)
                .FirstOrDefault();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ReportHistory> AddAsync(ReportHistory history, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _history.Add(history);
            await SaveAsync();
            return history;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _history.RemoveAll(h => h.Id == id);
            await SaveAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _history.RemoveAll(h => h.GroupId == groupId);
            await SaveAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _history.Clear();
            await SaveAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task LoadAsync()
    {
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            _history = JsonSerializer.Deserialize<List<ReportHistory>>(json, _jsonOptions) ?? new();
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_history, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
