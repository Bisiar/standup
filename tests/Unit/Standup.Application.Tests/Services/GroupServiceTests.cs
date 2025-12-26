using FluentAssertions;
using NSubstitute;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for GroupService, especially credential lookup for standup generation.
/// These tests verify the critical path: save credential → lookup credential → generate standup.
/// </summary>
public sealed class GroupServiceTests
{
    private readonly IGroupRepository _groupRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly GroupService _service;

    public GroupServiceTests()
    {
        _groupRepository = Substitute.For<IGroupRepository>();
        _credentialRepository = Substitute.For<ICredentialRepository>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _service = new GroupService(_groupRepository, _credentialRepository, _encryptionService);
    }

    // GetDecryptedPatForRepositoryAsync Tests - Critical Path
    [Fact]
    public async Task GetDecryptedPatForRepositoryAsync_WithRepoLevelPat_ReturnsDecryptedPat()
    {
        // Arrange - Repository has its own PAT
        var encryptedPat = "encrypted-repo-pat";
        var decryptedPat = "decrypted-repo-pat";
        var repository = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            Repository = "test-repo",
            EncryptedPat = encryptedPat
        };

        _encryptionService.DecryptAsync(encryptedPat).Returns(decryptedPat);

        // Act
        var result = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        result.Should().Be(decryptedPat);
        await _encryptionService.Received(1).DecryptAsync(encryptedPat);

        // Should NOT look up org credential when repo has its own PAT
        await _credentialRepository.DidNotReceive().GetByOrgAsync(
            Arg.Any<SourceType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDecryptedPatForRepositoryAsync_WithoutRepoLevelPat_FallsBackToOrgCredential()
    {
        // Arrange - Repository has no PAT, but org-level credential exists
        var repository = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            Repository = "test-repo",
            EncryptedPat = null // No repo-level PAT
        };

        var orgCredential = new OrgCredential
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            EncryptedPat = "encrypted-org-pat"
        };

        _credentialRepository.GetByOrgAsync(SourceType.AzureDevOps, "test-org", Arg.Any<CancellationToken>())
            .Returns(orgCredential);
        _encryptionService.DecryptAsync("encrypted-org-pat").Returns("decrypted-org-pat");

        // Act
        var result = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        result.Should().Be("decrypted-org-pat");
        await _credentialRepository.Received(1).GetByOrgAsync(
            SourceType.AzureDevOps, "test-org", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDecryptedPatForRepositoryAsync_WithNoCredentialAtAll_ReturnsNull()
    {
        // Arrange - No repo PAT, no org credential - THIS IS THE BUG SCENARIO
        var repository = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            Repository = "test-repo",
            EncryptedPat = null
        };

        _credentialRepository.GetByOrgAsync(SourceType.AzureDevOps, "test-org", Arg.Any<CancellationToken>())
            .Returns((OrgCredential?)null);

        // Act
        var result = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        result.Should().BeNull();

        // This test documents the behavior that causes "No PAT available" log message
    }

    [Fact]
    public async Task GetDecryptedPatForRepositoryAsync_WithEmptyRepoPatString_FallsBackToOrgCredential()
    {
        // Arrange - Repository has empty string PAT (not null)
        var repository = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            Repository = "test-repo",
            EncryptedPat = string.Empty // Empty string, should fall back
        };

