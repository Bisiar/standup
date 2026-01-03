// <copyright file="IntegrationServiceTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using NSubstitute;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for the <see cref="IntegrationService"/> class.
/// </summary>
public sealed class IntegrationServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly IEncryptionService _encryptionService;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly IntegrationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationServiceTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public IntegrationServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _encryptionService = Substitute.For<IEncryptionService>();
        _integrationRepository = Substitute.For<IIntegrationRepository>();
        _service = new IntegrationService(_encryptionService, _integrationRepository);
    }

    #region RegisterIntegration Tests

    [Fact]
    public void RegisterIntegration_StoresIntegration()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();

        // Act
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);

        // Assert
        var result = _service.GetIntegration(IntegrationType.Office365Email);
        result.Should().Be(integration);
        _output.WriteLine("Integration registered successfully");
    }

    [Fact]
    public void RegisterIntegration_OverwritesPreviousIntegration()
    {
        // Arrange
        var integration1 = Substitute.For<IIntegration>();
        var integration2 = Substitute.For<IIntegration>();

        // Act
        _service.RegisterIntegration(IntegrationType.Office365Email, integration1);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration2);

        // Assert
        var result = _service.GetIntegration(IntegrationType.Office365Email);
        result.Should().Be(integration2);
    }

    #endregion

    #region GetIntegration Tests

    [Fact]
    public void GetIntegration_WhenNotRegistered_ReturnsNull()
    {
        // Act
        var result = _service.GetIntegration(IntegrationType.Office365Email);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllIntegrations Tests

    [Fact]
    public void GetAllIntegrations_ReturnsAllRegistered()
    {
        // Arrange
        var emailIntegration = Substitute.For<IIntegration>();
        _service.RegisterIntegration(IntegrationType.Office365Email, emailIntegration);

        // Act
        var result = _service.GetAllIntegrations().ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be(IntegrationType.Office365Email);
        result[0].Value.Should().Be(emailIntegration);
    }

    [Fact]
    public void GetAllIntegrations_WhenEmpty_ReturnsEmptyCollection()
    {
        // Act
        var result = _service.GetAllIntegrations().ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ValidateIntegrationAsync Tests

    [Fact]
    public async Task ValidateIntegrationAsync_WhenIntegrationNotRegistered_ReturnsFalse()
    {
        // Act
        var result = await _service.ValidateIntegrationAsync(IntegrationType.Office365Email);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateIntegrationAsync_WhenIntegrationValid_ReturnsTrue()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.ValidateConnectionAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);

        // Act
        var result = await _service.ValidateIntegrationAsync(IntegrationType.Office365Email);

        // Assert
        result.Should().BeTrue();
        await integration.Received(1).ValidateConnectionAsync(Arg.Any<CancellationToken>());
        _output.WriteLine("Integration validated successfully");
    }

    [Fact]
    public async Task ValidateIntegrationAsync_WhenIntegrationInvalid_ReturnsFalse()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.ValidateConnectionAsync(Arg.Any<CancellationToken>()).Returns(false);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);

        // Act
        var result = await _service.ValidateIntegrationAsync(IntegrationType.Office365Email);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region FetchIntegrationDataAsync Tests

    [Fact]
    public async Task FetchIntegrationDataAsync_WhenIntegrationNotRegistered_ReturnsNull()
    {
        // Act
        var result = await _service.FetchIntegrationDataAsync(
            IntegrationType.Office365Email,
            "ACME",
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchIntegrationDataAsync_WhenIntegrationDisabled_ReturnsNull()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.IsEnabled.Returns(false);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);

        // Act
        var result = await _service.FetchIntegrationDataAsync(
            IntegrationType.Office365Email,
            "ACME",
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeNull();
        await integration.DidNotReceive().FetchDataAsync(
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchIntegrationDataAsync_WhenIntegrationEnabled_FetchesData()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.IsEnabled.Returns(true);
        var expectedData = new IntegrationData();
        integration.FetchDataAsync(
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>()).Returns(expectedData);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);

        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        // Act
        var result = await _service.FetchIntegrationDataAsync(IntegrationType.Office365Email, "ACME", since, until);

        // Assert
        result.Should().Be(expectedData);
        await integration.Received(1).FetchDataAsync("ACME", since, until, Arg.Any<CancellationToken>());
        _output.WriteLine("Data fetched successfully");
    }

    #endregion

    #region EncryptCredentialAsync Tests

    [Fact]
    public async Task EncryptCredentialAsync_CallsEncryptionService()
    {
        // Arrange
        _encryptionService.EncryptAsync("plain-text").Returns("encrypted-text");

        // Act
        var result = await _service.EncryptCredentialAsync("plain-text");

        // Assert
        result.Should().Be("encrypted-text");
        await _encryptionService.Received(1).EncryptAsync("plain-text");
    }

    #endregion

    #region DecryptCredentialAsync Tests

    [Fact]
    public async Task DecryptCredentialAsync_CallsEncryptionService()
    {
        // Arrange
        _encryptionService.DecryptAsync("encrypted-text").Returns("plain-text");

        // Act
        var result = await _service.DecryptCredentialAsync("encrypted-text");

        // Assert
        result.Should().Be("plain-text");
        await _encryptionService.Received(1).DecryptAsync("encrypted-text");
    }

    #endregion

    #region ApplyIntegrationSettingsAsync Tests

    [Fact]
    public async Task ApplyIntegrationSettingsAsync_WhenIntegrationNotRegistered_ThrowsException()
    {
        // Arrange
        var settings = new IntegrationSettings { Type = IntegrationType.Office365Email, IsEnabled = true };

        // Act
        var act = () => _service.ApplyIntegrationSettingsAsync(settings);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Office365Email*not registered*");
    }

    [Fact]
    public async Task ApplyIntegrationSettingsAsync_SetsIsEnabled()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.ValidateConnectionAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);
        var settings = new IntegrationSettings { Type = IntegrationType.Office365Email, IsEnabled = true };

        // Act
        await _service.ApplyIntegrationSettingsAsync(settings);

        // Assert
        integration.IsEnabled = true;
        _output.WriteLine("Settings applied successfully");
    }

    [Fact]
    public async Task ApplyIntegrationSettingsAsync_WhenEnabled_ValidatesConnection()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.ValidateConnectionAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);
        var settings = new IntegrationSettings { Type = IntegrationType.Office365Email, IsEnabled = true };

        // Act
        await _service.ApplyIntegrationSettingsAsync(settings);

        // Assert
        await integration.Received(1).ValidateConnectionAsync(Arg.Any<CancellationToken>());
        settings.LastValidatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        settings.LastValidationSuccess.Should().BeTrue();
        settings.LastValidationError.Should().BeNull();
    }

    [Fact]
    public async Task ApplyIntegrationSettingsAsync_WhenValidationFails_SetsError()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.ValidateConnectionAsync(Arg.Any<CancellationToken>()).Returns(false);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);
        var settings = new IntegrationSettings { Type = IntegrationType.Office365Email, IsEnabled = true };

        // Act
        await _service.ApplyIntegrationSettingsAsync(settings);

        // Assert
        settings.LastValidationSuccess.Should().BeFalse();
        settings.LastValidationError.Should().Be("Connection validation failed.");
    }

    [Fact]
    public async Task ApplyIntegrationSettingsAsync_UpdatesTimestamp()
    {
        // Arrange
        var integration = Substitute.For<IIntegration>();
        integration.ValidateConnectionAsync(Arg.Any<CancellationToken>()).Returns(true);
        _service.RegisterIntegration(IntegrationType.Office365Email, integration);
        var settings = new IntegrationSettings
        {
            Type = IntegrationType.Office365Email,
            IsEnabled = true,
            UpdatedAt = DateTimeOffset.MinValue
        };

        // Act
        await _service.ApplyIntegrationSettingsAsync(settings);

        // Assert
        settings.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetAllSettingsAsync Tests

    [Fact]
    public async Task GetAllSettingsAsync_DelegatesToRepository()
    {
        // Arrange
        var expectedSettings = new List<IntegrationSettings>
        {
            new IntegrationSettings { Id = "1", Type = IntegrationType.Office365Email }
        };
        _integrationRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expectedSettings);

        // Act
        var result = await _service.GetAllSettingsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedSettings);
        await _integrationRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSettingsByTypeAsync Tests

    [Fact]
    public async Task GetSettingsByTypeAsync_DelegatesToRepository()
    {
        // Arrange
        var expectedSettings = new IntegrationSettings { Id = "1", Type = IntegrationType.Office365Email };
        _integrationRepository.GetByTypeAsync(IntegrationType.Office365Email, Arg.Any<CancellationToken>())
            .Returns(expectedSettings);

        // Act
        var result = await _service.GetSettingsByTypeAsync(IntegrationType.Office365Email);

        // Assert
        result.Should().Be(expectedSettings);
        await _integrationRepository.Received(1).GetByTypeAsync(IntegrationType.Office365Email, Arg.Any<CancellationToken>());
    }

    #endregion

    #region SaveSettingsAsync Tests

    [Fact]
    public async Task SaveSettingsAsync_WithNewSettings_AssignsIdAndCreatesTimestamp()
    {
        // Arrange
        var settings = new IntegrationSettings { Type = IntegrationType.Office365Email };
        _integrationRepository.AddAsync(Arg.Any<IntegrationSettings>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<IntegrationSettings>());

        // Act
        var result = await _service.SaveSettingsAsync(settings);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        await _integrationRepository.Received(1).AddAsync(settings, Arg.Any<CancellationToken>());
        _output.WriteLine($"New settings created with ID: {result.Id}");
    }

    [Fact]
    public async Task SaveSettingsAsync_WithExistingSettings_UpdatesTimestamp()
    {
        // Arrange
        var settings = new IntegrationSettings
        {
            Id = "existing-id",
            Type = IntegrationType.Office365Email,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };
        _integrationRepository.UpdateAsync(Arg.Any<IntegrationSettings>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<IntegrationSettings>());

        // Act
        var result = await _service.SaveSettingsAsync(settings);

        // Assert
        result.Id.Should().Be("existing-id");
        result.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        await _integrationRepository.Received(1).UpdateAsync(settings, Arg.Any<CancellationToken>());
        await _integrationRepository.DidNotReceive().AddAsync(Arg.Any<IntegrationSettings>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteSettingsAsync Tests

    [Fact]
    public async Task DeleteSettingsAsync_DelegatesToRepository()
    {
        // Act
        await _service.DeleteSettingsAsync("settings-id");

        // Assert
        await _integrationRepository.Received(1).DeleteAsync("settings-id", Arg.Any<CancellationToken>());
    }

    #endregion
}
