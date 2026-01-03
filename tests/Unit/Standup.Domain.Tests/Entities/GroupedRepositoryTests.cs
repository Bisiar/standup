// <copyright file="GroupedRepositoryTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="GroupedRepository"/> entity.
/// </summary>
public sealed class GroupedRepositoryTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedRepositoryTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public GroupedRepositoryTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NewGroupedRepository_HasDefaultValues()
    {
        // Act
        var repo = new GroupedRepository();

        // Assert
        repo.Id.Should().NotBeNullOrEmpty();
        repo.ClientCode.Should().BeEmpty();
        repo.Name.Should().BeEmpty();
        repo.SourceType.Should().Be(SourceType.GitHub); // GitHub is first enum value (default)
        repo.Organization.Should().BeEmpty();
        repo.Project.Should().BeNull();
        repo.Repository.Should().BeEmpty();
        repo.EncryptedPat.Should().BeNull();
        repo.AuthorIdentifier.Should().BeNull();
        repo.LocalPath.Should().BeNull();
        repo.ApiEndpoint.Should().BeNull();
        repo.IsActive.Should().BeTrue();
        repo.IncludeInGeneration.Should().BeTrue();
        _output.WriteLine($"Created repository with ID: {repo.Id}");
    }

    [Fact]
    public void GroupedRepository_CanSetAzureDevOpsProperties()
    {
        // Arrange
        var repo = new GroupedRepository();

        // Act
        repo.ClientCode = "ACME";
        repo.Name = "acme-web-app";
        repo.SourceType = SourceType.AzureDevOps;
        repo.Organization = "acme-org";
        repo.Project = "Web-Platform";
        repo.Repository = "acme-web-app";
        repo.EncryptedPat = "encrypted-pat-value";
        repo.AuthorIdentifier = "john.doe@acme.com";
        repo.LocalPath = "/Users/dev/repos/acme-web-app";

        // Assert
        repo.ClientCode.Should().Be("ACME");
        repo.Name.Should().Be("acme-web-app");
        repo.SourceType.Should().Be(SourceType.AzureDevOps);
        repo.Organization.Should().Be("acme-org");
        repo.Project.Should().Be("Web-Platform");
        repo.Repository.Should().Be("acme-web-app");
        repo.EncryptedPat.Should().Be("encrypted-pat-value");
        repo.AuthorIdentifier.Should().Be("john.doe@acme.com");
        repo.LocalPath.Should().Be("/Users/dev/repos/acme-web-app");
        _output.WriteLine($"Azure DevOps repo: {repo.Organization}/{repo.Project}/{repo.Repository}");
    }

    [Fact]
    public void GroupedRepository_CanSetGitHubProperties()
    {
        // Arrange
        var repo = new GroupedRepository();

        // Act
        repo.ClientCode = "OPENSOURCE";
        repo.Name = "awesome-lib";
        repo.SourceType = SourceType.GitHub;
        repo.Organization = "awesome-org";
        repo.Repository = "awesome-lib";
        repo.ApiEndpoint = null; // Public GitHub

        // Assert
        repo.SourceType.Should().Be(SourceType.GitHub);
        repo.Organization.Should().Be("awesome-org");
        repo.Repository.Should().Be("awesome-lib");
        repo.ApiEndpoint.Should().BeNull();
        _output.WriteLine($"GitHub repo: {repo.Organization}/{repo.Repository}");
    }

    [Fact]
    public void GroupedRepository_CanSetGitHubEnterpriseProperties()
    {
        // Arrange
        var repo = new GroupedRepository();

        // Act
        repo.ClientCode = "INTERNAL";
        repo.Name = "internal-tool";
        repo.SourceType = SourceType.GitHub;
        repo.Organization = "enterprise-org";
        repo.Repository = "internal-tool";
        repo.ApiEndpoint = "https://github.company.com";

        // Assert
        repo.SourceType.Should().Be(SourceType.GitHub);
        repo.ApiEndpoint.Should().Be("https://github.company.com");
        _output.WriteLine($"GitHub Enterprise repo at: {repo.ApiEndpoint}");
    }

    [Fact]
    public void GroupedRepository_IsActive_CanBeDisabled()
    {
        // Arrange
        var repo = new GroupedRepository { Repository = "test-repo", IsActive = true };

        // Act
        repo.IsActive = false;

        // Assert
        repo.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GroupedRepository_IncludeInGeneration_CanBeDisabled()
    {
        // Arrange
        var repo = new GroupedRepository { Repository = "test-repo", IncludeInGeneration = true };

        // Act
        repo.IncludeInGeneration = false;

        // Assert
        repo.IncludeInGeneration.Should().BeFalse();
    }

    [Fact]
    public void GroupedRepository_GeneratesUniqueIds()
    {
        // Arrange & Act
        var repo1 = new GroupedRepository();
        var repo2 = new GroupedRepository();

        // Assert
        repo1.Id.Should().NotBe(repo2.Id);
        _output.WriteLine($"Repo1 ID: {repo1.Id}, Repo2 ID: {repo2.Id}");
    }

    [Fact]
    public void GroupedRepository_WithAllSourceTypes_SetsCorrectly()
    {
        // Test each source type
        var azureDevOps = new GroupedRepository { SourceType = SourceType.AzureDevOps };
        var github = new GroupedRepository { SourceType = SourceType.GitHub };

        azureDevOps.SourceType.Should().Be(SourceType.AzureDevOps);
        github.SourceType.Should().Be(SourceType.GitHub);
    }
}