        var orgCredential = new OrgCredential
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            EncryptedPat = "encrypted-org-pat"
        };

        _credentialRepository.GetByOrgAsync(SourceType.AzureDevOps, "test-org", Arg.Any<CancellationToken>())
            .Returns(orgCredential);
        _encryptionService.DecryptAsync("encrypted-org-pat").Returns("decrypted-org-pat");

        // Act
        var result = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        result.Should().Be("decrypted-org-pat");
    }

    [Fact]
    public async Task GetDecryptedPatForRepositoryAsync_OrgCredentialWithEmptyPat_ReturnsNull()
    {
        // Arrange - Org credential exists but has empty PAT
        var repository = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            Repository = "test-repo",
            EncryptedPat = null
        };

        var orgCredential = new OrgCredential
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "test-org",
            EncryptedPat = string.Empty // Empty - credential exists but no PAT
        };

        _credentialRepository.GetByOrgAsync(SourceType.AzureDevOps, "test-org", Arg.Any<CancellationToken>())
            .Returns(orgCredential);

        // Act
        var result = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDecryptedPatForRepositoryAsync_GitHubSourceType_UsesCorrectSourceType()
    {
        // Arrange - Test with GitHub to ensure source type is passed correctly
        var repository = new GroupedRepository
        {
            SourceType = SourceType.GitHub,
            Organization = "github-org",
            Repository = "test-repo",
            EncryptedPat = null
        };

        var orgCredential = new OrgCredential
        {
            SourceType = SourceType.GitHub,
            Organization = "github-org",
            EncryptedPat = "encrypted-github-pat"
        };

        _credentialRepository.GetByOrgAsync(SourceType.GitHub, "github-org", Arg.Any<CancellationToken>())
            .Returns(orgCredential);
        _encryptionService.DecryptAsync("encrypted-github-pat").Returns("github-token");

        // Act
        var result = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        result.Should().Be("github-token");
        await _credentialRepository.Received(1).GetByOrgAsync(
            SourceType.GitHub, "github-org", Arg.Any<CancellationToken>());
    }

    // Integration Scenario Tests
    [Fact]
    public async Task FullFlow_SaveCredentialThenLookup_FindsPat()
    {
        // This test simulates the full flow that was broken:
        // 1. User adds repo with PAT
        // 2. PAT is saved to org credential
        // 3. Later, standup generation looks up PAT
        // 4. PAT should be found

        // Arrange - Simulate saved org credential
        var savedCredential = new OrgCredential
        {
            Id = "cred-123",
            SourceType = SourceType.AzureDevOps,
            Organization = "UPREHS",
            EncryptedPat = "encrypted-real-pat"
        };

        _credentialRepository.GetByOrgAsync(SourceType.AzureDevOps, "UPREHS", Arg.Any<CancellationToken>())
            .Returns(savedCredential);
        _encryptionService.DecryptAsync("encrypted-real-pat").Returns("real-pat-token");

        // Act - Simulate standup generation looking up PAT for a repo
        var repository = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "UPREHS",
            Project = "AI-Chat-Bot",
            Repository = "AI-Chat-Bot",
            EncryptedPat = null // No repo-level PAT - must use org credential
        };

        var pat = await _service.GetDecryptedPatForRepositoryAsync(repository);

        // Assert
        pat.Should().Be("real-pat-token");
    }

    [Fact]
    public async Task MultipleRepos_SameOrg_UseSharedOrgCredential()
    {
        // Arrange - Multiple repos in same org should share credential
        var orgCredential = new OrgCredential
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "shared-org",
            EncryptedPat = "encrypted-shared-pat"
        };

        _credentialRepository.GetByOrgAsync(SourceType.AzureDevOps, "shared-org", Arg.Any<CancellationToken>())
            .Returns(orgCredential);
        _encryptionService.DecryptAsync("encrypted-shared-pat").Returns("shared-pat");

        var repo1 = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "shared-org",
            Repository = "repo1",
            EncryptedPat = null
        };

        var repo2 = new GroupedRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "shared-org",
            Repository = "repo2",
            EncryptedPat = null
        };

        // Act
        var pat1 = await _service.GetDecryptedPatForRepositoryAsync(repo1);
        var pat2 = await _service.GetDecryptedPatForRepositoryAsync(repo2);

        // Assert - Both repos should get the same PAT
        pat1.Should().Be("shared-pat");
        pat2.Should().Be("shared-pat");
    }

    // Group CRUD Tests
    [Fact]
    public async Task AddGroupAsync_AssignsIdAndCreatedAt()
    {
        // Arrange
        var group = new RepositoryGroup { Name = "Test Group" };
        _groupRepository.AddAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RepositoryGroup>());

        // Act
        var result = await _service.AddGroupAsync(group);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddRepositoryToGroupAsync_ThrowsWhenGroupNotFound()
    {
        // Arrange
        _groupRepository.GetByIdAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns((RepositoryGroup?)null);

        var repository = new GroupedRepository { Repository = "test-repo" };

        // Act & Assert
        var act = () => _service.AddRepositoryToGroupAsync("non-existent", repository);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Group non-existent not found*");
    }

    [Fact]
    public async Task AddRepositoryToGroupAsync_AddsToGroupAndAssignsId()
    {
        // Arrange
        var group = new RepositoryGroup { Id = "group-1", Name = "Test" };
        _groupRepository.GetByIdAsync("group-1", Arg.Any<CancellationToken>()).Returns(group);
        _groupRepository.UpdateAsync(Arg.Any<RepositoryGroup>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<RepositoryGroup>());

        var repository = new GroupedRepository { Repository = "test-repo" };

        // Act
        var result = await _service.AddRepositoryToGroupAsync("group-1", repository);

        // Assert
        result.Id.Should().NotBeNullOrEmpty();
        group.Repositories.Should().Contain(result);
    }
}
