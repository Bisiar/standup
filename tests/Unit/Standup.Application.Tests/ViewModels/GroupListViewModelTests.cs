// <copyright file="GroupListViewModelTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Application.ViewModels;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.ViewModels;

/// <summary>
/// Tests for <see cref="GroupListViewModel"/> methods and behavior.
/// Uses NSubstitute for mocking dependencies.
/// </summary>
public sealed class GroupListViewModelTests
{
    private readonly GroupService _groupService;
    private readonly CredentialService _credentialService;
    private readonly IEncryptionService _encryptionService;
    private readonly IProjectService _projectService;
    private readonly IGroupRepository _groupRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly GroupListViewModel _viewModel;

    public GroupListViewModelTests()
    {
        _groupRepository = Substitute.For<IGroupRepository>();
        _credentialRepository = Substitute.For<ICredentialRepository>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _projectService = Substitute.For<IProjectService>();

        _groupService = new GroupService(_groupRepository, _credentialRepository, _encryptionService);
        _credentialService = new CredentialService(_credentialRepository, _encryptionService);

        _viewModel = new GroupListViewModel(
            _groupService,
            _credentialService,
            _encryptionService,
            _projectService);
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_HasDefaults()
    {
        // Assert
        _viewModel.Groups.Should().BeEmpty();
        _viewModel.SelectedGroup.Should().BeNull();
        _viewModel.IsLoading.Should().BeFalse();
        _viewModel.IsAddingGroup.Should().BeFalse();
        _viewModel.NewGroupName.Should().BeEmpty();
        _viewModel.NewGroupDescription.Should().BeEmpty();
        _viewModel.StatusMessage.Should().BeEmpty();
        _viewModel.IsEditingGroup.Should().BeFalse();
        _viewModel.EditingGroupId.Should().BeNull();
        _viewModel.IsAddingRepository.Should().BeFalse();
        _viewModel.AddingToGroupId.Should().BeNull();
        _viewModel.CanShowAddGroupButton.Should().BeTrue();
    }

    #endregion

    #region CanShowAddGroupButton Tests

    [Fact]
    public void CanShowAddGroupButton_WhenNotAddingOrEditing_ReturnsTrue()
    {
        // Arrange
        _viewModel.IsAddingGroup = false;
        _viewModel.IsEditingGroup = false;

        // Assert
        _viewModel.CanShowAddGroupButton.Should().BeTrue();
    }

    [Fact]
    public void CanShowAddGroupButton_WhenAddingGroup_ReturnsFalse()
    {
        // Act
        _viewModel.ShowAddGroupCommand.Execute(null);

        // Assert
        _viewModel.CanShowAddGroupButton.Should().BeFalse();
    }

    [Fact]
    public void CanShowAddGroupButton_WhenEditingGroup_ReturnsFalse()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };

        // Act
        _viewModel.ShowEditGroupCommand.Execute(group);

