using FluentAssertions;
using Standup.Domain.Enums;
using Standup.Infrastructure.Git;
using Xunit;

namespace Standup.Infrastructure.Tests.Git;

public class GitConfigParserTests
{
    private readonly GitConfigParser _parser;

    public GitConfigParserTests()
    {
        _parser = new GitConfigParser();
    }

    [Theory]
    [InlineData("https://github.com/microsoft/vscode", SourceType.GitHub, "microsoft", null, "vscode")]
    [InlineData("https://github.com/dotnet/runtime.git", SourceType.GitHub, "dotnet", null, "runtime")]
    [InlineData("git@github.com:facebook/react.git", SourceType.GitHub, "facebook", null, "react")]
    [InlineData("git@github.com:angular/angular", SourceType.GitHub, "angular", null, "angular")]
    public void ParseRemoteUrl_WithGitHubUrl_ReturnsCorrectInfo(
        string remoteUrl,
        SourceType expectedSourceType,
        string expectedOrg,
        string? expectedProject,
        string expectedRepo)
    {
        // Act
        var result = _parser.ParseRemoteUrl(remoteUrl);

        // Assert
        result.Should().NotBeNull();
        result!.SourceType.Should().Be(expectedSourceType);
        result.Organization.Should().Be(expectedOrg);
        result.Project.Should().Be(expectedProject);
        result.Repository.Should().Be(expectedRepo);
        result.RemoteUrl.Should().Be(remoteUrl);
    }

    [Theory]
    [InlineData("https://dev.azure.com/microsoft/vscode/_git/vscode-python", SourceType.AzureDevOps, "microsoft", "vscode", "vscode-python")]
    [InlineData("git@ssh.dev.azure.com:v3/company/MyProject/MyRepo", SourceType.AzureDevOps, "company", "MyProject", "MyRepo")]
    [InlineData("https://myorg.visualstudio.com/DefaultCollection/_git/MyRepository", SourceType.AzureDevOps, "myorg", "DefaultCollection", "MyRepository")]
    public void ParseRemoteUrl_WithAzureDevOpsUrl_ReturnsCorrectInfo(
        string remoteUrl,
        SourceType expectedSourceType,
        string expectedOrg,
        string expectedProject,
        string expectedRepo)
    {
        // Act
        var result = _parser.ParseRemoteUrl(remoteUrl);

        // Assert
        result.Should().NotBeNull();
        result!.SourceType.Should().Be(expectedSourceType);
        result.Organization.Should().Be(expectedOrg);
        result.Project.Should().Be(expectedProject);
        result.Repository.Should().Be(expectedRepo);
        result.RemoteUrl.Should().Be(remoteUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-url")]
    [InlineData("ftp://example.com/repo.git")]
    [InlineData("https://bitbucket.org/user/repo")]
    public void ParseRemoteUrl_WithInvalidUrl_ReturnsNull(string remoteUrl)
    {
        // Act
        var result = _parser.ParseRemoteUrl(remoteUrl);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseConfigContent_WithValidOriginRemote_ExtractsUrl()
    {
        // Arrange
        var configContent = @"
[core]
    repositoryformatversion = 0
    filemode = true
[remote ""origin""]
    url = https://github.com/microsoft/vscode
    fetch = +refs/heads/*:refs/remotes/origin/*
[branch ""main""]
    remote = origin
    merge = refs/heads/main";

        // Act
        var result = _parser.ParseConfigContent(configContent);

        // Assert
        result.Should().NotBeNull();
        result!.SourceType.Should().Be(SourceType.GitHub);
        result.Organization.Should().Be("microsoft");
        result.Repository.Should().Be("vscode");
    }

    [Fact]
    public void ParseConfigContent_WithTabsAndSpaces_ParsesCorrectly()
    {
        // Arrange
        var configContent = "[remote \"origin\"]\n\turl = git@github.com:dotnet/runtime.git";

        // Act
        var result = _parser.ParseConfigContent(configContent);

        // Assert
        result.Should().NotBeNull();
        result!.Organization.Should().Be("dotnet");
        result.Repository.Should().Be("runtime");
    }

    [Fact]
    public void ParseConfigContent_WithoutOriginRemote_ReturnsNull()
    {
        // Arrange
        var configContent = @"
[core]
    repositoryformatversion = 0
[remote ""upstream""]
    url = https://github.com/other/repo";

        // Act
        var result = _parser.ParseConfigContent(configContent);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseConfigContent_WithEmptyContent_ReturnsNull()
    {
        // Act
        var result = _parser.ParseConfigContent(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseConfigContent_WithMultipleRemotes_UsesOrigin()
    {
        // Arrange
        var configContent = @"
[remote ""upstream""]
    url = https://github.com/upstream/repo
[remote ""origin""]
    url = https://github.com/myorg/myrepo
[remote ""fork""]
    url = https://github.com/other/fork";

        // Act
        var result = _parser.ParseConfigContent(configContent);

        // Assert
        result.Should().NotBeNull();
        result!.Organization.Should().Be("myorg");
        result.Repository.Should().Be("myrepo");
    }

    [Fact]
    public void IsGitRepository_WithGitDirectory_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        try
        {
            // Act
            var result = _parser.IsGitRepository(tempDir);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsGitRepository_WithoutGitDirectory_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _parser.IsGitRepository(tempDir);

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseRepository_WithValidGitConfig_ReturnsInfo()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gitDir = Path.Combine(tempDir, ".git");
        Directory.CreateDirectory(gitDir);

        var configPath = Path.Combine(gitDir, "config");
        var configContent = @"
[remote ""origin""]
    url = https://github.com/test/repo
    fetch = +refs/heads/*:refs/remotes/origin/*";
        File.WriteAllText(configPath, configContent);

        try
        {
            // Act
            var result = _parser.ParseRepository(tempDir);

            // Assert
            result.Should().NotBeNull();
            result!.Organization.Should().Be("test");
            result.Repository.Should().Be("repo");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseRepository_WithoutGitConfig_ReturnsNull()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _parser.ParseRepository(tempDir);

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        // Arrange & Act
        var parser = new GitConfigParser(null);

        // Assert
        parser.Should().NotBeNull();
    }

    [Theory]
    [InlineData("https://github.com/ORG/REPO", "ORG", "REPO")]
    [InlineData("https://github.com/org/repo", "org", "repo")]
    [InlineData("https://dev.azure.com/ORG/PROJ/_git/REPO", "ORG", "REPO")]
    public void ParseRemoteUrl_PreservesCasing(string url, string expectedOrg, string expectedRepo)
    {
        // Act
        var result = _parser.ParseRemoteUrl(url);

        // Assert
        result.Should().NotBeNull();
        result!.Organization.Should().Be(expectedOrg);
        result.Repository.Should().Be(expectedRepo);
    }

    [Theory]
    [InlineData("https://github.com/org/repo.git")]
    [InlineData("https://github.com/org/repo")]
    public void ParseRemoteUrl_WithOrWithoutDotGit_ParsesCorrectly(string url)
    {
        // Act
        var result = _parser.ParseRemoteUrl(url);

        // Assert
        result.Should().NotBeNull();
        result!.Repository.Should().Be("repo");
    }
}
