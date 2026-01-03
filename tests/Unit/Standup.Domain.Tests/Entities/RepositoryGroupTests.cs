// <copyright file="RepositoryGroupTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="RepositoryGroup"/> entity.
/// </summary>
public sealed class RepositoryGroupTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryGroupTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public RepositoryGroupTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void NewRepositoryGroup_HasDefaultValues()
    {
        // Act
        var group = new RepositoryGroup();

        // Assert
        group.Id.Should().NotBeNullOrEmpty();
        group.Name.Should().BeEmpty();
        group.Description.Should().BeNull();
        group.IsDefault.Should().BeFalse();
        group.IncludeInGeneration.Should().BeTrue();
        group.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        group.Repositories.Should().NotBeNull().And.BeEmpty();
        _output.WriteLine($"Created group with ID: {group.Id}");
    }

    [Fact]
    public void RepositoryGroup_CanSetProperties()
    {
        // Arrange
        var group = new RepositoryGroup();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-7);

        // Act
        group.Name = "Client Projects";
        group.Description = "All repositories for client work";
        group.IsDefault = true;
        group.IncludeInGeneration = false;
        group.CreatedAt = createdAt;

        // Assert
        group.Name.Should().Be("Client Projects");
        group.Description.Should().Be("All repositories for client work");
        group.IsDefault.Should().BeTrue();
        group.IncludeInGeneration.Should().BeFalse();
        group.CreatedAt.Should().Be(createdAt);
        _output.WriteLine($"Group: {group.Name}, IsDefault: {group.IsDefault}");
    }

    [Fact]
    public void RepositoryGroup_CanAddRepositories()
    {
        // Arrange
        var group = new RepositoryGroup { Name = "Test Group" };
        var repo1 = new GroupedRepository { Repository = "repo-1" };
        var repo2 = new GroupedRepository { Repository = "repo-2" };

        // Act
        group.Repositories.Add(repo1);
        group.Repositories.Add(repo2);

        // Assert
        group.Repositories.Should().HaveCount(2);
        group.Repositories.Should().Contain(repo1);
        group.Repositories.Should().Contain(repo2);
        _output.WriteLine($"Group has {group.Repositories.Count} repositories");
    }

    [Fact]
    public void RepositoryGroup_GeneratesUniqueIds()
    {
        // Arrange & Act
        var group1 = new RepositoryGroup();
        var group2 = new RepositoryGroup();

        // Assert
        group1.Id.Should().NotBe(group2.Id);
        _output.WriteLine($"Group1 ID: {group1.Id}, Group2 ID: {group2.Id}");
    }

    [Fact]
    public void RepositoryGroup_CanFilterActiveRepositories()
    {
        // Arrange
        var group = new RepositoryGroup { Name = "Test Group" };
        group.Repositories.Add(new GroupedRepository { Repository = "active-1", IsActive = true });
        group.Repositories.Add(new GroupedRepository { Repository = "inactive", IsActive = false });
        group.Repositories.Add(new GroupedRepository { Repository = "active-2", IsActive = true });

        // Act
        var activeRepos = group.Repositories.Where(r => r.IsActive).ToList();

        // Assert
        activeRepos.Should().HaveCount(2);
        activeRepos.Select(r => r.Repository).Should().Contain("active-1", "active-2");
        _output.WriteLine($"Active repos: {activeRepos.Count}/{group.Repositories.Count}");
    }

    [Fact]
    public void RepositoryGroup_CanFilterIncludedRepositories()
    {
        // Arrange
        var group = new RepositoryGroup { Name = "Test Group" };
        group.Repositories.Add(new GroupedRepository { Repository = "included-1", IncludeInGeneration = true });
        group.Repositories.Add(new GroupedRepository { Repository = "excluded", IncludeInGeneration = false });
        group.Repositories.Add(new GroupedRepository { Repository = "included-2", IncludeInGeneration = true });

        // Act
        var includedRepos = group.Repositories.Where(r => r.IncludeInGeneration).ToList();

        // Assert
        includedRepos.Should().HaveCount(2);
        _output.WriteLine($"Included repos: {includedRepos.Count}/{group.Repositories.Count}");
    }
}
