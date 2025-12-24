using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

/// <summary>
/// Interface for external integrations that enhance standup reports with additional context.
/// </summary>
public interface IIntegration
{
    /// <summary>
    /// Gets the name of the integration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the integration.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets a value indicating whether the integration is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the integration is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Validates the connection to the external service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection is valid, false otherwise.</returns>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches data from the external service for the specified time range.
    /// </summary>
    /// <param name="clientCode">The client code to filter data.</param>
    /// <param name="since">The start of the time range.</param>
    /// <param name="until">The end of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Integration data for the specified time range.</returns>
    Task<IntegrationData> FetchDataAsync(
        string clientCode,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}
