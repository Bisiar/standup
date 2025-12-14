using System.Text.Json;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Maui.Repositories;

/// <summary>
/// MAUI-specific implementation of IGroupRepository using Preferences for persistence.
/// </summary>
public sealed class PreferencesGroupRepository : IGroupRepository
{
    private const string GroupsKey = "standup_groups";
    private const string DefaultGroupKey = "standup_default_group";
    private List<RepositoryGroup>? _cachedGroups;

    public Task<IEnumerable<RepositoryGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Log.Debug("PreferencesGroupRepository.GetAllAsync called");

        if (_cachedGroups != null)
        {
            return Task.FromResult<IEnumerable<RepositoryGroup>>(_cachedGroups);
        }

        try
        {
            var json = Preferences.Default.Get<string?>(GroupsKey, null);

            if (string.IsNullOrEmpty(json))
            {
                _cachedGroups = new List<RepositoryGroup>();
            }
            else
            {
                _cachedGroups = JsonSerializer.Deserialize<List<RepositoryGroup>>(json) ?? new();
                Log.Debug("Loaded {Count} groups from Preferences", _cachedGroups.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load groups from Preferences");
            _cachedGroups = new List<RepositoryGroup>();
        }

        return Task.FromResult<IEnumerable<RepositoryGroup>>(_cachedGroups);
    }

    public async Task<RepositoryGroup?> GetByIdAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var groups = await GetAllAsync(cancellationToken);
        return groups.FirstOrDefault(g => g.Id == groupId);
    }

    public async Task<RepositoryGroup?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        var defaultId = Preferences.Default.Get<string?>(DefaultGroupKey, null);
        var groups = await GetAllAsync(cancellationToken);

        if (!string.IsNullOrEmpty(defaultId))
        {
            var defaultGroup = groups.FirstOrDefault(g => g.Id == defaultId);
            if (defaultGroup != null)
            {
                return defaultGroup;
            }
        }

        return groups.FirstOrDefault(g => g.IsDefault) ?? groups.FirstOrDefault();
    }

    public async Task<RepositoryGroup> AddAsync(RepositoryGroup group, CancellationToken cancellationToken = default)
    {
        var groups = (await GetAllAsync(cancellationToken)).ToList();

        if (group.IsDefault)
        {
            foreach (var g in groups)
            {
                g.IsDefault = false;
            }
        }

        groups.Add(group);
        _cachedGroups = groups;
        SaveGroups();

        Log.Information("Added group: {Name} ({Id})", group.Name, group.Id);
        return group;
    }

    public async Task<RepositoryGroup> UpdateAsync(RepositoryGroup group, CancellationToken cancellationToken = default)
    {
        var groups = (await GetAllAsync(cancellationToken)).ToList();
        var index = groups.FindIndex(g => g.Id == group.Id);

        if (index < 0)
        {
            throw new InvalidOperationException($"Group {group.Id} not found");
        }

        if (group.IsDefault)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (i != index)
                {
                    groups[i].IsDefault = false;
                }
            }
        }

        groups[index] = group;
        _cachedGroups = groups;
        SaveGroups();

        Log.Information("Updated group: {Name} ({Id})", group.Name, group.Id);
        return group;
    }

    public async Task DeleteAsync(string groupId, CancellationToken cancellationToken = default)
    {
        var groups = (await GetAllAsync(cancellationToken)).ToList();
        groups.RemoveAll(g => g.Id == groupId);
        _cachedGroups = groups;
        SaveGroups();

        Log.Information("Deleted group: {Id}", groupId);
    }

    public async Task SetDefaultAsync(string groupId, CancellationToken cancellationToken = default)
    {
        Preferences.Default.Set(DefaultGroupKey, groupId);

        var groups = (await GetAllAsync(cancellationToken)).ToList();
        foreach (var g in groups)
        {
            g.IsDefault = g.Id == groupId;
        }

        _cachedGroups = groups;
        SaveGroups();

        Log.Information("Set default group: {Id}", groupId);
    }

    private void SaveGroups()
    {
        if (_cachedGroups == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(_cachedGroups);
        Preferences.Default.Set(GroupsKey, json);
        Log.Debug("Saved {Count} groups to Preferences", _cachedGroups.Count);
    }
}
