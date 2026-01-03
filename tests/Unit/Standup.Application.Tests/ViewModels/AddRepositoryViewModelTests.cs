// <copyright file="AddRepositoryViewModelTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="AddRepositoryViewModel"/> methods and behavior.
/// Uses NSubstitute for mocking dependencies.
/// </summary>
public sealed class AddRepositoryViewModelTests
{
    private readonly GroupService _groupService;
    private readonly CredentialService _credentialService;
    private readonly IRepositoryDiscoveryService _discoveryService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger _logger;
    private readonly IGroupRepository _groupRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly AddRepositoryViewModel _viewModel;

    public AddRepositoryViewModelTests()
    {
        _groupRepository = Substitute.For<IGroupRepository>();
        _credentialRepository = Substitute.For<ICredentialRepository>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _discoveryService = Substitute.For<IRepositoryDiscoveryService>();
        _logger = Substitute.For<ILogger>();

        _groupService = new GroupService(_groupRepository, _credentialRepository, _encryptionService);
        _credentialService = new CredentialService(_credentialRepository, _encryptionService);

        _viewModel = new AddRepositoryViewModel(
            _groupService,
            _credentialService,
            _discoveryService,
            _encryptionService,
            _logger);
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_HasDefaults()
    {
        // Assert
        _viewModel.ClientCode.Should().BeEmpty();
        _viewModel.RecentClientCodes.Should().BeEmpty();
        _viewModel.SourceType.Should().Be(SourceType.AzureDevOps);
        _viewModel.Organization.Should().BeEmpty();
        _viewModel.Pat.Should().BeEmpty();
        _viewModel.Project.Should().BeNull();
        _viewModel.AvailableProjects.Should().BeEmpty();
        _viewModel.SelectedRepository.Should().BeNull();
        _viewModel.AvailableRepositories.Should().BeEmpty();
        _viewModel.AuthorIdentifier.Should().BeNull();
        _viewModel.LocalPath.Should().BeNull();
        _viewModel.UseRepoPat.Should().BeFalse();
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.IsDiscovering.Should().BeFalse();
        _viewModel.PatValidated.Should().BeFalse();
        _viewModel.StatusMessage.Should().BeEmpty();
        _viewModel.HasExistingOrgCredential.Should().BeFalse();
    }

    #endregion

    #region Initialize Tests

    [Fact]
    public void Initialize_SetsGroupIdAndClearsState()
    {
        // Arrange
        _viewModel.ClientCode = "OLD";
        _viewModel.Organization = "old-org";

        // Act
        _viewModel.Initialize("group-123");

        // Assert
        _viewModel.ClientCode.Should().BeEmpty();
        _viewModel.Organization.Should().BeEmpty();
        _viewModel.Pat.Should().BeEmpty();
    }

    [Fact]
    public void Initialize_LoadsRecentClientCodes()
    {
        // Arrange
        var recentCodes = new[] { "ACME", "CORP", "DEMO" };

        // Act
        _viewModel.Initialize("group-123", recentCodes);

        // Assert
        _viewModel.RecentClientCodes.Should().HaveCount(3);
        _viewModel.RecentClientCodes.Should().Contain("ACME");
        _viewModel.RecentClientCodes.Should().Contain("CORP");
        _viewModel.RecentClientCodes.Should().Contain("DEMO");
    }

    [Fact]
    public void Initialize_LimitsRecentClientCodesToFive()
    {
        // Arrange
        var recentCodes = new[] { "A", "B", "C", "D", "E", "F", "G" };

        // Act
        _viewModel.Initialize("group-123", recentCodes);

        // Assert
        _viewModel.RecentClientCodes.Should().HaveCount(5);
    }

    #endregion

    #region SelectRecentClientCode Tests

    [Fact]
    public void SelectRecentClientCode_SetsClientCode()
    {
        // Arrange
        _viewModel.Initialize("group-123", new[] { "ACME", "CORP" });

        // Act
        _viewModel.SelectRecentClientCodeCommand.Execute("ACME");

        // Assert
        _viewModel.ClientCode.Should().Be("ACME");
    }

    #endregion

    #region SourceType Changed Tests

    [Fact]
    public void OnSourceTypeChanged_ToGitHub_ClearsProjectFields()
    {
        // Arrange
        _viewModel.Project = "old-project";
        _viewModel.AvailableProjects.Add("proj1");

        // Act
        _viewModel.SourceType = SourceType.GitHub;

        // Assert
        _viewModel.Project.Should().BeNull();
        _viewModel.AvailableProjects.Should().BeEmpty();
    }

    [Fact]
    public void OnSourceTypeChanged_ResetsValidationState()
    {
        // Arrange
        _viewModel.PatValidated = true;
        _viewModel.AvailableRepositories.Add(new DiscoveredRepository("repo", null, "org", SourceType.AzureDevOps));

        // Act
        _viewModel.SourceType = SourceType.GitHub;

        // Assert
        _viewModel.PatValidated.Should().BeFalse();
        _viewModel.AvailableRepositories.Should().BeEmpty();
        _viewModel.SelectedRepository.Should().BeNull();
    }

    #endregion

    #region Organization Changed Tests

    [Fact]
    public void OnOrganizationChanged_ResetsValidationState()
    {
        // Arrange
        _viewModel.PatValidated = true;
        _viewModel.AvailableRepositories.Add(new DiscoveredRepository("repo", null, "org", SourceType.AzureDevOps));
        _viewModel.SelectedRepository = new DiscoveredRepository("repo", "org", null, SourceType.AzureDevOps);

        // Act
        _viewModel.Organization = "new-org";

        // Assert
        _viewModel.PatValidated.Should().BeFalse();
        _viewModel.AvailableRepositories.Should().BeEmpty();
        _viewModel.SelectedRepository.Should().BeNull();
    }

    #endregion

    #region ValidateAndDiscover Tests

    [Fact]
    public async Task ValidateAndDiscoverAsync_WithNoOrganization_SetsError()
    {
        // Arrange
        _viewModel.Organization = string.Empty;

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Organization is required");
        _viewModel.PatValidated.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAndDiscoverAsync_WithNoPatAndNoCredential_SetsError()
    {
        // Arrange
        _viewModel.Organization = "my-org";
        _viewModel.Pat = string.Empty;
        _viewModel.HasExistingOrgCredential = false;

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("PAT is required");
        _viewModel.PatValidated.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAndDiscoverAsync_WithInvalidPat_SetsError()
    {
        // Arrange
        _viewModel.Organization = "my-org";
        _viewModel.Pat = "invalid-pat";
        _discoveryService.ValidatePatAsync(
            Arg.Any<SourceType>(),
            Arg.Any<string>(),
            Arg.Any<string>()).Returns(false);

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Invalid credentials");
        _viewModel.PatValidated.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAndDiscoverAsync_AzureDevOps_DiscoversProjects()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.Organization = "my-org";
        _viewModel.Pat = "valid-pat";
        _viewModel.SourceType = SourceType.AzureDevOps;

        _discoveryService.ValidatePatAsync(
            SourceType.AzureDevOps,
            "my-org",
            "valid-pat").Returns(true);

        _discoveryService.GetProjectsAsync(
            SourceType.AzureDevOps,
            "my-org",
            "valid-pat").Returns(new List<string> { "Project1", "Project2" });

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PatValidated.Should().BeTrue();
        _viewModel.AvailableProjects.Should().HaveCount(2);
        _viewModel.AvailableProjects.Should().Contain("Project1");
        _viewModel.StatusMessage.Should().Contain("Found 2 projects");
    }

    [Fact]
    public async Task ValidateAndDiscoverAsync_GitHub_DiscoversReposDirectly()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.Organization = "my-org";
        _viewModel.Pat = "valid-pat";
        _viewModel.SourceType = SourceType.GitHub;

        _discoveryService.ValidatePatAsync(
            SourceType.GitHub,
            "my-org",
            "valid-pat").Returns(true);

        _discoveryService.GetRepositoriesAsync(
            SourceType.GitHub,
            "my-org",
            null,
            "valid-pat").Returns(new List<DiscoveredRepository>
        {
            new DiscoveredRepository("repo1", "my-org", null, SourceType.GitHub),
            new DiscoveredRepository("repo2", "my-org", null, SourceType.GitHub)
        });

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert
        _viewModel.PatValidated.Should().BeTrue();
        _viewModel.AvailableRepositories.Should().HaveCount(2);
        _viewModel.StatusMessage.Should().Contain("Found 2 repositories");
    }

    [Fact]
    public async Task ValidateAndDiscoverAsync_SetsIsDiscoveringDuringOperation()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.Organization = "my-org";
        _viewModel.Pat = "valid-pat";
        _viewModel.SourceType = SourceType.GitHub;

        _discoveryService.ValidatePatAsync(
            Arg.Any<SourceType>(),
            Arg.Any<string>(),
            Arg.Any<string>()).Returns(true);

        _discoveryService.GetRepositoriesAsync(
            Arg.Any<SourceType>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>()).Returns(new List<DiscoveredRepository>());

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert - should be false after completion
        _viewModel.IsDiscovering.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAndDiscoverAsync_WithException_SetsError()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.Organization = "my-org";
        _viewModel.Pat = "valid-pat";

        _discoveryService.ValidatePatAsync(
            Arg.Any<SourceType>(),
            Arg.Any<string>(),
            Arg.Any<string>()).ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.ValidateAndDiscoverCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Network error");
        _viewModel.PatValidated.Should().BeFalse();
        _viewModel.IsDiscovering.Should().BeFalse();
    }

    #endregion

    #region AddRepository Tests

    [Fact]
    public async Task AddRepositoryAsync_WithNoGroupId_SetsError()
    {
        // Arrange - don't call Initialize

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("No group selected");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithNoSelectedRepository_SetsError()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.SelectedRepository = null;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Please select a repository");
    }

    [Fact]
    public async Task AddRepositoryAsync_AddsRepositoryToGroup()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.ClientCode = "ACME";
        _viewModel.SelectedRepository = new DiscoveredRepository(
            Name: "test-repo",
            Project: "my-project",
            Organization: "my-org",
            SourceType: SourceType.AzureDevOps);

        var group = new RepositoryGroup { Id = "group-123", Name = "Test Group" };
        _groupRepository.GetByIdAsync("group-123", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        GroupedRepository? addedRepo = null;
        _viewModel.OnRepositoryAdded += (sender, repo) => addedRepo = repo;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("added successfully");
        addedRepo.Should().NotBeNull();
        addedRepo!.ClientCode.Should().Be("ACME");
        addedRepo.Repository.Should().Be("test-repo");
        addedRepo.Organization.Should().Be("my-org");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithRepoPat_EncryptsPat()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.ClientCode = "ACME";
        _viewModel.SelectedRepository = new DiscoveredRepository(
            Name: "test-repo",
            Project: null,
            Organization: "my-org",
            SourceType: SourceType.GitHub);
        _viewModel.UseRepoPat = true;
        _viewModel.Pat = "my-secret-pat";

        var group = new RepositoryGroup { Id = "group-123", Name = "Test Group" };
        _groupRepository.GetByIdAsync("group-123", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        _encryptionService.EncryptAsync("my-secret-pat").Returns("encrypted-pat");

        GroupedRepository? addedRepo = null;
        _viewModel.OnRepositoryAdded += (sender, repo) => addedRepo = repo;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        await _encryptionService.Received(1).EncryptAsync("my-secret-pat");
        addedRepo.Should().NotBeNull();
        addedRepo!.EncryptedPat.Should().Be("encrypted-pat");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithoutRepoPat_DoesNotEncrypt()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.ClientCode = "ACME";
        _viewModel.SelectedRepository = new DiscoveredRepository(
            Name: "test-repo",
            Project: null,
            Organization: "my-org",
            SourceType: SourceType.GitHub);
        _viewModel.UseRepoPat = false; // Not using repo-level PAT

        var group = new RepositoryGroup { Id = "group-123", Name = "Test Group" };
        _groupRepository.GetByIdAsync("group-123", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        GroupedRepository? addedRepo = null;
        _viewModel.OnRepositoryAdded += (sender, repo) => addedRepo = repo;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        await _encryptionService.DidNotReceive().EncryptAsync(Arg.Any<string>());
        addedRepo!.EncryptedPat.Should().BeNull();
    }

    [Fact]
    public async Task AddRepositoryAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.SelectedRepository = new DiscoveredRepository(
            Name: "test-repo",
            Project: null,
            Organization: "my-org",
            SourceType: SourceType.GitHub);

        var group = new RepositoryGroup { Id = "group-123", Name = "Test Group" };
        _groupRepository.GetByIdAsync("group-123", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert - should be false after completion
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task AddRepositoryAsync_WithException_SetsError()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.SelectedRepository = new DiscoveredRepository(
            Name: "test-repo",
            Project: null,
            Organization: "my-org",
            SourceType: SourceType.GitHub);

        _groupRepository.GetByIdAsync("group-123", Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Database error");
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_RaisesOnCancelledEvent()
    {
        // Arrange
        bool cancelledEventRaised = false;
        _viewModel.OnCancelled += (sender, args) => cancelledEventRaised = true;

        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        cancelledEventRaised.Should().BeTrue();
    }

    #endregion

    #region Event Tests

    [Fact]
    public async Task OnRepositoryAdded_IsRaisedWithCorrectData()
    {
        // Arrange
        _viewModel.Initialize("group-123");
        _viewModel.ClientCode = "TEST";
        _viewModel.AuthorIdentifier = "author@example.com";
        _viewModel.LocalPath = "/path/to/repo";
        _viewModel.SelectedRepository = new DiscoveredRepository(
            Name: "my-repo",
            Project: "my-project",
            Organization: "my-org",
            SourceType: SourceType.AzureDevOps);

        var group = new RepositoryGroup { Id = "group-123", Name = "Test Group" };
        _groupRepository.GetByIdAsync("group-123", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        GroupedRepository? addedRepo = null;
        _viewModel.OnRepositoryAdded += (sender, repo) => addedRepo = repo;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        addedRepo.Should().NotBeNull();
        addedRepo!.ClientCode.Should().Be("TEST");
        addedRepo.SourceType.Should().Be(SourceType.AzureDevOps);
        addedRepo.Organization.Should().Be("my-org");
        addedRepo.Project.Should().Be("my-project");
        addedRepo.Repository.Should().Be("my-repo");
        addedRepo.AuthorIdentifier.Should().Be("author@example.com");
        addedRepo.LocalPath.Should().Be("/path/to/repo");
        addedRepo.IsActive.Should().BeTrue();
    }

    #endregion
}
