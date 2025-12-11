using FluentAssertions;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Infrastructure.SourceProviders;
using Xunit;

namespace Standup.Infrastructure.Tests.SourceProviders;

public class AzureDevOpsSourceProviderTests
{
    // PAT for UPREHS Azure DevOps organization
    private const string Pat = "AZURE_DEVOPS_PAT_PLACEHOLDER";
    private const string Organization = "UPREHS";

    private readonly AzureDevOpsSourceProvider _provider;
    private readonly TestEncryptionService _encryptionService;

    public AzureDevOpsSourceProviderTests()
    {
        _encryptionService = new TestEncryptionService();
        _provider = new AzureDevOpsSourceProvider(_encryptionService);
    }

    [Fact]
    public async Task GetCommits_WithValidPat_ReturnsCommits()
    {
        // Arrange
        var repository = CreateRepository();
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        // Act
        var commits = await _provider.GetCommitsAsync(repository, since, until);

        // Assert
        commits.Should().NotBeNull();
        Console.WriteLine($"Found {commits.Count()} commits in the last 7 days");
        foreach (var commit in commits.Take(5))
        {
            Console.WriteLine($"  - {commit.CommittedAt:yyyy-MM-dd}: {commit.Message?.Split('\n').FirstOrDefault()}");
        }
    }

    [Fact]
    public async Task GetInProgressWorkItems_WithValidPat_ReturnsWorkItems()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var workItems = await _provider.GetInProgressWorkItemsAsync(repository);

        // Assert
        workItems.Should().NotBeNull();
        Console.WriteLine($"Found {workItems.Count()} in-progress work items");
        foreach (var item in workItems.Take(10))
        {
            Console.WriteLine($"  - [{item.Type}] {item.Title} ({item.Status})");
        }
    }

    [Fact]
    public async Task GetCompletedWorkItems_WithValidPat_ReturnsCompletedItems()
    {
        // Arrange
        var repository = CreateRepository();
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        // Act
        var workItems = await _provider.GetCompletedWorkItemsAsync(repository, since, until);

        // Assert
        workItems.Should().NotBeNull();
        Console.WriteLine($"Found {workItems.Count()} completed work items in the last 7 days");
        foreach (var item in workItems.Take(10))
        {
            Console.WriteLine($"  - [{item.Type}] {item.Title}");
        }
    }

    [Fact]
    public async Task GetOpenPullRequests_WithValidPat_ReturnsPRs()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var prs = await _provider.GetOpenPullRequestsAsync(repository);

        // Assert
        prs.Should().NotBeNull();
        Console.WriteLine($"Found {prs.Count()} open pull requests");
        foreach (var pr in prs.Take(5))
        {
            Console.WriteLine($"  - PR #{pr.Id}: {pr.Title}");
        }
    }

    [Fact]
    public async Task ValidateConnection_WithValidPat_ReturnsTrue()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var isValid = await _provider.ValidateConnectionAsync(repository);

        // Assert
        isValid.Should().BeTrue();
        Console.WriteLine("Connection validated successfully!");
    }

    [Fact]
    public async Task GenerateTomorrowsStandup_IntegrationTest()
    {
        // Arrange
        var repository = CreateRepository();
        var since = DateTimeOffset.UtcNow.AddDays(-1); // Yesterday
        var until = DateTimeOffset.UtcNow;

        Console.WriteLine("=== Tomorrow's Standup Preview ===");
        Console.WriteLine($"Period: {since:yyyy-MM-dd} to {until:yyyy-MM-dd}");
        Console.WriteLine($"Organization: {Organization}");
        Console.WriteLine();

        // Get all activity
        var commits = (await _provider.GetCommitsAsync(repository, since, until)).ToList();
        var completedWorkItems = (await _provider.GetCompletedWorkItemsAsync(repository, since, until)).ToList();
        var inProgressWorkItems = (await _provider.GetInProgressWorkItemsAsync(repository)).ToList();
        var openPRs = (await _provider.GetOpenPullRequestsAsync(repository)).ToList();

        // Output standup format
        Console.WriteLine("## What I completed yesterday:");
        if (commits.Any() || completedWorkItems.Any())
        {
            foreach (var commit in commits)
            {
                Console.WriteLine($"- Committed: {commit.Message?.Split('\n').FirstOrDefault()}");
            }
            foreach (var item in completedWorkItems)
            {
                Console.WriteLine($"- Completed [{item.Type}]: {item.Title}");
            }
        }
        else
        {
            Console.WriteLine("- No completed items");
        }

        Console.WriteLine();
        Console.WriteLine("## What I'm working on today:");
        if (inProgressWorkItems.Any())
        {
            foreach (var item in inProgressWorkItems)
            {
                Console.WriteLine($"- [{item.Type}] {item.Title}");
            }
        }
        else
        {
            Console.WriteLine("- No items in progress");
        }

        Console.WriteLine();
        Console.WriteLine("## Open Pull Requests:");
        if (openPRs.Any())
        {
            foreach (var pr in openPRs)
            {
                Console.WriteLine($"- PR #{pr.Id}: {pr.Title}");
            }
        }
        else
        {
            Console.WriteLine("- No open PRs");
        }

        Console.WriteLine();
        Console.WriteLine($"=== Summary: {commits.Count} commits, {completedWorkItems.Count} completed items, {inProgressWorkItems.Count} in progress, {openPRs.Count} open PRs ===");
    }

    private SourceRepository CreateRepository()
    {
        return new SourceRepository
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user",
            SourceType = SourceType.AzureDevOps,
            Organization = Organization,
            Project = "AI-Chat-Bot",
            Repository = "AI-Chat-Bot",
            AuthorIdentifier = "", // Empty to get all authors
            EncryptedPat = _encryptionService.Encrypt(Pat),
            IsActive = true
        };
    }

    // Simple test encryption service that doesn't actually encrypt
    private class TestEncryptionService : IEncryptionService
    {
        public string Encrypt(string plainText) => plainText;
        public Task<string> EncryptAsync(string plainText) => Task.FromResult(plainText);
        public Task<string> DecryptAsync(string cipherText) => Task.FromResult(cipherText);
    }
}
