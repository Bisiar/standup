using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Application.Tests.DTOs;

public class SourceRepositoryDtoTests
{
    [Fact]
    public void CreateSourceRepositoryDto_CanBeCreatedWithRequiredFields()
    {
        // Arrange & Act
        var dto = new CreateSourceRepositoryDto(
            SourceType: SourceType.GitHub,
            Organization: "octocat",
            Project: null,
            Repository: "hello-world",
            DisplayName: "Hello World Repo",
            AuthorIdentifier: "user@example.com",
            PersonalAccessToken: "ghp_token123");

        // Assert
        dto.SourceType.Should().Be(SourceType.GitHub);
        dto.Organization.Should().Be("octocat");
        dto.Project.Should().BeNull();
        dto.Repository.Should().Be("hello-world");
        dto.DisplayName.Should().Be("Hello World Repo");
        dto.AuthorIdentifier.Should().Be("user@example.com");
        dto.PersonalAccessToken.Should().Be("ghp_token123");
        dto.DefaultBranch.Should().Be("main");
    }

    [Fact]
    public void CreateSourceRepositoryDto_CanOverrideDefaultBranch()
    {
        // Arrange & Act
        var dto = new CreateSourceRepositoryDto(
            SourceType: SourceType.AzureDevOps,
            Organization: "contoso",
            Project: "WebApp",
            Repository: "backend",
            DisplayName: null,
            AuthorIdentifier: "dev@contoso.com",
            PersonalAccessToken: null,
            DefaultBranch: "develop");

        // Assert
        dto.DefaultBranch.Should().Be("develop");
    }

    [Fact]
    public void CreateSourceRepositoryDto_ForAzureDevOps_IncludesProject()
    {
        // Arrange & Act
        var dto = new CreateSourceRepositoryDto(
            SourceType: SourceType.AzureDevOps,
            Organization: "journeyteam",
            Project: "ops",
            Repository: "infra",
            DisplayName: "Infrastructure",
            AuthorIdentifier: "jsmith",
            PersonalAccessToken: "pat123");

        // Assert
        dto.SourceType.Should().Be(SourceType.AzureDevOps);
        dto.Project.Should().Be("ops");
        dto.Organization.Should().Be("journeyteam");
        dto.Repository.Should().Be("infra");
    }

    [Fact]
    public void SourceRepositoryDto_CanBeCreated()
    {
        // Arrange & Act
        var dto = new SourceRepositoryDto(
            Id: "repo-123",
            SourceType: SourceType.GitHub,
            Organization: "microsoft",
            Project: null,
            Repository: "vscode",
            DisplayName: "VS Code",
            AuthorIdentifier: "user@microsoft.com",
            DefaultBranch: "main",
            IsActive: true);

        // Assert
        dto.Id.Should().Be("repo-123");
        dto.SourceType.Should().Be(SourceType.GitHub);
        dto.Organization.Should().Be("microsoft");
        dto.Project.Should().BeNull();
        dto.Repository.Should().Be("vscode");
        dto.DisplayName.Should().Be("VS Code");
        dto.AuthorIdentifier.Should().Be("user@microsoft.com");
        dto.DefaultBranch.Should().Be("main");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SourceRepositoryDto_IsRecord_SupportsEquality()
    {
        // Arrange
        var dto1 = new SourceRepositoryDto(
            Id: "repo-1",
            SourceType: SourceType.GitHub,
            Organization: "org",
            Project: null,
            Repository: "repo",
            DisplayName: "Display",
            AuthorIdentifier: "author",
            DefaultBranch: "main",
            IsActive: true);

        var dto2 = new SourceRepositoryDto(
            Id: "repo-1",
            SourceType: SourceType.GitHub,
            Organization: "org",
            Project: null,
            Repository: "repo",
            DisplayName: "Display",
            AuthorIdentifier: "author",
            DefaultBranch: "main",
            IsActive: true);

        // Act & Assert
        dto1.Should().Be(dto2);
        (dto1 == dto2).Should().BeTrue();
    }

    [Fact]
    public void SourceRepositoryDto_DifferentValues_AreNotEqual()
    {
        // Arrange
        var dto1 = new SourceRepositoryDto(
            Id: "repo-1",
            SourceType: SourceType.GitHub,
            Organization: "org1",
            Project: null,
            Repository: "repo",
            DisplayName: "Display",
            AuthorIdentifier: "author",
            DefaultBranch: "main",
            IsActive: true);

        var dto2 = new SourceRepositoryDto(
            Id: "repo-1",
            SourceType: SourceType.GitHub,
            Organization: "org2",
            Project: null,
            Repository: "repo",
            DisplayName: "Display",
            AuthorIdentifier: "author",
            DefaultBranch: "main",
            IsActive: true);

        // Act & Assert
        dto1.Should().NotBe(dto2);
    }

    [Fact]
    public void UpdateSourceRepositoryDto_AllFieldsOptional()
    {
        // Arrange & Act
        var dto = new UpdateSourceRepositoryDto(
            DisplayName: "Updated Name",
            AuthorIdentifier: "new-author",
            PersonalAccessToken: "new-token",
            DefaultBranch: "develop",
            IsActive: false);

        // Assert
        dto.DisplayName.Should().Be("Updated Name");
        dto.AuthorIdentifier.Should().Be("new-author");
        dto.PersonalAccessToken.Should().Be("new-token");
        dto.DefaultBranch.Should().Be("develop");
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateSourceRepositoryDto_CanBeEmpty()
    {
        // Arrange & Act
        var dto = new UpdateSourceRepositoryDto(
            DisplayName: null,
            AuthorIdentifier: null,
            PersonalAccessToken: null,
            DefaultBranch: null,
            IsActive: null);

        // Assert
        dto.DisplayName.Should().BeNull();
        dto.AuthorIdentifier.Should().BeNull();
        dto.PersonalAccessToken.Should().BeNull();
        dto.DefaultBranch.Should().BeNull();
        dto.IsActive.Should().BeNull();
    }

    [Fact]
    public void UpdateSourceRepositoryDto_IsRecord_SupportsEquality()
    {
        // Arrange
        var dto1 = new UpdateSourceRepositoryDto(
            DisplayName: "Name",
            AuthorIdentifier: "author",
            PersonalAccessToken: "token",
            DefaultBranch: "main",
            IsActive: true);

        var dto2 = new UpdateSourceRepositoryDto(
            DisplayName: "Name",
            AuthorIdentifier: "author",
            PersonalAccessToken: "token",
            DefaultBranch: "main",
            IsActive: true);

        // Act & Assert
        dto1.Should().Be(dto2);
    }
}
