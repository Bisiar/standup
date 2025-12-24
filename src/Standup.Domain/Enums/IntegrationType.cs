namespace Standup.Domain.Enums;

/// <summary>
/// Represents the type of external integration.
/// </summary>
public enum IntegrationType
{
    /// <summary>
    /// Dynamics 365 CRM project integration.
    /// </summary>
    CRM,

    /// <summary>
    /// Office 365 email integration.
    /// </summary>
    Office365Email,

    /// <summary>
    /// Office 365 calendar integration.
    /// </summary>
    Office365Calendar,

    /// <summary>
    /// Microsoft Teams integration.
    /// </summary>
    MicrosoftTeams
}
