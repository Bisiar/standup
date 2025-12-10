using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Domain.Tests.Entities;

public sealed class SourceRepositoryTests
{
    [Fact]
    public void NewSourceRepository_HasDefaultValues()
    {
        // Act
        var repo = new SourceRepository();

        // Assert
        repo.Id.Should().NotBeNullOrEmpty();
        repo.UserId.Should().BeEmpty();
        repo.SourceType.Should().Be(SourceType.GitHub);
        repo.Organization.Should().BeEmpty();
        repo.Project.Should().BeNull();
        repo.Repository.Should().BeEmpty();
        repo.DisplayName.Should().BeNull();
        repo.AuthorIdentifier.Should().BeEmpty();
        repo.EncryptedPat.Should().BeNull();
        repo.DefaultBranch.Should().Be("main");
        repo.IsActive.Should().BeTrue();
        repo.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        repo.User.Should().BeNull();
    }

    [Fact]
    public void FullPath_ForGitHub_ReturnsOrgSlashRepo()
    {
        // Arrange
        var repo = new SourceRepository
        {
            SourceType = SourceType.GitHub,
            Organization = "microsoft",
            Repository = "dotnet"
        };

        // Act
        var fullPath = repo.FullPath;

        // Assert
        fullPath.Should().Be("microsoft/dotnet");
    }

    [Fact]
    public void FullPath_ForAzureDevOps_ReturnsOrgSlashProjectSlashRepo()
    {
        // Arrange
        var repo = new SourceRepository
        {
            SourceType = SourceType.AzureDevOps,
            Organization = "journeyteam",
            Project = "my-project",
            Repository = "my-repo"
        };

        // Act
        var fullPath = repo.FullPath;

        // Assert
        fullPath.Should().Be("journeyteam/my-project/my-repo");
    }

    [Fact]
    public void SourceRepository_CanSetProperties()
    {
        // Arrange
        var repo = new SourceRepository();

        // Act
        repo.UserId = "user-123";
        repo.SourceType = SourceType.AzureDevOps;
        repo.Organization = "journeyteam";
        repo.Project = "my-project";
        repo.Repository = "my-repo";
        repo.DisplayName = "My Repository";
        repo.AuthorIdentifier = "john.doe@company.com";
        repo.EncryptedPat = "encrypted-pat-data";
        repo.DefaultBranch = "develop";
        repo.IsActive = false;

        // Assert
        repo.UserId.Should().Be("user-123");
        repo.SourceType.Should().Be(SourceType.AzureDevOps);
        repo.Organization.Should().Be("journeyteam");
        repo.Project.Should().Be("my-project");
        repo.Repository.Should().Be("my-repo");
        repo.DisplayName.Should().Be("My Repository");
        repo.AuthorIdentifier.Should().Be("john.doe@company.com");
        repo.EncryptedPat.Should().Be("encrypted-pat-data");
        repo.DefaultBranch.Should().Be("develop");
        repo.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SourceRepository_CanHaveUser()
    {
        // Arrange
        var user = new User { DisplayName = "John Doe" };
        var repo = new SourceRepository { Repository = "my-repo" };

        // Act
        repo.User = user;
        repo.UserId = user.Id;

        // Assert
        repo.User.Should().Be(user);
        repo.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void SourceRepository_GeneratesUniqueIds()
    {
        // Arrange & Act
        var repo1 = new SourceRepository();
        var repo2 = new SourceRepository();

        // Assert
        repo1.Id.Should().NotBe(repo2.Id);
    }

    [Theory]
    [InlineData(SourceType.GitHub, "octocat", null, "hello-world", "octocat/hello-world")]
    [InlineData(SourceType.GitHub, "microsoft", null, "vscode", "microsoft/vscode")]
    [InlineData(SourceType.AzureDevOps, "contoso", "web-app", "frontend", "contoso/web-app/frontend")]
    [InlineData(SourceType.AzureDevOps, "journeyteam", "ops", "infra", "journeyteam/ops/infra")]
    public void FullPath_ReturnsCorrectFormat(
        SourceType sourceType,
        string organization,
        string? project,
        string repository,
        string expectedPath)
    {
        // Arrange
        var repo = new SourceRepository
        {
            SourceType = sourceType,
            Organization = organization,
            Project = project,
            Repository = repository
        };

        // Act
        var fullPath = repo.FullPath;

        // Assert
        fullPath.Should().Be(expectedPath);
    }
}
