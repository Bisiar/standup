using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Application.Services;

/// <summary>
/// Application service for managing external integrations.
/// </summary>
public sealed class IntegrationService
{
    private readonly IEncryptionService _encryptionService;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly Dictionary<IntegrationType, IIntegration> _integrations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationService"/> class.
    /// </summary>
    /// <param name="encryptionService">The encryption service for secure credential storage.</param>
    /// <param name="integrationRepository">The repository for persisting integration settings.</param>
    public IntegrationService(
        IEncryptionService encryptionService,
        IIntegrationRepository integrationRepository)
    {
        _encryptionService = encryptionService;
        _integrationRepository = integrationRepository;
    }

    /// <summary>
    /// Registers an integration implementation.
    /// </summary>
    /// <param name="type">The type of integration.</param>
    /// <param name="integration">The integration implementation.</param>
    public void RegisterIntegration(IntegrationType type, IIntegration integration)
    {
        _integrations[type] = integration;
    }

    /// <summary>
    /// Gets an integration by type.
    /// </summary>
    /// <param name="type">The type of integration.</param>
    /// <returns>The integration implementation, or null if not found.</returns>
    public IIntegration? GetIntegration(IntegrationType type)
    {
        return _integrations.GetValueOrDefault(type);
    }

    /// <summary>
    /// Gets all registered integrations.
    /// </summary>
    /// <returns>A collection of all registered integrations.</returns>
    public IEnumerable<KeyValuePair<IntegrationType, IIntegration>> GetAllIntegrations()
    {
        return _integrations;
    }

    /// <summary>
    /// Validates an integration's connection.
    /// </summary>
    /// <param name="type">The type of integration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection is valid, false otherwise.</returns>
    public async Task<bool> ValidateIntegrationAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        var integration = GetIntegration(type);
        if (integration == null)
        {
            return false;
        }

        return await integration.ValidateConnectionAsync(cancellationToken);
    }

    /// <summary>
    /// Fetches data from an integration.
    /// </summary>
    /// <param name="type">The type of integration.</param>
    /// <param name="clientCode">The client code to filter data.</param>
    /// <param name="since">The start of the time range.</param>
    /// <param name="until">The end of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integration data for the specified time range.</returns>
    public async Task<IntegrationData?> FetchIntegrationDataAsync(
        IntegrationType type,
        string clientCode,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        var integration = GetIntegration(type);
        if (integration == null || !integration.IsEnabled)
        {
            return null;
        }

        return await integration.FetchDataAsync(clientCode, since, until, cancellationToken);
    }

    /// <summary>
    /// Encrypts a credential value.
    /// </summary>
    /// <param name="plainText">The plain text credential.</param>
    /// <returns>The encrypted credential.</returns>
    public async Task<string> EncryptCredentialAsync(string plainText)
    {
        return await _encryptionService.EncryptAsync(plainText);
    }

    /// <summary>
    /// Decrypts a credential value.
    /// </summary>
    /// <param name="cipherText">The encrypted credential.</param>
    /// <returns>The decrypted plain text credential.</returns>
    public async Task<string> DecryptCredentialAsync(string cipherText)
    {
        return await _encryptionService.DecryptAsync(cipherText);
    }

    /// <summary>
    /// Applies integration settings to an integration implementation.
    /// </summary>
    /// <param name="settings">The integration settings to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ApplyIntegrationSettingsAsync(
        IntegrationSettings settings,
        CancellationToken cancellationToken = default)
    {
        var integration = GetIntegration(settings.Type);
        if (integration == null)
        {
            throw new InvalidOperationException($"Integration of type {settings.Type} is not registered.");
        }

        integration.IsEnabled = settings.IsEnabled;

        if (settings.IsEnabled)
        {
            var validationSuccess = await integration.ValidateConnectionAsync(cancellationToken);
            settings.LastValidatedAt = DateTimeOffset.UtcNow;
            settings.LastValidationSuccess = validationSuccess;

            if (!validationSuccess)
            {
                settings.LastValidationError = "Connection validation failed.";
            }
            else
            {
                settings.LastValidationError = null;
            }
        }

        settings.UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets all integration settings from the repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all integration settings.</returns>
    public Task<IEnumerable<IntegrationSettings>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return _integrationRepository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Gets integration settings by type.
    /// </summary>
    /// <param name="type">The type of integration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration settings, or null if not found.</returns>
    public Task<IntegrationSettings?> GetSettingsByTypeAsync(
        IntegrationType type,
        CancellationToken cancellationToken = default)
    {
        return _integrationRepository.GetByTypeAsync(type, cancellationToken);
    }

    /// <summary>
    /// Saves integration settings to the repository.
    /// </summary>
    /// <param name="settings">The integration settings to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved integration settings.</returns>
    public async Task<IntegrationSettings> SaveSettingsAsync(
        IntegrationSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(settings.Id))
        {
            settings.Id = Guid.NewGuid().ToString();
            settings.CreatedAt = DateTimeOffset.UtcNow;
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            return await _integrationRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            return await _integrationRepository.UpdateAsync(settings, cancellationToken);
        }
    }

    /// <summary>
    /// Deletes integration settings.
    /// </summary>
    /// <param name="id">The ID of the integration settings to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteSettingsAsync(string id, CancellationToken cancellationToken = default)
    {
        return _integrationRepository.DeleteAsync(id, cancellationToken);
    }
}
