// <copyright file="JsonFileCredentialRepository.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using System.Text.Json;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Blazor.Services;

/// <summary>
/// JSON file-based implementation of ICredentialRepository for Blazor Server.
/// </summary>
public class JsonFileCredentialRepository : ICredentialRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<OrgCredential> _credentials = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFileCredentialRepository"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public JsonFileCredentialRepository(IConfiguration configuration)
    {
        var dataPath = configuration["DataPath"] ?? "./data";
        Directory.CreateDirectory(dataPath);
        _filePath = Path.Combine(dataPath, "credentials.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        LoadAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<OrgCredential>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _credentials.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<OrgCredential?> GetByOrgAsync(
        SourceType sourceType,
        string organization,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _credentials.FirstOrDefault(c =>
                c.SourceType == sourceType &&
                c.Organization.Equals(organization, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<OrgCredential> SaveAsync(OrgCredential credential, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var index = _credentials.FindIndex(c =>
                c.SourceType == credential.SourceType &&
                c.Organization.Equals(credential.Organization, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                _credentials[index] = credential;
            }
            else
            {
                _credentials.Add(credential);
            }

            await SaveAsync();
            return credential;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _credentials.RemoveAll(c => c.Id == credentialId);
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
            _credentials = JsonSerializer.Deserialize<List<OrgCredential>>(json, _jsonOptions) ?? new();
        }
    }

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_credentials, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
