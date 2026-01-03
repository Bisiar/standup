// <copyright file="IntegrationSettingsTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="IntegrationSettings"/> entity.
/// </summary>
public sealed class IntegrationSettingsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationSettingsTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public IntegrationSettingsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NewIntegrationSettings_HasDefaultValues()
    {
        // Act
        var settings = new IntegrationSettings();

        // Assert
        settings.Id.Should().BeEmpty();
        settings.Type.Should().Be(default(IntegrationType));
        settings.IsEnabled.Should().BeFalse();
        settings.Configuration.Should().NotBeNull().And.BeEmpty();
        settings.EncryptedCredentials.Should().NotBeNull().And.BeEmpty();
        settings.ClientCodeMappings.Should().NotBeNull().And.BeEmpty();
        settings.CreatedAt.Should().Be(default);
        settings.UpdatedAt.Should().Be(default);
        settings.LastValidatedAt.Should().BeNull();
        settings.LastValidationSuccess.Should().BeNull();
        settings.LastValidationError.Should().BeNull();
    }

    [Fact]
    public void IntegrationSettings_CanSetAllProperties()
    {
        // Arrange
        var settings = new IntegrationSettings();
        var now = DateTimeOffset.UtcNow;

        // Act
        settings.Id = "integration-123";
        settings.Type = IntegrationType.Office365Email;
        settings.IsEnabled = true;
        settings.Configuration["BaseUrl"] = "https://api.example.com";
        settings.Configuration["Timeout"] = "30";
        settings.EncryptedCredentials["ApiKey"] = "encrypted-key-value";
        settings.ClientCodeMappings["ACME"] = "guid-12345";
        settings.CreatedAt = now.AddDays(-7);
        settings.UpdatedAt = now;
        settings.LastValidatedAt = now.AddHours(-1);
        settings.LastValidationSuccess = true;
        settings.LastValidationError = null;

        // Assert
        settings.Id.Should().Be("integration-123");
        settings.Type.Should().Be(IntegrationType.Office365Email);
        settings.IsEnabled.Should().BeTrue();
        settings.Configuration.Should().HaveCount(2);
        settings.Configuration["BaseUrl"].Should().Be("https://api.example.com");
        settings.EncryptedCredentials.Should().HaveCount(1);
        settings.ClientCodeMappings.Should().HaveCount(1);
        settings.LastValidationSuccess.Should().BeTrue();
        _output.WriteLine($"Integration {settings.Id}: {settings.Type}, Enabled: {settings.IsEnabled}");
    }

    [Fact]
    public void IntegrationSettings_CanAddMultipleConfiguration()
    {
        // Arrange
        var settings = new IntegrationSettings { Id = "test", Type = IntegrationType.Office365Email };

        // Act
        settings.Configuration["Setting1"] = "Value1";
        settings.Configuration["Setting2"] = "Value2";
        settings.Configuration["Setting3"] = "Value3";

        // Assert
        settings.Configuration.Should().HaveCount(3);
        settings.Configuration.Keys.Should().Contain(new[] { "Setting1", "Setting2", "Setting3" });
    }

    [Fact]
    public void IntegrationSettings_CanAddMultipleCredentials()
    {
        // Arrange
        var settings = new IntegrationSettings { Id = "test", Type = IntegrationType.Office365Email };

        // Act
        settings.EncryptedCredentials["ClientId"] = "encrypted-client-id";
        settings.EncryptedCredentials["ClientSecret"] = "encrypted-client-secret";
        settings.EncryptedCredentials["TenantId"] = "encrypted-tenant-id";

        // Assert
        settings.EncryptedCredentials.Should().HaveCount(3);
        _output.WriteLine($"Stored {settings.EncryptedCredentials.Count} encrypted credentials");
    }

    [Fact]
    public void IntegrationSettings_CanAddClientCodeMappings()
    {
        // Arrange
        var settings = new IntegrationSettings { Id = "test", Type = IntegrationType.Office365Email };

        // Act
        settings.ClientCodeMappings["ACME"] = "crm-project-guid-1";
        settings.ClientCodeMappings["XYZ"] = "crm-project-guid-2";
        settings.ClientCodeMappings["ABC"] = "crm-project-guid-3";

        // Assert
        settings.ClientCodeMappings.Should().HaveCount(3);
        settings.ClientCodeMappings["ACME"].Should().Be("crm-project-guid-1");
        _output.WriteLine($"Mapped {settings.ClientCodeMappings.Count} client codes");
    }

    [Fact]
    public void IntegrationSettings_ValidationStatus_CanBeTracked()
    {
        // Arrange
        var settings = new IntegrationSettings
        {
            Id = "test",
            Type = IntegrationType.Office365Email,
            IsEnabled = true
        };
        var validationTime = DateTimeOffset.UtcNow;

        // Act - Simulate successful validation
        settings.LastValidatedAt = validationTime;
        settings.LastValidationSuccess = true;
        settings.LastValidationError = null;

        // Assert
        settings.LastValidatedAt.Should().Be(validationTime);
        settings.LastValidationSuccess.Should().BeTrue();
        settings.LastValidationError.Should().BeNull();
    }

    [Fact]
    public void IntegrationSettings_ValidationError_CanBeTracked()
    {
        // Arrange
        var settings = new IntegrationSettings
        {
            Id = "test",
            Type = IntegrationType.Office365Email,
            IsEnabled = true
        };
        var validationTime = DateTimeOffset.UtcNow;

        // Act - Simulate failed validation
        settings.LastValidatedAt = validationTime;
        settings.LastValidationSuccess = false;
        settings.LastValidationError = "Connection refused";

        // Assert
        settings.LastValidatedAt.Should().Be(validationTime);
        settings.LastValidationSuccess.Should().BeFalse();
        settings.LastValidationError.Should().Be("Connection refused");
        _output.WriteLine($"Validation failed: {settings.LastValidationError}");
    }
}
