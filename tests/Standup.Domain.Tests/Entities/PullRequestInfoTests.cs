using FluentAssertions;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class PullRequestInfoTests
{
    [Fact]
    public void PullRequestInfo_CanBeCreatedWithRequiredProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var pr = new PullRequestInfo(
            Id: "123",
            Title: "Add feature X",
            Repository: "my-repo",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/repo/pull/123",
            CreatedAt: createdAt);

        // Assert
        pr.Id.Should().Be("123");
        pr.Title.Should().Be("Add feature X");
        pr.Repository.Should().Be("my-repo");
        pr.SourceType.Should().Be(SourceType.GitHub);
        pr.Status.Should().Be("open");
        pr.Url.Should().Be("https://github.com/org/repo/pull/123");
        pr.CreatedAt.Should().Be(createdAt);
        pr.Description.Should().BeNull();
        pr.IsDraft.Should().BeFalse();
        pr.ReviewerCount.Should().Be(0);
    }

    [Fact]
    public void PullRequestInfo_SupportsOptionalProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var pr = new PullRequestInfo(
            Id: "456",
            Title: "Fix bug Y",
            Repository: "my-repo",
            SourceType: SourceType.AzureDevOps,
            Status: "active",
            Url: "https://dev.azure.com/org/project/_git/repo/pullrequest/456",
            CreatedAt: createdAt,
            Description: "This PR fixes a critical bug",
            IsDraft: true,
            ReviewerCount: 3);

        // Assert
        pr.Description.Should().Be("This PR fixes a critical bug");
        pr.IsDraft.Should().BeTrue();
        pr.ReviewerCount.Should().Be(3);
    }

    [Fact]
    public void PullRequestInfo_IsRecord_SupportsEquality()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var pr1 = new PullRequestInfo(
            Id: "123",
            Title: "Add feature",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/repo/pull/123",
            CreatedAt: createdAt);

        var pr2 = new PullRequestInfo(
            Id: "123",
            Title: "Add feature",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/repo/pull/123",
            CreatedAt: createdAt);

        // Assert
        pr1.Should().Be(pr2);
        (pr1 == pr2).Should().BeTrue();
    }

    [Fact]
    public void PullRequestInfo_DifferentValues_AreNotEqual()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var pr1 = new PullRequestInfo(
            Id: "123",
            Title: "Add feature",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/repo/pull/123",
            CreatedAt: createdAt);

        var pr2 = new PullRequestInfo(
            Id: "456",
            Title: "Add feature",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/repo/pull/456",
            CreatedAt: createdAt);

        // Assert
        pr1.Should().NotBe(pr2);
        (pr1 != pr2).Should().BeTrue();
    }

    [Fact]
    public void PullRequestInfo_SupportsDeconstruction()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var pr = new PullRequestInfo(
            Id: "789",
            Title: "Update docs",
            Repository: "docs-repo",
            SourceType: SourceType.GitHub,
            Status: "merged",
            Url: "https://github.com/org/repo/pull/789",
            CreatedAt: createdAt,
            Description: "Documentation updates");

        // Act
        var (id, title, repository, sourceType, status, url, created, description, isDraft, reviewerCount) = pr;

        // Assert
        id.Should().Be("789");
        title.Should().Be("Update docs");
        repository.Should().Be("docs-repo");
        sourceType.Should().Be(SourceType.GitHub);
        status.Should().Be("merged");
        url.Should().Be("https://github.com/org/repo/pull/789");
        created.Should().Be(createdAt);
        description.Should().Be("Documentation updates");
        isDraft.Should().BeFalse();
        reviewerCount.Should().Be(0);
    }

    [Theory]
    [InlineData(SourceType.GitHub, "https://github.com/org/repo/pull/1")]
    [InlineData(SourceType.AzureDevOps, "https://dev.azure.com/org/project/_git/repo/pullrequest/1")]
    public void PullRequestInfo_SupportsDifferentSourceTypes(SourceType sourceType, string expectedUrl)
    {
        // Act
        var pr = new PullRequestInfo(
            Id: "1",
            Title: "Test PR",
            Repository: "repo",
            SourceType: sourceType,
            Status: "open",
            Url: expectedUrl,
            CreatedAt: DateTimeOffset.UtcNow);

        // Assert
        pr.SourceType.Should().Be(sourceType);
        pr.Url.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData("open")]
    [InlineData("active")]
    [InlineData("merged")]
    [InlineData("completed")]
    [InlineData("closed")]
    [InlineData("abandoned")]
    public void PullRequestInfo_SupportsDifferentStatuses(string status)
    {
        // Act
        var pr = new PullRequestInfo(
            Id: "1",
            Title: "Test PR",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            Status: status,
            Url: "https://github.com/org/repo/pull/1",
            CreatedAt: DateTimeOffset.UtcNow);

        // Assert
        pr.Status.Should().Be(status);
    }

    [Fact]
    public void PullRequestInfo_WithDescription_StoresDescription()
    {
        // Act
        var pr = new PullRequestInfo(
            Id: "1",
            Title: "Feature: Add authentication",
            Repository: "api",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/api/pull/1",
            CreatedAt: DateTimeOffset.UtcNow,
            Description: "Implements JWT-based authentication\n\nBreaking changes:\n- New auth endpoint");

        // Assert
        pr.Description.Should().Contain("JWT-based authentication");
        pr.Description.Should().Contain("Breaking changes");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void PullRequestInfo_SupportsVariousReviewerCounts(int reviewerCount)
    {
        // Act
        var pr = new PullRequestInfo(
            Id: "1",
            Title: "Test PR",
            Repository: "repo",
            SourceType: SourceType.GitHub,
            Status: "open",
            Url: "https://github.com/org/repo/pull/1",
            CreatedAt: DateTimeOffset.UtcNow,
            ReviewerCount: reviewerCount);

        // Assert
        pr.ReviewerCount.Should().Be(reviewerCount);
    }
}
