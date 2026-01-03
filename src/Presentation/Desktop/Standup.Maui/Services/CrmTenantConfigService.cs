// -----------------------------------------------------------------------
// <copyright file="CrmTenantConfigService.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Domain.Interfaces;
using Standup.Maui.Json;

namespace Standup.Maui.Services;

/// <summary>
/// Service for managing CRM tenant configurations using MAUI Preferences.
/// </summary>
public class CrmTenantConfigService : ICrmTenantConfigService
{
    private const string ConfigsKey = "standup_crm_tenant_configs";
    private readonly ICrmProjectService? _crmProjectService;
    private List<CrmTenantConfig>? _cachedConfigs;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrmTenantConfigService"/> class.
    /// </summary>
    /// <param name="crmProjectService">Optional CRM project service for connection testing.</param>
    public CrmTenantConfigService(ICrmProjectService? crmProjectService = null)
    {
        _crmProjectService = crmProjectService;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CrmTenantConfig>> GetAllAsync()
    {
        if (_cachedConfigs != null)
        {
            return Task.FromResult<IReadOnlyList<CrmTenantConfig>>(_cachedConfigs);
        }

        try
        {
            var json = Preferences.Get(ConfigsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _cachedConfigs = new List<CrmTenantConfig>();
            }
            else
            {
                _cachedConfigs = JsonSerializer.Deserialize(json, MauiJsonContext.Default.ListCrmTenantConfig)
                    ?? new List<CrmTenantConfig>();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load CRM tenant configs from Preferences");
            _cachedConfigs = new List<CrmTenantConfig>();
        }

        return Task.FromResult<IReadOnlyList<CrmTenantConfig>>(_cachedConfigs);
    }

    /// <inheritdoc/>
    public async Task<CrmTenantConfig?> GetByIdAsync(Guid id)
    {
        var configs = await GetAllAsync();
        return configs.FirstOrDefault(c => c.Id == id);
    }

    /// <inheritdoc/>
    public async Task AddAsync(CrmTenantConfig config)
    {
        await GetAllAsync(); // Ensure cache is loaded
        _cachedConfigs!.Add(config);
        await SaveAsync();
        Log.Information("Added CRM tenant config: {Name} ({InstanceUrl})", config.Name, config.InstanceUrl);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(CrmTenantConfig config)
    {
        await GetAllAsync(); // Ensure cache is loaded
        var index = _cachedConfigs!.FindIndex(c => c.Id == config.Id);
        if (index >= 0)
        {
            _cachedConfigs[index] = config;
            await SaveAsync();
            Log.Information("Updated CRM tenant config: {Name}", config.Name);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        await GetAllAsync(); // Ensure cache is loaded
        var removed = _cachedConfigs!.RemoveAll(c => c.Id == id);
        if (removed > 0)
        {
            await SaveAsync();
            Log.Information("Deleted CRM tenant config: {Id}", id);
        }
    }

    /// <inheritdoc/>
    public Task<(bool Success, string Message)> TestConnectionAsync(CrmTenantConfig config)
    {
        if (string.IsNullOrEmpty(config.InstanceUrl))
        {
            return Task.FromResult((false, "Instance URL is required"));
        }

        // Connection testing will be delegated to the UI layer which can create
        // a temporary DynamicsCrmService with the config to test
        // For now, we just validate the URL format
        if (!Uri.TryCreate(config.InstanceUrl, UriKind.Absolute, out var uri))
        {
            return Task.FromResult((false, "Invalid URL format"));
        }

        if (uri.Scheme != "https")
        {
            return Task.FromResult((false, "URL must use HTTPS"));
        }

        Log.Information("CRM config validated: {Name} ({InstanceUrl})", config.Name, config.InstanceUrl);
        return Task.FromResult((true, "Configuration valid. Save to test connection."));
    }

    private Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_cachedConfigs, MauiJsonContext.Default.ListCrmTenantConfig);
            Preferences.Set(ConfigsKey, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save CRM tenant configs to Preferences");
        }

        return Task.CompletedTask;
    }
}
