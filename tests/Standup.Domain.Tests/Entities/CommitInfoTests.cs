using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class CommitInfoTests
{
    [Fact]
    public void CommitInfo_CanBeCreatedWithRequiredProperties()
    {
        // Arrange
        var committedAt = DateTimeOffset.UtcNow;

        // Act
        var commit = new CommitInfo(
            Sha: "abc123",
            Message: "Fix bug in authentication",
            Repository: "my-app",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt);

        // Assert
        commit.Sha.Should().Be("abc123");
        commit.Message.Should().Be("Fix bug in authentication");
        commit.Repository.Should().Be("my-app");
        commit.SourceType.Should().Be(SourceType.GitHub);
        commit.CommittedAt.Should().Be(committedAt);
        commit.FilesChanged.Should().NotBeNull();
        commit.FilesChanged.Should().BeEmpty();
        commit.Additions.Should().Be(0);
        commit.Deletions.Should().Be(0);
    }

    [Fact]
    public void CommitInfo_SupportsOptionalProperties()
    {
        // Arrange
        var filesChanged = new List<string> { "src/Auth.cs", "tests/AuthTests.cs" };
        var committedAt = DateTimeOffset.UtcNow;

        // Act
        var commit = new CommitInfo(
            Sha: "def456",
            Message: "Add new feature",
            Repository: "backend",
            SourceType: SourceType.AzureDevOps,
            CommittedAt: committedAt,
            FilesChanged: filesChanged,
            Additions: 45,
            Deletions: 12);

        // Assert
        commit.FilesChanged.Should().HaveCount(2);
        commit.FilesChanged.Should().Contain("src/Auth.cs");
        commit.FilesChanged.Should().Contain("tests/AuthTests.cs");
        commit.Additions.Should().Be(45);
        commit.Deletions.Should().Be(12);
    }

    [Fact]
    public void CommitInfo_WithNullFilesChanged_InitializesEmptyList()
    {
        // Act
        var commit = new CommitInfo(
            Sha: "xyz789",
            Message: "Update README",
            Repository: "docs",
            SourceType: SourceType.GitHub,
            CommittedAt: DateTimeOffset.UtcNow,
            FilesChanged: null);

        // Assert
        commit.FilesChanged.Should().NotBeNull();
        commit.FilesChanged.Should().BeEmpty();
    }

    [Fact]
    public void CommitInfo_IsRecord_SupportsEquality()
    {
        // Arrange
        var committedAt = DateTimeOffset.UtcNow;
        var files = new List<string> { "file1.cs", "file2.cs" };

        var commit1 = new CommitInfo(
            Sha: "aaa111",
            Message: "Same commit",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt,
            FilesChanged: files,
            Additions: 10,
            Deletions: 5);

        var commit2 = new CommitInfo(
            Sha: "aaa111",
            Message: "Same commit",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt,
            FilesChanged: files,
            Additions: 10,
            Deletions: 5);

        // Assert
        commit1.Should().Be(commit2);
        (commit1 == commit2).Should().BeTrue();
    }

    [Fact]
    public void CommitInfo_DifferentValues_AreNotEqual()
    {
        // Arrange
        var committedAt = DateTimeOffset.UtcNow;

        var commit1 = new CommitInfo(
            Sha: "aaa111",
            Message: "Commit 1",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt);

        var commit2 = new CommitInfo(
            Sha: "bbb222",
            Message: "Commit 2",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt);

        // Assert
        commit1.Should().NotBe(commit2);
        (commit1 != commit2).Should().BeTrue();
    }

    [Fact]
    public void CommitInfo_SupportsDeconstruction()
    {
        // Arrange
        var committedAt = DateTimeOffset.UtcNow;
        var files = new List<string> { "Program.cs" };

        var commit = new CommitInfo(
            Sha: "commit123",
            Message: "Initial commit",
            Repository: "new-repo",
            SourceType: SourceType.GitHub,
            CommittedAt: committedAt,
            FilesChanged: files,
            Additions: 100,
            Deletions: 0);

        // Act
        var (sha, message, repository, sourceType, committed, filesChanged, additions, deletions) = commit;

        // Assert
        sha.Should().Be("commit123");
        message.Should().Be("Initial commit");
        repository.Should().Be("new-repo");
        sourceType.Should().Be(SourceType.GitHub);
        committed.Should().Be(committedAt);
        filesChanged.Should().HaveCount(1);
        additions.Should().Be(100);
        deletions.Should().Be(0);
    }

    [Theory]
    [InlineData(SourceType.GitHub)]
    [InlineData(SourceType.AzureDevOps)]
    public void CommitInfo_SupportsDifferentSourceTypes(SourceType sourceType)
    {
        // Act
        var commit = new CommitInfo(
            Sha: "sha123",
            Message: "Test commit",
            Repository: "test-repo",
            SourceType: sourceType,
            CommittedAt: DateTimeOffset.UtcNow);

        // Assert
        commit.SourceType.Should().Be(sourceType);
    }

    [Fact]
    public void CommitInfo_CanHaveMultipleFilesChanged()
    {
        // Arrange
        var files = new List<string>
        {
            "src/Services/UserService.cs",
            "src/Controllers/UserController.cs",
            "tests/UserServiceTests.cs",
            "tests/UserControllerTests.cs",
            "README.md"
        };

        // Act
        var commit = new CommitInfo(
            Sha: "multi-file-commit",
            Message: "Refactor user management",
            Repository: "backend-api",
            SourceType: SourceType.AzureDevOps,
            CommittedAt: DateTimeOffset.UtcNow,
            FilesChanged: files,
            Additions: 250,
            Deletions: 100);

        // Assert
        commit.FilesChanged.Should().HaveCount(5);
        commit.FilesChanged.Should().ContainInOrder(files);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 0)]
    [InlineData(0, 5)]
    [InlineData(100, 50)]
    public void CommitInfo_SupportsVariousAdditionsAndDeletions(int additions, int deletions)
    {
        // Act
        var commit = new CommitInfo(
            Sha: "test",
            Message: "Test",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            CommittedAt: DateTimeOffset.UtcNow,
            Additions: additions,
            Deletions: deletions);

        // Assert
        commit.Additions.Should().Be(additions);
        commit.Deletions.Should().Be(deletions);
    }

    [Fact]
    public void CommitInfo_WithLongMessage_StoresFullMessage()
    {
        // Arrange
        var longMessage = @"feat: Add comprehensive user authentication

This commit implements:
- JWT token generation
- Refresh token support
- Password hashing with bcrypt
- Email verification flow
- Password reset functionality

Breaking changes:
- Old auth endpoints are deprecated

Closes #123, #124, #125";

        // Act
        var commit = new CommitInfo(
            Sha: "feature-commit",
            Message: longMessage,
            Repository: "auth-service",
            SourceType: SourceType.GitHub,
            CommittedAt: DateTimeOffset.UtcNow);

        // Assert
        commit.Message.Should().Be(longMessage);
        commit.Message.Should().Contain("JWT token generation");
        commit.Message.Should().Contain("Breaking changes");
    }

    [Fact]
    public void CommitInfo_WithTimestamp_PreservesExactTime()
    {
        // Arrange
        var exactTime = new DateTimeOffset(2025, 12, 14, 10, 30, 45, TimeSpan.Zero);

        // Act
        var commit = new CommitInfo(
            Sha: "time-test",
            Message: "Test commit",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            CommittedAt: exactTime);

        // Assert
        commit.CommittedAt.Should().Be(exactTime);
        commit.CommittedAt.Hour.Should().Be(10);
        commit.CommittedAt.Minute.Should().Be(30);
        commit.CommittedAt.Second.Should().Be(45);
    }
}
