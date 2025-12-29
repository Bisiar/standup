using FluentAssertions;
using Standup.Infrastructure.Git;
using Xunit;

namespace Standup.Infrastructure.Tests.Git;

public class LocalGitServiceTests : IDisposable
{
    private readonly LocalGitService _service;
    private readonly string _testRepoPath;
    private bool _disposed;

    public LocalGitServiceTests()
    {
        _service = new LocalGitService();
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);

        // Initialize a test git repository
        InitializeTestRepository();
    }

    [Fact]
    public async Task GetCommitsAsync_WithValidRepo_ReturnsCommits()
    {
        // Act
        var commits = await _service.GetCommitsAsync(_testRepoPath);

        // Assert
        commits.Should().NotBeEmpty();
        commits.Should().HaveCountGreaterThanOrEqualTo(1);
        commits.First().Subject.Should().Be("Initial commit");
        commits.First().AuthorName.Should().Be("Test User");
        commits.First().AuthorEmail.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCommitsAsync_WithNonExistentPath_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var commits = await _service.GetCommitsAsync(nonExistentPath);

        // Assert
        commits.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommitsAsync_WithMaxCount_LimitsResults()
    {
        // Arrange - create multiple commits
        for (int i = 0; i < 5; i++)
        {
            var testFile = Path.Combine(_testRepoPath, $"file{i}.txt");
            await File.WriteAllTextAsync(testFile, $"Content {i}");
            RunGitCommand($"add file{i}.txt");
            RunGitCommand($"commit -m \"Commit {i}\"");
        }

        // Act
        var commits = await _service.GetCommitsAsync(_testRepoPath, maxCount: 3);

        // Assert
        commits.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCommitsAsync_WithAuthorFilter_ReturnsOnlyMatchingCommits()
    {
        // Act
        var commits = await _service.GetCommitsAsync(
            _testRepoPath,
            authorIdentifier: "Test User");

        // Assert
        commits.Should().NotBeEmpty();
        commits.Should().OnlyContain(c => c.AuthorName == "Test User");
    }

    [Fact]
    public async Task GetCommitsAsync_WithDateRange_ReturnsCommitsInRange()
    {
        // Arrange
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var commits = await _service.GetCommitsAsync(
            _testRepoPath,
            since: since,
            until: until);

        // Assert
        commits.Should().NotBeEmpty();
        commits.Should().OnlyContain(c => c.CommitDate >= since && c.CommitDate <= until);
    }

    [Fact]
    public async Task GetHeadShaAsync_WithValidRepo_ReturnsSha()
    {
        // Act
        var sha = await _service.GetHeadShaAsync(_testRepoPath);

        // Assert
        sha.Should().NotBeNullOrWhiteSpace();
        sha.Should().HaveLength(40); // Full SHA is 40 characters
        sha.Should().MatchRegex("^[0-9a-f]{40}$");
    }

    [Fact]
    public async Task GetHeadShaAsync_WithNonExistentRepo_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var sha = await _service.GetHeadShaAsync(nonExistentPath);

        // Assert
        sha.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WithValidRepo_ReturnsBranchName()
    {
        // Act
        var branch = await _service.GetCurrentBranchAsync(_testRepoPath);

        // Assert
        branch.Should().NotBeNullOrWhiteSpace();

        // Default branch is typically 'main' or 'master'
        branch.Should().Match(b => b == "main" || b == "master");
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WithNonExistentRepo_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var branch = await _service.GetCurrentBranchAsync(nonExistentPath);

        // Assert
        branch.Should().BeNull();
    }

    [Fact]
    public async Task GetAuthorsAsync_WithNonExistentRepo_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var authors = await _service.GetAuthorsAsync(nonExistentPath);

        // Assert
        authors.Should().BeEmpty();
    }

    [Fact]
    public void GetRepositoryName_WithValidPath_ReturnsDirectoryName()
    {
        // Act
        var name = LocalGitService.GetRepositoryName(_testRepoPath);

        // Assert
        name.Should().NotBeNullOrEmpty();
        name.Should().Be(new DirectoryInfo(_testRepoPath).Name);
    }

    [Fact]
    public async Task GetCommitsAsync_ParsesCommitFieldsCorrectly()
    {
        // Arrange - create a commit with known details
        var testFile = Path.Combine(_testRepoPath, "detailed.txt");
        await File.WriteAllTextAsync(testFile, "Detailed content");
        RunGitCommand("add detailed.txt");
        RunGitCommand("commit -m \"Test subject\" -m \"Test body line 1\" -m \"Test body line 2\"");

        // Act
        var commits = await _service.GetCommitsAsync(_testRepoPath, maxCount: 1);

        // Assert
        var commit = commits.First();
        commit.Subject.Should().Be("Test subject");
        commit.Body.Should().Contain("Test body");
        commit.Sha.Should().HaveLength(40);
        commit.ShortSha.Should().HaveLength(7);
        commit.CommitDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task IsInSyncWithRemoteAsync_WithoutRemote_ReturnsFalse()
    {
        // Act
        var isInSync = await _service.IsInSyncWithRemoteAsync(_testRepoPath);

        // Assert
        // Local-only repo without remote should return false
        isInSync.Should().BeFalse();
    }

    [Fact]
    public async Task GetCommitsAsync_WithEmptyRepo_ReturnsEmptyList()
    {
        // Arrange - create a new empty repo
        var emptyRepoPath = Path.Combine(Path.GetTempPath(), $"git-empty-{Guid.NewGuid()}");
        Directory.CreateDirectory(emptyRepoPath);

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = emptyRepoPath,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }

        try
        {
            // Act
            var commits = await _service.GetCommitsAsync(emptyRepoPath);

            // Assert
            commits.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(emptyRepoPath, true);
        }
    }

    [Fact]
    public async Task GetCommitsAsync_WithPastDateRange_ReturnsEmptyList()
    {
        // Arrange - date range in the past (before any commits)
        var since = DateTimeOffset.UtcNow.AddYears(-10);
        var until = DateTimeOffset.UtcNow.AddYears(-9);

        // Act
        var commits = await _service.GetCommitsAsync(
            _testRepoPath,
            since: since,
            until: until);

        // Assert
        commits.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void InitializeTestRepository()
    {
        // Initialize git repo
        RunGitCommand("init");
        RunGitCommand("config user.name \"Test User\"");
        RunGitCommand("config user.email \"test@example.com\"");

        // Create initial commit
        var testFile = Path.Combine(_testRepoPath, "test.txt");
        File.WriteAllText(testFile, "Initial content");
        RunGitCommand("add test.txt");
        RunGitCommand("commit -m \"Initial commit\"");
    }

    private void RunGitCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();
    }
}
