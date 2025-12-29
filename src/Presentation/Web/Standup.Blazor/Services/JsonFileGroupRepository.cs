// <copyright file="JsonFileGroupRepository.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Text.Json;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Blazor.Services;

/// <summary>
/// JSON file-based implementation of IGroupRepository for Blazor Server.
/// </summary>
public class JsonFileGroupRepository : IGroupRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<RepositoryGroup> _groups = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileGroupRepository"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public JsonFileGroupRepository(IConfiguration configuration)
    {
        var dataPath = configuration["DataPath"] ?? "./data";
        Directory.CreateDirectory(dataPath);
        _filePath = Path.Combine(dataPath, "groups.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        LoadAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<RepositoryGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _groups.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryGroup?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _groups.FirstOrDefault(g => g.Id == groupId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryGroup?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _groups.FirstOrDefault(g => g.IsDefault) ?? _groups.FirstOrDefault();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryGroup> AddAsync(RepositoryGroup group, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _groups.Add(group);
            await SaveAsync();
            return group;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<RepositoryGroup> UpdateAsync(RepositoryGroup group, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var index = _groups.FindIndex(g => g.Id == group.Id);
            if (index >= 0)
            {
                _groups[index] = group;
                await SaveAsync();
            }

            return group;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string groupId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _groups.RemoveAll(g => g.Id == groupId);
            await SaveAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetDefaultAsync(string groupId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            foreach (var g in _groups)
            {
                g.IsDefault = g.Id == groupId;
            }

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
            _groups = JsonSerializer.Deserialize<List<RepositoryGroup>>(json, _jsonOptions) ?? new();
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_groups, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
