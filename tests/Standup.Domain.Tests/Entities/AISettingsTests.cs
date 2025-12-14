using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class AISettingsTests
{
    [Fact]
    public void AISettings_HasDefaultValues()
    {
        // Act
        var settings = new AISettings();

        // Assert
        settings.Endpoint.Should().BeEmpty();
        settings.DeploymentName.Should().Be("gpt-4o");
        settings.ApiKey.Should().BeNull();
        settings.UseAzureIdentity.Should().BeTrue();
    }

    [Fact]
    public void AISettings_CanSetCustomValues()
    {
        // Act
        var settings = new AISettings(
            Endpoint: "https://my-ai-foundry.openai.azure.com",
            DeploymentName: "gpt-4o-mini",
            ApiKey: "sk-test-key-12345",
            UseAzureIdentity: false);

        // Assert
        settings.Endpoint.Should().Be("https://my-ai-foundry.openai.azure.com");
        settings.DeploymentName.Should().Be("gpt-4o-mini");
        settings.ApiKey.Should().Be("sk-test-key-12345");
        settings.UseAzureIdentity.Should().BeFalse();
    }

    [Fact]
    public void AISettings_IsRecord_SupportsEquality()
    {
        // Arrange
        var settings1 = new AISettings(
            Endpoint: "https://example.openai.azure.com",
            DeploymentName: "gpt-4o",
            ApiKey: null,
            UseAzureIdentity: true);

        var settings2 = new AISettings(
            Endpoint: "https://example.openai.azure.com",
            DeploymentName: "gpt-4o",
            ApiKey: null,
            UseAzureIdentity: true);

        // Assert
        settings1.Should().Be(settings2);
        (settings1 == settings2).Should().BeTrue();
    }

    [Fact]
    public void AISettings_DifferentValues_AreNotEqual()
    {
        // Arrange
        var settings1 = new AISettings(
            Endpoint: "https://endpoint1.openai.azure.com",
            DeploymentName: "gpt-4o");

        var settings2 = new AISettings(
            Endpoint: "https://endpoint2.openai.azure.com",
            DeploymentName: "gpt-4o");

        // Assert
        settings1.Should().NotBe(settings2);
        (settings1 != settings2).Should().BeTrue();
    }

    [Fact]
    public void AISettings_SupportsDeconstruction()
    {
        // Arrange
        var settings = new AISettings(
            Endpoint: "https://test.openai.azure.com",
            DeploymentName: "gpt-4",
            ApiKey: "test-key",
            UseAzureIdentity: false);

        // Act
        var (endpoint, deployment, apiKey, useAzureIdentity) = settings;

        // Assert
        endpoint.Should().Be("https://test.openai.azure.com");
        deployment.Should().Be("gpt-4");
        apiKey.Should().Be("test-key");
        useAzureIdentity.Should().BeFalse();
    }

    [Fact]
    public void AISettings_WithDefaultDeployment_UsesGpt4o()
    {
        // Act
        var settings = new AISettings(
            Endpoint: "https://example.openai.azure.com");

        // Assert
        settings.DeploymentName.Should().Be("gpt-4o");
    }

    [Fact]
    public void AISettings_WithAzureIdentity_HasNoApiKey()
    {
        // Act
        var settings = new AISettings(
            Endpoint: "https://example.openai.azure.com",
            UseAzureIdentity: true);

        // Assert
        settings.UseAzureIdentity.Should().BeTrue();
        settings.ApiKey.Should().BeNull();
    }

    [Fact]
    public void AISettings_WithApiKey_CanDisableAzureIdentity()
    {
        // Act
        var settings = new AISettings(
            Endpoint: "https://example.openai.azure.com",
            ApiKey: "sk-custom-key",
            UseAzureIdentity: false);

        // Assert
        settings.UseAzureIdentity.Should().BeFalse();
        settings.ApiKey.Should().Be("sk-custom-key");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4")]
    [InlineData("gpt-35-turbo")]
    public void AISettings_SupportsDifferentDeploymentNames(string deploymentName)
    {
        // Act
        var settings = new AISettings(
            Endpoint: "https://example.openai.azure.com",
            DeploymentName: deploymentName);

        // Assert
        settings.DeploymentName.Should().Be(deploymentName);
    }

    [Fact]
    public void AISettings_SupportsWithExpression()
    {
        // Arrange
        var original = new AISettings(
            Endpoint: "https://original.openai.azure.com",
            DeploymentName: "gpt-4o",
            UseAzureIdentity: true);

        // Act
        var updated = original with { DeploymentName = "gpt-4o-mini" };

        // Assert
        updated.Endpoint.Should().Be(original.Endpoint);
        updated.DeploymentName.Should().Be("gpt-4o-mini");
        updated.UseAzureIdentity.Should().Be(original.UseAzureIdentity);
        original.DeploymentName.Should().Be("gpt-4o"); // Original unchanged
    }
}
