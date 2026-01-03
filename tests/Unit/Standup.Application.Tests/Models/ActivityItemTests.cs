// <copyright file="ActivityItemTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="ActivityItem"/> record.
/// </summary>
public sealed class ActivityItemTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityItemTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ActivityItemTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ActivityItem_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var item = new ActivityItem(
            Description: "John committed: Fixed login bug",
            TimeAgo: "2h ago",
            Type: ActivityType.Commit,
            Timestamp: timestamp);

        // Assert
        item.Description.Should().Be("John committed: Fixed login bug");
        item.TimeAgo.Should().Be("2h ago");
        item.Type.Should().Be(ActivityType.Commit);
        item.Timestamp.Should().Be(timestamp);
        _output.WriteLine($"Activity: {item.Description} ({item.TimeAgo})");
    }

    [Theory]
    [InlineData(ActivityType.Commit)]
    [InlineData(ActivityType.PullRequest)]
    [InlineData(ActivityType.Task)]
    [InlineData(ActivityType.Deployment)]
    public void ActivityItem_AllActivityTypes_Work(ActivityType type)
    {
        // Arrange & Act
        var item = new ActivityItem("Test", "1m ago", type, DateTimeOffset.UtcNow);

        // Assert
        item.Type.Should().Be(type);
    }

    [Fact]
    public void ActivityItem_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var item1 = new ActivityItem("Desc", "1h ago", ActivityType.Commit, timestamp);
        var item2 = new ActivityItem("Desc", "1h ago", ActivityType.Commit, timestamp);
        var item3 = new ActivityItem("Different", "1h ago", ActivityType.Commit, timestamp);

        // Act & Assert
        item1.Should().Be(item2);
        item1.Should().NotBe(item3);
    }

    [Fact]
    public void ActivityItem_Timestamp_CanBeUsedForSorting()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var items = new List<ActivityItem>
        {
            new ActivityItem("Old", "3h ago", ActivityType.Commit, now.AddHours(-3)),
            new ActivityItem("Newest", "1m ago", ActivityType.Commit, now.AddMinutes(-1)),
            new ActivityItem("Middle", "1h ago", ActivityType.Task, now.AddHours(-1)),
        };

        // Act
        var sorted = items.OrderByDescending(x => x.Timestamp).ToList();

        // Assert
        sorted[0].Description.Should().Be("Newest");
        sorted[1].Description.Should().Be("Middle");
        sorted[2].Description.Should().Be("Old");
        _output.WriteLine($"Sorted by timestamp: {string.Join(", ", sorted.Select(x => x.Description))}");
    }
}
