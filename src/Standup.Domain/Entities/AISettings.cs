namespace Standup.Domain.Entities;

public record AISettings(
    string Endpoint = "",
    string DeploymentName = "gpt-4o",
    string? ApiKey = null,
    bool UseAzureIdentity = true);
