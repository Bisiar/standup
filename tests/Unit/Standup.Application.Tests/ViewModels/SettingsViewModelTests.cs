// <copyright file="SettingsViewModelTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using NSubstitute;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="SettingsViewModel"/> methods and behavior.
/// Uses NSubstitute for mocking dependencies.
/// </summary>
public sealed class SettingsViewModelTests
{
    private readonly IProjectService _projectService;
    private readonly ILocalStandupService _localStandupService;
    private readonly ICrmTenantConfigService _crmTenantConfigService;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _projectService = Substitute.For<IProjectService>();
        _localStandupService = Substitute.For<ILocalStandupService>();
        _crmTenantConfigService = Substitute.For<ICrmTenantConfigService>();
        _crmTenantConfigService.GetAllAsync().Returns(new List<CrmTenantConfig>());
        _viewModel = new SettingsViewModel(_projectService, _localStandupService, _crmTenantConfigService);
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_HasDefaultValues()
    {
        // Assert
        _viewModel.SelectedSettingsTab.Should().Be(0);
        _viewModel.SelectedTheme.Should().Be("System");
        _viewModel.LastCacheCleared.Should().Be("Never");
        _viewModel.Projects.Should().BeEmpty();
        _viewModel.CurrentProject.Should().BeNull();
        _viewModel.UserId.Should().BeEmpty();
        _viewModel.TenantId.Should().BeEmpty();
        _viewModel.ApiEndpoint.Should().BeEmpty();
        _viewModel.UseLocalGeneration.Should().BeTrue();
        _viewModel.SourceType.Should().Be(SourceType.AzureDevOps);
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.IsTestingConnection.Should().BeFalse();
        _viewModel.StatusMessage.Should().BeEmpty();
    }

    [Fact]
    public void InitialState_AISettingsAreEmpty()
    {
        // Assert
        _viewModel.AiEndpoint.Should().BeEmpty();
        _viewModel.AiDeploymentName.Should().BeEmpty();
        _viewModel.AiApiKey.Should().BeEmpty();
        _viewModel.AiUseAzureIdentity.Should().BeFalse();
        _viewModel.IsValidatingAI.Should().BeFalse();
        _viewModel.AiValidationResult.Should().BeEmpty();
        _viewModel.AiValidationSuccess.Should().BeFalse();
    }

    [Fact]
    public void InitialState_CRMSettingsAreEmpty()
    {
        // Assert
        _viewModel.CrmEnabled.Should().BeFalse();
        _viewModel.CrmInstanceUrl.Should().BeEmpty();
        _viewModel.CrmTenantId.Should().BeEmpty();
        _viewModel.CrmClientId.Should().BeEmpty();
        _viewModel.CrmClientSecret.Should().BeEmpty();
        _viewModel.IsTestingCrmConnection.Should().BeFalse();
        _viewModel.CrmValidationResult.Should().BeEmpty();
        _viewModel.CrmValidationSuccess.Should().BeFalse();
    }

    [Fact]
    public void InitialState_CacheStatsAreZero()
    {
        // Assert
        _viewModel.CacheEntryCount.Should().Be(0);
        _viewModel.CacheSizeDisplay.Should().Be("0 KB");
    }

    #endregion

    #region AuthMethodDisplay Tests

    [Fact]
    public void AuthMethodDisplay_WithNoApiKey_ReturnsAzureIdentity()
    {
        // Arrange
        _viewModel.AiApiKey = string.Empty;

        // Assert
        _viewModel.AuthMethodDisplay.Should().Be("Azure Identity (Entra ID)");
    }

    [Fact]
    public void AuthMethodDisplay_WithApiKey_ReturnsApiKey()
    {
        // Arrange
        _viewModel.AiApiKey = "some-api-key";

        // Assert
        _viewModel.AuthMethodDisplay.Should().Be("API Key");
    }

    [Fact]
    public void AuthMethodColor_WithNoApiKey_ReturnsGreen()
    {
        // Arrange
        _viewModel.AiApiKey = string.Empty;

        // Assert
        _viewModel.AuthMethodColor.Should().Be("#10B981");
    }

