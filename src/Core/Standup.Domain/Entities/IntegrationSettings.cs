using Standup.Domain.Enums;

namespace Standup.Domain.Entities;

/// <summary>
/// Represents configuration settings for an external integration.
/// </summary>
public sealed class IntegrationSettings
{
    /// <summary>
    /// Gets or sets the unique identifier for this integration configuration.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of integration.
    /// </summary>
    public IntegrationType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this integration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the configuration data specific to this integration type.
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the encrypted credentials for this integration.
    /// </summary>
    public Dictionary<string, string> EncryptedCredentials { get; set; } = new();

    /// <summary>
    /// Gets or sets the client code mappings (e.g., ELGP -> CRM project GUID).
    /// </summary>
    public Dictionary<string, string> ClientCodeMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the date and time when this integration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this integration was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this integration was last validated.
    /// </summary>
    public DateTimeOffset? LastValidatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the last validation was successful.
    /// </summary>
    public bool? LastValidationSuccess { get; set; }

    /// <summary>
    /// Gets or sets the last validation error message, if any.
    /// </summary>
    public string? LastValidationError { get; set; }
}
