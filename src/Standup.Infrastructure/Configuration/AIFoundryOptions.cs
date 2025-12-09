namespace Standup.Infrastructure.Configuration;

public class AIFoundryOptions
{
    public const string SectionName = "AIFoundry";

    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o";
    public string? ApiKey { get; set; }
    public bool UseAzureIdentity { get; set; } = true;
}
