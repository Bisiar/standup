using FluentAssertions;
using NSubstitute;
using Standup.Application.Interfaces;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Application.Tests.Services;

public sealed class StandupAggregatorServiceTests
{
    private readonly ISourceProviderFactory _providerFactory;
    private readonly ISourceProvider _mockProvider;
    private readonly StandupAggregatorService _service;

    public StandupAggregatorServiceTests()
    {
        _providerFactory = Substitute.For<ISourceProviderFactory>();
        _mockProvider = Substitute.For<ISourceProvider>();
        _providerFactory.GetProvider(Arg.Any<SourceType>()).Returns(_mockProvider);
        _service = new StandupAggregatorService(_providerFactory);
    }

    [Fact]
    public async Task AggregateDataAsync_WithNoRepositories_ReturnsEmptyData()
    {
        // Arrange
        var repositories = new List<SourceRepository>();
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow;

        // Act
        var result = await _service.AggregateDataAsync(repositories, since, until);

        // Assert
        result.Commits.Should().BeEmpty();
        result.PullRequests.Should().BeEmpty();
        result.WorkItems.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateDataAsync_WithSingleRepository_AggregatesData()
    {
        // Arrange
        var repository = new SourceRepository
        {
            SourceType = SourceType.GitHub,
            Organization = "test-org",
            Repository = "test-repo"
        };
        var repositories = new List<SourceRepository> { repository };
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow;

        var commits = new List<CommitInfo>
        {
            new("sha1", "feat: add feature", "john", since.AddHours(2), "test-repo")
        };

        var pullRequests = new List<PullRequestInfo>
        {
            new(1, "Add feature", "open", "john", since.AddHours(1), "test-repo", "https://github.com/test-org/test-repo/pull/1")
        };

        var workItems = new List<WorkItemInfo>
        {
            new(123, "Bug fix", "Bug", WorkItemStatus.InProgress, null, null)
        };

        _mockProvider.GetCommitsAsync(repository, since, until, Arg.Any<CancellationToken>())
            .Returns(commits);
        _mockProvider.GetOpenPullRequestsAsync(repository, Arg.Any<CancellationToken>())
            .Returns(pullRequests);
        _mockProvider.GetInProgressWorkItemsAsync(repository, Arg.Any<CancellationToken>())
            .Returns(workItems);
        _mockProvider.GetCompletedWorkItemsAsync(repository, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());

        // Act
        var result = await _service.AggregateDataAsync(repositories, since, until);

        // Assert
        result.Commits.Should().HaveCount(1);
        result.PullRequests.Should().HaveCount(1);
        result.WorkItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task AggregateDataAsync_WithMultipleRepositories_AggregatesAllData()
    {
        // Arrange
        var repo1 = new SourceRepository { SourceType = SourceType.GitHub, Repository = "repo1" };
        var repo2 = new SourceRepository { SourceType = SourceType.GitHub, Repository = "repo2" };
        var repositories = new List<SourceRepository> { repo1, repo2 };
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow;

        _mockProvider.GetCommitsAsync(Arg.Any<SourceRepository>(), since, until, Arg.Any<CancellationToken>())
            .Returns(new List<CommitInfo>
            {
                new("sha1", "commit 1", "john", since.AddHours(1), "repo")
            });

        _mockProvider.GetOpenPullRequestsAsync(Arg.Any<SourceRepository>(), Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());

        _mockProvider.GetInProgressWorkItemsAsync(Arg.Any<SourceRepository>(), Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());

        _mockProvider.GetCompletedWorkItemsAsync(Arg.Any<SourceRepository>(), since, until, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());

        // Act
        var result = await _service.AggregateDataAsync(repositories, since, until);

        // Assert
        result.Commits.Should().HaveCount(2);
    }

    [Fact]
    public async Task AggregateDataAsync_OrdersCommitsByDateDescending()
    {
        // Arrange
        var repository = new SourceRepository { SourceType = SourceType.GitHub, Repository = "test-repo" };
        var repositories = new List<SourceRepository> { repository };
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow;

        var commits = new List<CommitInfo>
        {
            new("sha1", "oldest", "john", since.AddHours(1), "test-repo"),
            new("sha3", "newest", "john", since.AddHours(5), "test-repo"),
            new("sha2", "middle", "john", since.AddHours(3), "test-repo")
        };

        _mockProvider.GetCommitsAsync(repository, since, until, Arg.Any<CancellationToken>())
            .Returns(commits);
        _mockProvider.GetOpenPullRequestsAsync(repository, Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());
        _mockProvider.GetInProgressWorkItemsAsync(repository, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());
        _mockProvider.GetCompletedWorkItemsAsync(repository, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());

        // Act
        var result = await _service.AggregateDataAsync(repositories, since, until);

        // Assert
        result.Commits.Should().HaveCount(3);
        result.Commits[0].Message.Should().Be("newest");
        result.Commits[1].Message.Should().Be("middle");
        result.Commits[2].Message.Should().Be("oldest");
    }

    [Fact]
    public async Task AggregateDataAsync_DeduplicatesWorkItems()
    {
        // Arrange
        var repository = new SourceRepository { SourceType = SourceType.GitHub, Repository = "test-repo" };
        var repositories = new List<SourceRepository> { repository };
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow;

        var inProgressItems = new List<WorkItemInfo>
        {
            new(123, "Item 1", "Bug", WorkItemStatus.InProgress, null, null)
        };

        var completedItems = new List<WorkItemInfo>
        {
            new(123, "Item 1", "Bug", WorkItemStatus.Closed, null, null),
            new(456, "Item 2", "Task", WorkItemStatus.Closed, null, null)
        };

        _mockProvider.GetCommitsAsync(repository, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<CommitInfo>());
        _mockProvider.GetOpenPullRequestsAsync(repository, Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());
        _mockProvider.GetInProgressWorkItemsAsync(repository, Arg.Any<CancellationToken>())
            .Returns(inProgressItems);
        _mockProvider.GetCompletedWorkItemsAsync(repository, since, until, Arg.Any<CancellationToken>())
            .Returns(completedItems);

        // Act
        var result = await _service.AggregateDataAsync(repositories, since, until);

        // Assert
        result.WorkItems.Should().HaveCount(2);
        result.WorkItems.Select(w => w.Id).Should().BeEquivalentTo(new[] { 123, 456 });
    }

    [Fact]
    public async Task AggregateDataAsync_UsesCorrectProviderForEachSourceType()
    {
        // Arrange
        var githubProvider = Substitute.For<ISourceProvider>();
        var azureDevOpsProvider = Substitute.For<ISourceProvider>();

        _providerFactory.GetProvider(SourceType.GitHub).Returns(githubProvider);
        _providerFactory.GetProvider(SourceType.AzureDevOps).Returns(azureDevOpsProvider);

        var githubRepo = new SourceRepository { SourceType = SourceType.GitHub, Repository = "github-repo" };
        var adoRepo = new SourceRepository { SourceType = SourceType.AzureDevOps, Repository = "ado-repo" };
        var repositories = new List<SourceRepository> { githubRepo, adoRepo };
        var since = DateTimeOffset.UtcNow.AddDays(-1);
        var until = DateTimeOffset.UtcNow;

        githubProvider.GetCommitsAsync(githubRepo, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<CommitInfo>());
        githubProvider.GetOpenPullRequestsAsync(githubRepo, Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());
        githubProvider.GetInProgressWorkItemsAsync(githubRepo, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());
        githubProvider.GetCompletedWorkItemsAsync(githubRepo, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());

        azureDevOpsProvider.GetCommitsAsync(adoRepo, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<CommitInfo>());
        azureDevOpsProvider.GetOpenPullRequestsAsync(adoRepo, Arg.Any<CancellationToken>())
            .Returns(new List<PullRequestInfo>());
        azureDevOpsProvider.GetInProgressWorkItemsAsync(adoRepo, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());
        azureDevOpsProvider.GetCompletedWorkItemsAsync(adoRepo, since, until, Arg.Any<CancellationToken>())
            .Returns(new List<WorkItemInfo>());

        // Act
        await _service.AggregateDataAsync(repositories, since, until);

        // Assert
        _providerFactory.Received(1).GetProvider(SourceType.GitHub);
        _providerFactory.Received(1).GetProvider(SourceType.AzureDevOps);
    }
}
