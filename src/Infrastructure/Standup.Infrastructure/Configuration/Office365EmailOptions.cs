namespace Standup.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Office 365 email integration.
/// </summary>
public class Office365EmailOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Office365Email";

    /// <summary>
    /// Gets or sets the Azure AD tenant ID.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD application (client) ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the redirect URI for OAuth2 callback.
    /// </summary>
    public string RedirectUri { get; set; } = "standbot://auth-callback";

    /// <summary>
    /// Gets or sets the Microsoft Graph API scopes required.
    /// </summary>
    public string[] Scopes { get; set; } = { "Mail.Read", "User.Read" };

    /// <summary>
    /// Gets or sets a value indicating whether email integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the domain-to-client-code mappings.
    /// Format: { "domain": "elgp.com", "clientCode": "ELGP" }.
    /// </summary>
    public List<DomainMapping> DomainMappings { get; set; } = new();
}
