using FluentAssertions;
using Serilog;
using Standup.Domain.Enums;
using Standup.Infrastructure.SourceProviders;
using Standup.Infrastructure.Tests.Configuration;
using Xunit;

namespace Standup.Infrastructure.Tests.SourceProviders;

/// <summary>
/// Integration tests for RepositoryDiscoveryService.
/// Tests Azure DevOps project and repository discovery using PAT authentication.
/// </summary>
public class RepositoryDiscoveryServiceTests
{
    private const string Organization = "JT-Ops";

    private readonly RepositoryDiscoveryService _service;
    private readonly string _pat;

    public RepositoryDiscoveryServiceTests()
    {
        // Load environment variables from .env file
        TestEnvironmentLoader.LoadEnvironmentVariables();

        _pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT") ?? string.Empty;
        _service = new RepositoryDiscoveryService(Log.Logger);
    }

    [Fact]
    public async Task GetProjects_WithValidPat_ReturnsProjects()
    {
        // Arrange & Act
        var projects = await _service.GetProjectsAsync(
            SourceType.AzureDevOps,
            Organization,
            _pat);

        // Assert
        projects.Should().NotBeNull();
        var projectList = projects.ToList();
        projectList.Should().NotBeEmpty();

        Console.WriteLine($"Found {projectList.Count} projects in organization '{Organization}':");
        foreach (var project in projectList)
        {
            Console.WriteLine($"  - {project}");
        }

        // Verify JTP project exists
        projectList.Should().Contain("JTP", "JTP project should exist in JT-Ops organization");
    }

    [Fact]
    public async Task GetRepositories_ForJTPProject_ReturnsRepositories()
    {
        // Arrange
        const string project = "JTP";

        // Act
        var repos = await _service.GetRepositoriesAsync(
            SourceType.AzureDevOps,
            Organization,
            project,
            _pat);

        // Assert
        repos.Should().NotBeNull();
        var repoList = repos.ToList();
        repoList.Should().NotBeEmpty();

        Console.WriteLine($"Found {repoList.Count} repositories in '{Organization}/{project}':");
        foreach (var repo in repoList.Take(20))
        {
            Console.WriteLine($"  - {repo.Name} (Project: {repo.Project})");
        }

        // Verify jt-crm-time-entry-ai exists
        repoList.Should().Contain(
            r => r.Name == "jt-crm-time-entry-ai",
            "jt-crm-time-entry-ai repository should exist in JTP project");
    }

    [Fact]
    public async Task ValidatePat_WithValidPat_ReturnsTrue()
    {
        // Act
        var isValid = await _service.ValidatePatAsync(
            SourceType.AzureDevOps,
            Organization,
            _pat);

        // Assert
        isValid.Should().BeTrue();
        Console.WriteLine("PAT validation successful!");
    }

    [Fact]
    public async Task ValidatePat_WithInvalidPat_ReturnsFalse()
    {
        // Arrange
        const string invalidPat = "invalid-pat-12345";

        // Act
        var isValid = await _service.ValidatePatAsync(
            SourceType.AzureDevOps,
            Organization,
            invalidPat);

        // Assert
        isValid.Should().BeFalse();
        Console.WriteLine("Invalid PAT correctly rejected!");
    }

    [Fact]
    public async Task GetProjects_ForGitHub_ReturnsEmpty()
    {
        // GitHub doesn't have "projects" in the Azure DevOps sense
        // Act
        var projects = await _service.GetProjectsAsync(
            SourceType.GitHub,
            "some-org",
            _pat);

        // Assert
        projects.Should().BeEmpty();
    }
}