        // Assert
        _viewModel.CanShowAddGroupButton.Should().BeFalse();
    }

    #endregion

    #region LoadGroups Tests

    [Fact]
    public async Task LoadGroupsAsync_LoadsGroupsFromService()
    {
        // Arrange
        var groups = new List<RepositoryGroup>
        {
            new RepositoryGroup { Id = "group-1", Name = "Group 1" },
            new RepositoryGroup { Id = "group-2", Name = "Group 2" }
        };
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(groups);
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(groups[0]);

        // Act
        await _viewModel.LoadGroupsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Groups.Should().HaveCount(2);
        _viewModel.SelectedGroup.Should().Be(groups[0]);
    }

    [Fact]
    public async Task LoadGroupsAsync_WithNoDefaultGroup_SelectsFirstGroup()
    {
        // Arrange
        var groups = new List<RepositoryGroup>
        {
            new RepositoryGroup { Id = "group-1", Name = "Group 1" }
        };
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(groups);
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.LoadGroupsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.SelectedGroup.Should().Be(groups[0]);
    }

    [Fact]
    public async Task LoadGroupsAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<RepositoryGroup>());
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.LoadGroupsCommand.ExecuteAsync(null);

        // Assert - should be false after completion
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region SelectGroup Tests

    [Fact]
    public async Task SelectGroupAsync_SetsSelectedGroupAndDefault()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test Group" };

        // Act
        await _viewModel.SelectGroupCommand.ExecuteAsync(group);

        // Assert
        _viewModel.SelectedGroup.Should().Be(group);
        await _groupRepository.Received(1).SetDefaultAsync("group-1", Arg.Any<CancellationToken>());
    }

    #endregion

    #region ShowAddGroup Tests

    [Fact]
    public void ShowAddGroup_SetsAddingModeAndClearsForm()
    {
        // Arrange
        _viewModel.NewGroupName = "Old Name";
        _viewModel.NewGroupDescription = "Old Desc";

        // Act
        _viewModel.ShowAddGroupCommand.Execute(null);

        // Assert
        _viewModel.IsAddingGroup.Should().BeTrue();
        _viewModel.NewGroupName.Should().BeEmpty();
        _viewModel.NewGroupDescription.Should().BeEmpty();
    }

    #endregion

    #region AddGroup Tests

    [Fact]
    public async Task AddGroupAsync_WithEmptyName_SetsError()
    {
        // Arrange
        _viewModel.ShowAddGroupCommand.Execute(null);
        _viewModel.NewGroupName = string.Empty;

        // Act
        await _viewModel.AddGroupCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Group name is required");
        _viewModel.IsAddingGroup.Should().BeTrue(); // Still in adding mode
    }

    [Fact]
    public async Task AddGroupAsync_WithValidName_AddsGroup()
    {
        // Arrange
        _viewModel.ShowAddGroupCommand.Execute(null);
        _viewModel.NewGroupName = "New Group";
        _viewModel.NewGroupDescription = "Description";

        _groupRepository.AddAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var g = (RepositoryGroup)x[0];
                g.Id = Guid.NewGuid().ToString();
                return g;
            });

        // Act
        await _viewModel.AddGroupCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsAddingGroup.Should().BeFalse();
        _viewModel.Groups.Should().HaveCount(1);
        _viewModel.Groups[0].Name.Should().Be("New Group");
        _viewModel.Groups[0].Description.Should().Be("Description");
        _viewModel.StatusMessage.Should().Contain("added successfully");
    }

    [Fact]
    public async Task AddGroupAsync_WithException_SetsErrorMessage()
    {
        // Arrange
        _viewModel.ShowAddGroupCommand.Execute(null);
        _viewModel.NewGroupName = "New Group";

        _groupRepository.AddAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        await _viewModel.AddGroupCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Database error");
    }

    #endregion

    #region CancelAddGroup Tests

    [Fact]
    public void CancelAddGroup_ExitsAddingMode()
    {
        // Arrange
        _viewModel.ShowAddGroupCommand.Execute(null);
        _viewModel.IsAddingGroup.Should().BeTrue();

        // Act
        _viewModel.CancelAddGroupCommand.Execute(null);

        // Assert
        _viewModel.IsAddingGroup.Should().BeFalse();
    }

    #endregion

    #region ShowEditGroup Tests

    [Fact]
    public void ShowEditGroup_SetsEditingModeAndLoadsData()
    {
        // Arrange
        var group = new RepositoryGroup
        {
            Id = "group-1",
            Name = "Test Group",
            Description = "Test Description"
        };

        // Act
        _viewModel.ShowEditGroupCommand.Execute(group);

        // Assert
        _viewModel.IsEditingGroup.Should().BeTrue();
        _viewModel.EditingGroupId.Should().Be("group-1");
        _viewModel.EditGroupName.Should().Be("Test Group");
        _viewModel.EditGroupDescription.Should().Be("Test Description");
    }

    [Fact]
    public void ShowEditGroup_WithNullDescription_SetsEmptyString()
    {
        // Arrange
        var group = new RepositoryGroup
        {
            Id = "group-1",
            Name = "Test Group",
            Description = null
        };

        // Act
        _viewModel.ShowEditGroupCommand.Execute(group);

        // Assert
        _viewModel.EditGroupDescription.Should().BeEmpty();
    }

    #endregion

    #region SaveEditGroup Tests

    [Fact]
    public async Task SaveEditGroupAsync_WithNoEditingGroupId_DoesNothing()
    {
        // Arrange
        _viewModel.EditingGroupId = null;

        // Act
        await _viewModel.SaveEditGroupCommand.ExecuteAsync(null);

        // Assert
        await _groupRepository.DidNotReceive().UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveEditGroupAsync_WithEmptyName_SetsError()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.ShowEditGroupCommand.Execute(group);
        _viewModel.EditGroupName = string.Empty;

        // Act
        await _viewModel.SaveEditGroupCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Group name is required");
    }

    [Fact]
    public async Task SaveEditGroupAsync_UpdatesGroupAndExitsEditMode()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Old Name" };
        _viewModel.Groups.Add(group);
        _viewModel.ShowEditGroupCommand.Execute(group);
        _viewModel.EditGroupName = "New Name";
        _viewModel.EditGroupDescription = "New Description";

        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RepositoryGroup> { group });
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.SaveEditGroupCommand.ExecuteAsync(null);

        // Assert
        group.Name.Should().Be("New Name");
        group.Description.Should().Be("New Description");
        _viewModel.IsEditingGroup.Should().BeFalse();
        _viewModel.EditingGroupId.Should().BeNull();
        _viewModel.StatusMessage.Should().Contain("updated successfully");
    }

    #endregion

    #region CancelEditGroup Tests

    [Fact]
    public void CancelEditGroup_ExitsEditModeAndClearsFields()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.ShowEditGroupCommand.Execute(group);

        // Act
        _viewModel.CancelEditGroupCommand.Execute(null);

        // Assert
        _viewModel.IsEditingGroup.Should().BeFalse();
        _viewModel.EditingGroupId.Should().BeNull();
        _viewModel.EditGroupName.Should().BeEmpty();
        _viewModel.EditGroupDescription.Should().BeEmpty();
    }

    #endregion

    #region DeleteGroup Tests

    [Fact]
    public async Task DeleteGroupAsync_RemovesGroupFromList()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.Groups.Add(group);

        // Act
        await _viewModel.DeleteGroupCommand.ExecuteAsync(group);

        // Assert
        _viewModel.Groups.Should().BeEmpty();
        await _groupRepository.Received(1).DeleteAsync("group-1", Arg.Any<CancellationToken>());
    }

    #endregion

    #region ToggleIncludeInGeneration Tests

    [Fact]
    public async Task ToggleIncludeInGenerationAsync_TogglesAndSaves()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test", IncludeInGeneration = true };
        _viewModel.Groups.Add(group);

        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<RepositoryGroup> { group });
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.ToggleIncludeInGenerationCommand.ExecuteAsync(group);

        // Assert
        group.IncludeInGeneration.Should().BeFalse();
        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleIncludeInGenerationAsync_WithException_SetsError()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test", IncludeInGeneration = true };
        _viewModel.Groups.Add(group);

        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        await _viewModel.ToggleIncludeInGenerationCommand.ExecuteAsync(group);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Update failed");
    }

    #endregion

    #region ShowAddRepository Tests

    [Fact]
    public void ShowAddRepository_SetsAddingModeAndClearsForm()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.NewRepoClientCode = "OLD";
        _viewModel.NewRepoOrganization = "old-org";

        bool eventRaised = false;
        RepositoryGroup? eventGroup = null;
        _viewModel.OnShowAddRepository += (sender, g) =>
        {
            eventRaised = true;
            eventGroup = g;
        };

        // Act
        _viewModel.ShowAddRepositoryCommand.Execute(group);

        // Assert
        _viewModel.IsAddingRepository.Should().BeTrue();
        _viewModel.AddingToGroupId.Should().Be("group-1");
        _viewModel.NewRepoClientCode.Should().BeEmpty();
        _viewModel.NewRepoOrganization.Should().BeEmpty();
        eventRaised.Should().BeTrue();
        eventGroup.Should().Be(group);
    }

    #endregion

    #region CancelAddRepository Tests

    [Fact]
    public void CancelAddRepository_ExitsAddingModeAndClearsForm()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.ShowAddRepositoryCommand.Execute(group);
        _viewModel.NewRepoClientCode = "TEST";

        // Act
        _viewModel.CancelAddRepositoryCommand.Execute(null);

        // Assert
        _viewModel.IsAddingRepository.Should().BeFalse();
        _viewModel.AddingToGroupId.Should().BeNull();
        _viewModel.NewRepoClientCode.Should().BeEmpty();
    }

    #endregion

    #region AddRepository Tests

    [Fact]
    public async Task AddRepositoryAsync_WithNoGroupId_SetsError()
    {
        // Arrange
        _viewModel.AddingToGroupId = null;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("No group selected");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithNoRepoName_SetsError()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.ShowAddRepositoryCommand.Execute(group);
        _viewModel.NewRepoRepository = string.Empty;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Repository name is required");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithNoOrganization_SetsError()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.ShowAddRepositoryCommand.Execute(group);
        _viewModel.NewRepoRepository = "my-repo";
        _viewModel.NewRepoOrganization = string.Empty;

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Organization is required");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithDuplicate_SetsError()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        group.Repositories.Add(new GroupedRepository
        {
            Organization = "my-org",
            Repository = "my-repo"
        });
        _viewModel.Groups.Add(group);

        _viewModel.ShowAddRepositoryCommand.Execute(group);
        _viewModel.NewRepoOrganization = "my-org";
        _viewModel.NewRepoRepository = "my-repo";

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithValidData_AddsRepository()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.Groups.Add(group);
        _viewModel.ShowAddRepositoryCommand.Execute(group);

        _viewModel.NewRepoClientCode = "acme";
        _viewModel.NewRepoOrganization = "my-org";
        _viewModel.NewRepoProject = "my-project";
        _viewModel.NewRepoRepository = "my-repo";
        _viewModel.NewRepoSourceType = SourceType.AzureDevOps;

        _groupRepository.GetByIdAsync("group-1", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);
        _projectService.GetProjectsAsync().Returns(new List<ProjectInstance>());
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<RepositoryGroup> { group });
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        _viewModel.StatusMessage.Should().Contain("added successfully");
        _viewModel.IsAddingRepository.Should().BeFalse();
        group.Repositories.Should().HaveCount(1);
        group.Repositories[0].ClientCode.Should().Be("ACME"); // Should be uppercased
        group.Repositories[0].Organization.Should().Be("my-org");
    }

    [Fact]
    public async Task AddRepositoryAsync_WithPat_EncryptsAndSavesPat()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _viewModel.Groups.Add(group);
        _viewModel.ShowAddRepositoryCommand.Execute(group);

        _viewModel.NewRepoOrganization = "my-org";
        _viewModel.NewRepoRepository = "my-repo";
        _viewModel.NewRepoPat = "my-secret-pat";
        _viewModel.NewRepoSourceType = SourceType.AzureDevOps;

        _groupRepository.GetByIdAsync("group-1", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);
        _encryptionService.EncryptAsync("my-secret-pat").Returns("encrypted-pat");
        _projectService.GetProjectsAsync().Returns(new List<ProjectInstance>());
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<RepositoryGroup> { group });
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.AddRepositoryCommand.ExecuteAsync(null);

        // Assert
        group.Repositories[0].EncryptedPat.Should().Be("encrypted-pat");
        await _credentialRepository.Received(1).SaveAsync(
            Arg.Is<OrgCredential>(c =>
                c.SourceType == SourceType.AzureDevOps &&
                c.Organization == "my-org"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteRepository Tests

    [Fact]
    public async Task DeleteRepositoryAsync_WithNoSelectedGroup_DoesNothing()
    {
        // Arrange
        _viewModel.SelectedGroup = null;
        var repo = new GroupedRepository { Id = "repo-1" };

        // Act
        await _viewModel.DeleteRepositoryCommand.ExecuteAsync(repo);

        // Assert
        await _groupRepository.DidNotReceive().GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRepositoryAsync_RemovesRepositoryFromGroup()
    {
        // Arrange
        var repo = new GroupedRepository { Id = "repo-1", Repository = "test-repo" };
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        group.Repositories.Add(repo);

        _viewModel.Groups.Add(group);
        _viewModel.SelectedGroup = group;

        _groupRepository.GetByIdAsync("group-1", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        // Act
        await _viewModel.DeleteRepositoryCommand.ExecuteAsync(repo);

        // Assert
        _viewModel.SelectedGroup.Repositories.Should().BeEmpty();
        await _groupRepository.Received(1).GetByIdAsync("group-1", Arg.Any<CancellationToken>());
    }

    #endregion

    #region SaveRepository Tests

    [Fact]
    public async Task SaveRepositoryAsync_FindsParentGroupAndSaves()
    {
        // Arrange
        var repo = new GroupedRepository { Id = "repo-1", Repository = "test-repo", ClientCode = "ACME" };
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        group.Repositories.Add(repo);
        _viewModel.Groups.Add(group);

        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);

        // Act
        await _viewModel.SaveRepositoryCommand.ExecuteAsync(repo);

        // Assert
        await _groupRepository.Received(1).UpdateAsync(group, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveRepositoryAsync_WithNoParentGroup_DoesNothing()
    {
        // Arrange
        var repo = new GroupedRepository { Id = "repo-1", Repository = "test-repo" };
        // Not added to any group

        // Act
        await _viewModel.SaveRepositoryCommand.ExecuteAsync(repo);

        // Assert
        await _groupRepository.DidNotReceive().UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region RemoveRepository Tests

    [Fact]
    public async Task RemoveRepositoryAsync_FindsParentGroupAndRemoves()
    {
        // Arrange
        var repo = new GroupedRepository { Id = "repo-1", Repository = "test-repo" };
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        group.Repositories.Add(repo);
        _viewModel.Groups.Add(group);

        _groupRepository.GetByIdAsync("group-1", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(x => (RepositoryGroup)x[0]);
        _groupRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<RepositoryGroup> { group });
        _groupRepository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((RepositoryGroup?)null);

        // Act
        await _viewModel.RemoveRepositoryCommand.ExecuteAsync(repo);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Removed");
    }

    [Fact]
    public async Task RemoveRepositoryAsync_WithNoParentGroup_SetsError()
    {
        // Arrange
        var repo = new GroupedRepository { Id = "repo-1", Repository = "test-repo" };
        // Not in any group

        // Act
        await _viewModel.RemoveRepositoryCommand.ExecuteAsync(repo);

        // Assert
        _viewModel.StatusMessage.Should().Contain("Could not find parent group");
    }

    #endregion

    #region GetRecentClientCodes Tests

    [Fact]
    public void GetRecentClientCodes_ReturnsDistinctCodesFromAllGroups()
    {
        // Arrange
        var group1 = new RepositoryGroup { Id = "g1", Name = "Group 1" };
        group1.Repositories.Add(new GroupedRepository { ClientCode = "ACME" });
        group1.Repositories.Add(new GroupedRepository { ClientCode = "CORP" });

        var group2 = new RepositoryGroup { Id = "g2", Name = "Group 2" };
        group2.Repositories.Add(new GroupedRepository { ClientCode = "ACME" }); // Duplicate
        group2.Repositories.Add(new GroupedRepository { ClientCode = "DEMO" });

        _viewModel.Groups.Add(group1);
        _viewModel.Groups.Add(group2);

        // Act
        var codes = _viewModel.GetRecentClientCodes().ToList();

        // Assert
        codes.Should().HaveCount(3);
        codes.Should().Contain("ACME");
        codes.Should().Contain("CORP");
        codes.Should().Contain("DEMO");
    }

    [Fact]
    public void GetRecentClientCodes_ExcludesEmptyCodes()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "g1", Name = "Group 1" };
        group.Repositories.Add(new GroupedRepository { ClientCode = "ACME" });
        group.Repositories.Add(new GroupedRepository { ClientCode = string.Empty });
        group.Repositories.Add(new GroupedRepository { ClientCode = null! });

        _viewModel.Groups.Add(group);

        // Act
        var codes = _viewModel.GetRecentClientCodes().ToList();

        // Assert
        codes.Should().HaveCount(1);
        codes.Should().Contain("ACME");
    }

    [Fact]
    public void GetRecentClientCodes_ReturnsMaximumTenCodes()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "g1", Name = "Group 1" };
        for (int i = 0; i < 15; i++)
        {
            group.Repositories.Add(new GroupedRepository { ClientCode = $"CODE{i:D2}" });
        }

        _viewModel.Groups.Add(group);

        // Act
        var codes = _viewModel.GetRecentClientCodes().ToList();

        // Assert
        codes.Should().HaveCount(10);
    }

    [Fact]
    public void GetRecentClientCodes_ReturnsOrderedAlphabetically()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "g1", Name = "Group 1" };
        group.Repositories.Add(new GroupedRepository { ClientCode = "ZEBRA" });
        group.Repositories.Add(new GroupedRepository { ClientCode = "ALPHA" });
        group.Repositories.Add(new GroupedRepository { ClientCode = "BRAVO" });

        _viewModel.Groups.Add(group);

        // Act
        var codes = _viewModel.GetRecentClientCodes().ToList();

        // Assert
        codes.Should().BeInAscendingOrder();
        codes[0].Should().Be("ALPHA");
        codes[1].Should().Be("BRAVO");
        codes[2].Should().Be("ZEBRA");
    }

    #endregion
}
