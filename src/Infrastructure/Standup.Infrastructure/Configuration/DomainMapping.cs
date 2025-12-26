namespace Standup.Infrastructure.Configuration;

/// <summary>
/// Mapping between email domain and client code.
/// </summary>
public class DomainMapping
{
    /// <summary>
    /// Gets or sets the email domain (e.g., elgp.com).
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client code (e.g., ELGP).
    /// </summary>
    public string ClientCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to match specific email addresses instead of domains.
    /// </summary>
    public bool IsEmailAddress { get; set; } = false;
}
