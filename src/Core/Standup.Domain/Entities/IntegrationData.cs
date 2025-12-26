namespace Standup.Domain.Entities;

/// <summary>
/// Data returned from an external integration.
/// </summary>
public record IntegrationData(
    List<ClientEmail>? Emails = null,
    Dictionary<string, object>? Metadata = null)
{
    /// <summary>
    /// Gets the list of client emails from the integration.
    /// </summary>
    public List<ClientEmail> Emails { get; init; } = Emails ?? new();

    /// <summary>
    /// Gets additional metadata from the integration.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = Metadata ?? new();
}