    [Fact]
    public void AuthMethodColor_WithApiKey_ReturnsBlue()
    {
        // Arrange
        _viewModel.AiApiKey = "some-api-key";

        // Assert
        _viewModel.AuthMethodColor.Should().Be("#3B82F6");
    }

    #endregion

    #region SourceTypes Tests

    [Fact]
    public void SourceTypes_ContainsExpectedTypes()
    {
        // Assert
        _viewModel.SourceTypes.Should().Contain(SourceType.AzureDevOps);
        _viewModel.SourceTypes.Should().Contain(SourceType.GitHub);
        _viewModel.SourceTypes.Should().HaveCount(2);
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        _projectService.GetProjectsAsync().Returns(new List<ProjectInstance>());
        _projectService.GetCurrentProjectAsync().Returns((ProjectInstance?)null);

        // Act
        var task = _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert - eventually IsLoading becomes false
        await task;
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_LoadsProjectsFromService()
    {
        // Arrange
        var projects = new List<ProjectInstance>
        {
            new ProjectInstance(
                Id: "project-1",
                Name: "Project One",
                TenantName: "Tenant1",
                ApiEndpoint: "https://api.example.com",
                SourceType: SourceType.AzureDevOps,
                SourceOrganization: "org1",
                SourceProject: "proj1",
                SourceRepository: "repo1",
                SourcePat: null,
                AuthorIdentifier: null,
                UseLocalGeneration: true),
            new ProjectInstance(
                Id: "project-2",
                Name: "Project Two",
                TenantName: "Tenant2",
                ApiEndpoint: "https://api2.example.com",
                SourceType: SourceType.GitHub,
                SourceOrganization: "org2",
                SourceProject: null,
                SourceRepository: "repo2",
                SourcePat: null,
                AuthorIdentifier: null,
                UseLocalGeneration: false)
        };
        _projectService.GetProjectsAsync().Returns(projects);
        _projectService.GetCurrentProjectAsync().Returns(projects[0]);

        // Act
        await _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Projects.Should().HaveCount(2);
        _viewModel.CurrentProject.Should().NotBeNull();
        _viewModel.CurrentProject!.Id.Should().Be("project-1");
    }

    [Fact]
    public async Task LoadAsync_SelectsFirstProjectWhenCurrentNotFound()
    {
        // Arrange
        var projects = new List<ProjectInstance>
        {
            new ProjectInstance(
                Id: "project-1",
                Name: "Project One",
                TenantName: "Tenant1",
                ApiEndpoint: "https://api.example.com",
                SourceType: SourceType.AzureDevOps,
                SourceOrganization: "org1",
                SourceProject: "proj1",
                SourceRepository: "repo1",
                SourcePat: null,
                AuthorIdentifier: null,
                UseLocalGeneration: true)
        };
        _projectService.GetProjectsAsync().Returns(projects);
        _projectService.GetCurrentProjectAsync().Returns((ProjectInstance?)null);

        // Act
        await _viewModel.LoadCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CurrentProject.Should().Be(projects[0]);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithNoCurrentProject_DoesNothing()
    {
        // Arrange
        _viewModel.CurrentProject.Should().BeNull();

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        await _projectService.DidNotReceive().UpdateProjectAsync(Arg.Any<ProjectInstance>());
    }

    [Fact]
    public async Task SaveAsync_UpdatesProjectWithNewValues()
    {
        // Arrange
        var project = new ProjectInstance(
            Id: "project-1",
            Name: "Test Project",
            TenantName: "Tenant",
            ApiEndpoint: "https://old.api.com",
            SourceType: SourceType.AzureDevOps,
            SourceOrganization: "old-org",
            SourceProject: "old-project",
            SourceRepository: "old-repo",
            SourcePat: null,
            AuthorIdentifier: null,
            UseLocalGeneration: true);

        _projectService.GetProjectsAsync().Returns(new List<ProjectInstance> { project });
        _projectService.GetCurrentProjectAsync().Returns(project);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        // Modify values
        _viewModel.UserId = "new-user-id";
        _viewModel.TenantId = "new-tenant-id";
        _viewModel.ApiEndpoint = "https://new.api.com";
        _viewModel.SourceOrganization = "new-org";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        await _projectService.Received(1).UpdateProjectAsync(
            Arg.Is<ProjectInstance>(p =>
                p.UserId == "new-user-id" &&
                p.TenantId == "new-tenant-id" &&
                p.ApiEndpoint == "https://new.api.com" &&
                p.SourceOrganization == "new-org"));

        _viewModel.StatusMessage.Should().Contain("saved");
    }

    #endregion

    #region SaveAISettings Tests

    [Fact]
    public void SaveAISettings_InvokesEventAndUpdatesAzureIdentityFlag()
    {
        // Arrange
        bool eventInvoked = false;
        string? savedEndpoint = null;
        string? savedDeployment = null;
        string? savedApiKey = null;

        _viewModel.SaveAISettingsRequested += (endpoint, deployment, apiKey) =>
        {
            eventInvoked = true;
            savedEndpoint = endpoint;
            savedDeployment = deployment;
            savedApiKey = apiKey;
        };

        _viewModel.AiEndpoint = "https://ai.azure.com";
        _viewModel.AiDeploymentName = "gpt-4";
        _viewModel.AiApiKey = string.Empty; // No API key = Azure Identity

        // Act
        _viewModel.SaveAISettingsCommand.Execute(null);

        // Assert
        eventInvoked.Should().BeTrue();
        savedEndpoint.Should().Be("https://ai.azure.com");
        savedDeployment.Should().Be("gpt-4");
        savedApiKey.Should().BeEmpty();
        _viewModel.AiUseAzureIdentity.Should().BeTrue();
        _viewModel.StatusMessage.Should().Contain("AI settings saved");
    }

    [Fact]
    public void SaveAISettings_WithApiKey_SetsAzureIdentityToFalse()
    {
        // Arrange
        _viewModel.SaveAISettingsRequested += (_, _, _) => { };
        _viewModel.AiApiKey = "my-secret-key";

        // Act
        _viewModel.SaveAISettingsCommand.Execute(null);

        // Assert
        _viewModel.AiUseAzureIdentity.Should().BeFalse();
    }

    #endregion

    #region ValidateAIConnection Tests

    [Fact]
    public async Task ValidateAIConnectionAsync_WithoutEndpoint_SetsError()
    {
        // Arrange
        _viewModel.AiEndpoint = string.Empty;
        _viewModel.AiDeploymentName = "gpt-4";

        // Act
        await _viewModel.ValidateAIConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.AiValidationResult.Should().Contain("Please enter endpoint");
        _viewModel.AiValidationSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAIConnectionAsync_WithoutDeploymentName_SetsError()
    {
        // Arrange
        _viewModel.AiEndpoint = "https://ai.azure.com";
        _viewModel.AiDeploymentName = string.Empty;

        // Act
        await _viewModel.ValidateAIConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.AiValidationResult.Should().Contain("Please enter endpoint and deployment");
        _viewModel.AiValidationSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAIConnectionAsync_WithValidSettings_CallsValidation()
    {
        // Arrange
        _viewModel.AiEndpoint = "https://ai.azure.com";
        _viewModel.AiDeploymentName = "gpt-4";
        _viewModel.ValidateAIConnectionRequested += () => Task.FromResult((true, "Connection successful!"));

        // Act
        await _viewModel.ValidateAIConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.AiValidationSuccess.Should().BeTrue();
        _viewModel.AiValidationResult.Should().Be("Connection successful!");
    }

    [Fact]
    public async Task ValidateAIConnectionAsync_WithFailedValidation_SetsErrorResult()
    {
        // Arrange
        _viewModel.AiEndpoint = "https://ai.azure.com";
        _viewModel.AiDeploymentName = "gpt-4";
        _viewModel.ValidateAIConnectionRequested += () => Task.FromResult((false, "Authentication failed"));

        // Act
        await _viewModel.ValidateAIConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.AiValidationSuccess.Should().BeFalse();
        _viewModel.AiValidationResult.Should().Be("Authentication failed");
    }

    #endregion

    #region ClearCache Tests

    [Fact]
    public async Task ClearCacheAsync_WithoutCacheService_SetsErrorMessage()
    {
        // Arrange - no cache service set

        // Act
        await _viewModel.ClearCacheCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Cache service not available");
    }

    [Fact]
    public async Task ClearCacheAsync_WithCacheService_ClearsAndRefreshes()
    {
        // Arrange
        var cacheService = Substitute.For<IReportCacheService>();
        cacheService.GetStats().Returns(new CacheStats(0, 0, 0, 0));
        _viewModel.SetCacheService(cacheService);

        // Act
        await _viewModel.ClearCacheCommand.ExecuteAsync(null);

        // Assert
        await cacheService.Received(1).InvalidateAllAsync();
        _viewModel.StatusMessage.Should().Contain("Cache cleared successfully");
    }

    #endregion

    #region TestConnection Tests

    [Fact]
    public async Task TestConnectionAsync_WithMissingFields_SetsErrorMessage()
    {
        // Arrange
        _viewModel.SourceOrganization = string.Empty;

        // Act
        await _viewModel.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Please fill in all source fields");
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidFields_CallsService()
    {
        // Arrange
        _viewModel.SourceOrganization = "my-org";
        _viewModel.SourceProject = "my-project";
        _viewModel.SourceRepository = "my-repo";
        _viewModel.SourcePat = "my-pat";
        _viewModel.SourceType = SourceType.AzureDevOps;

        _localStandupService.ValidateConnectionAsync(
            Arg.Any<SourceType>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _viewModel.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        await _localStandupService.Received(1).ValidateConnectionAsync(
            SourceType.AzureDevOps,
            "my-org",
            "my-project",
            "my-repo",
            "my-pat",
            Arg.Any<CancellationToken>());
        _viewModel.StatusMessage.Should().Contain("successful");
    }

    [Fact]
    public async Task TestConnectionAsync_WithInvalidConnection_SetsFailureMessage()
    {
        // Arrange
        _viewModel.SourceOrganization = "my-org";
        _viewModel.SourceProject = "my-project";
        _viewModel.SourceRepository = "my-repo";
        _viewModel.SourcePat = "my-pat";

        _localStandupService.ValidateConnectionAsync(
            Arg.Any<SourceType>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()).Returns(false);

        // Act
        await _viewModel.TestConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("failed");
    }

    #endregion

    #region ValidateCrmConnection Tests

    [Fact]
    public async Task ValidateCrmConnectionAsync_WhenDisabled_SetsMessage()
    {
        // Arrange
        _viewModel.CrmEnabled = false;

        // Act
        await _viewModel.ValidateCrmConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CrmValidationResult.Should().Contain("disabled");
        _viewModel.CrmValidationSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCrmConnectionAsync_WithMissingInstanceUrl_SetsError()
    {
        // Arrange
        _viewModel.CrmEnabled = true;
        _viewModel.CrmInstanceUrl = string.Empty; // Missing

        // Act
        await _viewModel.ValidateCrmConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CrmValidationResult.Should().Contain("Please enter the CRM Instance URL");
        _viewModel.CrmValidationSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCrmConnectionAsync_WithValidFields_CallsValidation()
    {
        // Arrange
        _viewModel.CrmEnabled = true;
        _viewModel.CrmInstanceUrl = "https://org.crm.dynamics.com";
        _viewModel.CrmTenantId = "tenant-id";
        _viewModel.CrmClientId = "client-id";
        _viewModel.CrmClientSecret = "client-secret";
        _viewModel.ValidateCrmConnectionRequested += () => Task.FromResult((true, "CRM connected!"));

        // Act
        await _viewModel.ValidateCrmConnectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.CrmValidationSuccess.Should().BeTrue();
        _viewModel.CrmValidationResult.Should().Be("CRM connected!");
    }

    #endregion

    #region Theme Selection Tests

    [Fact]
    public void SelectLightTheme_SetsThemeAndMessage()
    {
        // Act
        _viewModel.SelectLightThemeCommand.Execute(null);

        // Assert
        _viewModel.SelectedTheme.Should().Be("Light");
        _viewModel.StatusMessage.Should().Contain("Light theme selected");
    }

    [Fact]
    public void SelectDarkTheme_SetsThemeAndMessage()
    {
        // Act
        _viewModel.SelectDarkThemeCommand.Execute(null);

        // Assert
        _viewModel.SelectedTheme.Should().Be("Dark");
        _viewModel.StatusMessage.Should().Contain("Dark theme selected");
    }

    [Fact]
    public void SelectSystemTheme_SetsThemeAndMessage()
    {
        // Arrange - start with a different theme
        _viewModel.SelectedTheme = "Light";

        // Act
        _viewModel.SelectSystemThemeCommand.Execute(null);

        // Assert
        _viewModel.SelectedTheme.Should().Be("System");
        _viewModel.StatusMessage.Should().Contain("System theme selected");
    }

    #endregion

    #region SelectSettingsTab Tests

    [Fact]
    public void SelectSettingsTab_ChangesSelectedTab()
    {
        // Arrange
        _viewModel.SelectedSettingsTab.Should().Be(0);

        // Act
        _viewModel.SelectSettingsTabCommand.Execute(2);

        // Assert
        _viewModel.SelectedSettingsTab.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SelectSettingsTab_AcceptsAllValidTabs(int tabIndex)
    {
        // Act
        _viewModel.SelectSettingsTabCommand.Execute(tabIndex);

        // Assert
        _viewModel.SelectedSettingsTab.Should().Be(tabIndex);
    }

    #endregion

    #region SetCacheService Tests

    [Fact]
    public void SetCacheService_WithService_UpdatesCacheStats()
    {
        // Arrange
        var cacheService = Substitute.For<IReportCacheService>();
        cacheService.GetStats().Returns(new CacheStats(5, 10, 3, 10240)); // 5 reports, ~10KB

        // Act
        _viewModel.SetCacheService(cacheService);

        // Assert
        _viewModel.CacheEntryCount.Should().Be(5);
        _viewModel.CacheSizeDisplay.Should().Contain("KB");
    }

    [Fact]
    public void SetCacheService_WithNull_SetsCacheStatsToNA()
    {
        // Act
        _viewModel.SetCacheService(null);

        // Assert
        _viewModel.CacheEntryCount.Should().Be(0);
        _viewModel.CacheSizeDisplay.Should().Be("N/A");
    }

    #endregion

    #region SaveCrmSettings Tests

    [Fact]
    public void SaveCrmSettings_InvokesEventWithCurrentValues()
    {
        // Arrange
        bool eventInvoked = false;
        bool savedEnabled = false;
        string? savedUrl = null;
        string? savedTenantId = null;
        string? savedClientId = null;
        string? savedSecret = null;

        _viewModel.SaveCrmSettingsRequested += (enabled, url, tenantId, clientId, secret) =>
        {
            eventInvoked = true;
            savedEnabled = enabled;
            savedUrl = url;
            savedTenantId = tenantId;
            savedClientId = clientId;
            savedSecret = secret;
        };

        _viewModel.CrmEnabled = true;
        _viewModel.CrmInstanceUrl = "https://org.crm.dynamics.com";
        _viewModel.CrmTenantId = "tenant-123";
        _viewModel.CrmClientId = "client-456";
        _viewModel.CrmClientSecret = "secret-789";

        // Act
        _viewModel.SaveCrmSettingsCommand.Execute(null);

        // Assert
        eventInvoked.Should().BeTrue();
        savedEnabled.Should().BeTrue();
        savedUrl.Should().Be("https://org.crm.dynamics.com");
        savedTenantId.Should().Be("tenant-123");
        savedClientId.Should().Be("client-456");
        savedSecret.Should().Be("secret-789");
        _viewModel.StatusMessage.Should().Contain("CRM settings saved");
    }

    #endregion
}
