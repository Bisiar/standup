using System.Text.Json;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Maui.Repositories;

/// <summary>
/// MAUI-specific implementation of ICredentialRepository using Preferences for persistence.
/// </summary>
public sealed class PreferencesCredentialRepository : ICredentialRepository
{
    private const string CredentialsKey = "standup_org_credentials";
    private List<OrgCredential>? _cachedCredentials;

    public Task<IEnumerable<OrgCredential>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Log.Debug("PreferencesCredentialRepository.GetAllAsync called");

        if (_cachedCredentials != null)
        {
            return Task.FromResult<IEnumerable<OrgCredential>>(_cachedCredentials);
        }

        try
        {
            var json = Preferences.Default.Get<string?>(CredentialsKey, null);

            if (string.IsNullOrEmpty(json))
            {
                _cachedCredentials = new List<OrgCredential>();
            }
            else
            {
                _cachedCredentials = JsonSerializer.Deserialize<List<OrgCredential>>(json) ?? new();
                Log.Debug("Loaded {Count} credentials from Preferences", _cachedCredentials.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load credentials from Preferences");
            _cachedCredentials = new List<OrgCredential>();
        }

        return Task.FromResult<IEnumerable<OrgCredential>>(_cachedCredentials);
    }

    public async Task<OrgCredential?> GetByOrgAsync(
        SourceType sourceType,
        string organization,
        CancellationToken cancellationToken = default)
    {
        var credentials = await GetAllAsync(cancellationToken);
        return credentials.FirstOrDefault(c =>
            c.SourceType == sourceType &&
            c.Organization.Equals(organization, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<OrgCredential> SaveAsync(OrgCredential credential, CancellationToken cancellationToken = default)
    {
        var credentials = (await GetAllAsync(cancellationToken)).ToList();

        var existingIndex = credentials.FindIndex(c =>
            c.SourceType == credential.SourceType &&
            c.Organization.Equals(credential.Organization, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            credentials[existingIndex] = credential;
            Log.Information("Updated credential for {Type}/{Org}", credential.SourceType, credential.Organization);
        }
        else
        {
            credentials.Add(credential);
            Log.Information("Added credential for {Type}/{Org}", credential.SourceType, credential.Organization);
        }

        _cachedCredentials = credentials;
        SaveCredentials();

        return credential;
    }

    public async Task DeleteAsync(string credentialId, CancellationToken cancellationToken = default)
    {
        var credentials = (await GetAllAsync(cancellationToken)).ToList();
        credentials.RemoveAll(c => c.Id == credentialId);
        _cachedCredentials = credentials;
        SaveCredentials();

        Log.Information("Deleted credential: {Id}", credentialId);
    }

    private void SaveCredentials()
    {
        if (_cachedCredentials == null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(_cachedCredentials);
        Preferences.Default.Set(CredentialsKey, json);
        Log.Debug("Saved {Count} credentials to Preferences", _cachedCredentials.Count);
    }
}
