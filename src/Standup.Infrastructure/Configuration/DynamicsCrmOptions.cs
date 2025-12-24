namespace Standup.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Dynamics 365 CRM integration.
/// </summary>
public class DynamicsCrmOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "DynamicsCrm";

    /// <summary>
    /// Gets or sets the CRM instance URL (e.g., ***REMOVED***).
    /// </summary>
    public string InstanceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD application (client) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether CRM integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
}
