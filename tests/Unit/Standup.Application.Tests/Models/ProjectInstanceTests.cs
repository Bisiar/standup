// <copyright file="ProjectInstanceTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Standup.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="ProjectInstance"/> record.
/// </summary>
public sealed class ProjectInstanceTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectInstanceTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ProjectInstanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProjectInstance_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Act
        var project = new ProjectInstance(
            Id: "proj-123",
            Name: "ACME Web App",
            TenantName: "ACME Corp",
            ApiEndpoint: "https://api.example.com");

        // Assert
        project.Id.Should().Be("proj-123");
        project.Name.Should().Be("ACME Web App");
        project.TenantName.Should().Be("ACME Corp");
        project.ApiEndpoint.Should().Be("https://api.example.com");
        _output.WriteLine($"Project: {project.Name} ({project.TenantName})");
    }

    [Fact]
    public void ProjectInstance_OptionalProperties_DefaultCorrectly()
    {
        // Arrange & Act
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Assert
        project.UserId.Should().BeNull();
        project.TenantId.Should().BeNull();
        project.AccessToken.Should().BeNull();
        project.IsDefault.Should().BeFalse();
        project.SourceType.Should().Be(SourceType.AzureDevOps);
        project.SourceOrganization.Should().BeNull();
        project.SourceProject.Should().BeNull();
        project.SourceRepository.Should().BeNull();
        project.SourcePat.Should().BeNull();
        project.LocalPath.Should().BeNull();
        project.AuthorIdentifier.Should().BeNull();
        project.UseLocalGeneration.Should().BeTrue();
        project.ApiEndpointOverride.Should().BeNull();
        project.IncludeInGeneration.Should().BeTrue();
        project.CrmProjectId.Should().BeNull();
        project.CrmProjectName.Should().BeNull();
    }

    [Fact]
    public void ProjectInstance_WithAllProperties_SetsValuesCorrectly()
    {
        // Act
        var project = new ProjectInstance(
            Id: "proj-123",
            Name: "Full Project",
            TenantName: "Tenant",
            ApiEndpoint: "https://api.test.com",
            UserId: "user-456",
            TenantId: "tenant-789",
            AccessToken: "token-abc",
            IsDefault: true,
            SourceType: SourceType.GitHub,
            SourceOrganization: "org",
            SourceProject: "proj",
            SourceRepository: "repo",
            SourcePat: "pat-xyz",
            LocalPath: "/path/to/repo",
            AuthorIdentifier: "author@email.com",
            UseLocalGeneration: false,
            ApiEndpointOverride: "https://override.api.com",
            IncludeInGeneration: false,
            CrmProjectId: "crm-123",
            CrmProjectName: "CRM Project Name");

        // Assert
        project.UserId.Should().Be("user-456");
        project.TenantId.Should().Be("tenant-789");
        project.AccessToken.Should().Be("token-abc");
        project.IsDefault.Should().BeTrue();
        project.SourceType.Should().Be(SourceType.GitHub);
        project.SourceOrganization.Should().Be("org");
        project.SourceProject.Should().Be("proj");
        project.SourceRepository.Should().Be("repo");
        project.SourcePat.Should().Be("pat-xyz");
        project.LocalPath.Should().Be("/path/to/repo");
        project.AuthorIdentifier.Should().Be("author@email.com");
        project.UseLocalGeneration.Should().BeFalse();
        project.ApiEndpointOverride.Should().Be("https://override.api.com");
        project.IncludeInGeneration.Should().BeFalse();
        project.CrmProjectId.Should().Be("crm-123");
        project.CrmProjectName.Should().Be("CRM Project Name");
        _output.WriteLine($"Full project: {project.Name}, Source: {project.SourceType}");
    }

    [Fact]
    public void CreatedAt_DefaultsToNow()
    {
        // Arrange & Act
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Assert
        project.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void LastCommitHash_IsTransientProperty()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Act
        project.LastCommitHash = "abc12345";

        // Assert
        project.LastCommitHash.Should().Be("abc12345");
    }

    [Fact]
    public void LastCommitDate_IsTransientProperty()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");
        var commitDate = DateTimeOffset.UtcNow.AddHours(-2);

        // Act
        project.LastCommitDate = commitDate;

        // Assert
        project.LastCommitDate.Should().Be(commitDate);
    }

    [Fact]
    public void ValidationError_IsTransientProperty()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Act
        project.ValidationError = "Invalid credentials";

        // Assert
        project.ValidationError.Should().Be("Invalid credentials");
    }

    [Fact]
    public void IsValidating_IsTransientProperty()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Act
        project.IsValidating = true;

        // Assert
        project.IsValidating.Should().BeTrue();
    }

    [Fact]
    public void CommitInfoDisplay_WithValidCommitData_ReturnsFormattedString()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");
        var commitDate = new DateTimeOffset(2024, 12, 25, 10, 30, 0, TimeSpan.Zero);

        // Act
        project.LastCommitHash = "abc12345";
        project.LastCommitDate = commitDate;

        // Assert
        project.CommitInfoDisplay.Should().Be("abc12345 \u2022 Dec 25");
        _output.WriteLine($"Commit info: {project.CommitInfoDisplay}");
    }

    [Fact]
    public void CommitInfoDisplay_WithoutCommitData_ReturnsValidationError()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Act
        project.ValidationError = "Could not fetch commit";

        // Assert
        project.CommitInfoDisplay.Should().Be("Could not fetch commit");
    }

    [Fact]
    public void CommitInfoDisplay_WithoutCommitDataOrError_ReturnsEmptyString()
    {
        // Arrange
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");

        // Act & Assert
        project.CommitInfoDisplay.Should().BeEmpty();
    }

    [Fact]
    public void CommitInfoDisplay_WithOnlyHash_ReturnsEmpty()
    {
        // Arrange - Only hash set, no date
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");
        project.LastCommitHash = "abc12345";
        project.LastCommitDate = null;

        // Act & Assert
        project.CommitInfoDisplay.Should().BeEmpty();
    }

    [Fact]
    public void CommitInfoDisplay_WithOnlyDate_ReturnsEmpty()
    {
        // Arrange - Only date set, no hash
        var project = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");
        project.LastCommitHash = null;
        project.LastCommitDate = DateTimeOffset.UtcNow;

        // Act & Assert
        project.CommitInfoDisplay.Should().BeEmpty();
    }

    [Fact]
    public void ProjectInstance_RecordEquality_WorksCorrectly()
    {
        // Arrange - Use 'with' expression to ensure same CreatedAt timestamp
        var project1 = new ProjectInstance("1", "Test", "Tenant", "https://api.test.com");
        var project2 = project1 with { }; // Same values, same CreatedAt
        var project3 = project1 with { Id = "2" }; // Different Id

        // Act & Assert
        project1.Should().Be(project2);
        project1.Should().NotBe(project3);
    }
}
