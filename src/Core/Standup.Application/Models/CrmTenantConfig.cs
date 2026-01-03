// -----------------------------------------------------------------------
// <copyright file="CrmTenantConfig.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Standup.Application.Models;

/// <summary>
/// Configuration for a Dynamics 365 CRM tenant connection.
/// </summary>
/// <param name="Id">Unique identifier for this tenant config.</param>
/// <param name="Name">Friendly name for this CRM tenant (e.g., "JourneyTeam CRM").</param>
/// <param name="InstanceUrl">The Dynamics 365 instance URL (e.g., https://org.crm.dynamics.com).</param>
public record CrmTenantConfig(
    Guid Id,
    string Name,
    string InstanceUrl)
{
    /// <summary>
    /// Gets or sets the Azure AD tenant ID (optional, for client credentials auth).
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the Azure AD client/application ID (optional, for client credentials auth).
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// Gets or sets the client secret (optional, for client credentials auth).
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Gets a value indicating whether this tenant uses client credentials auth.
    /// If false, uses Azure Identity (DefaultAzureCredential).
    /// </summary>
    public bool UsesClientCredentials =>
        !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);

    /// <summary>
    /// Gets the authentication method description for display.
    /// </summary>
    public string AuthMethodDisplay =>
        UsesClientCredentials ? "Client Credentials" : "Azure Identity";

    /// <summary>
    /// Creates a new CRM tenant config with a generated ID.
    /// </summary>
    /// <param name="name">Friendly name.</param>
    /// <param name="instanceUrl">Instance URL.</param>
    /// <returns>A new CrmTenantConfig instance.</returns>
    public static CrmTenantConfig Create(string name, string instanceUrl) =>
        new(Guid.NewGuid(), name, instanceUrl);
}
