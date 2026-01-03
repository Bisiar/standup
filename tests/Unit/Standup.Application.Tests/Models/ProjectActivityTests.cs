// <copyright file="ProjectActivityTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="ProjectActivity"/> record.
/// </summary>
public sealed class ProjectActivityTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectActivityTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ProjectActivityTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProjectActivity_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Act
        var activity = new ProjectActivity(
            Type: ActivityType.Commit,
            Author: "John Doe",
            Description: "Fixed login bug",
            TimeAgo: "2h ago");

        // Assert
        activity.Type.Should().Be(ActivityType.Commit);
        activity.Author.Should().Be("John Doe");
        activity.Description.Should().Be("Fixed login bug");
        activity.TimeAgo.Should().Be("2h ago");
        _output.WriteLine($"Activity: {activity.Type} by {activity.Author}");
    }

    [Fact]
    public void Icon_ForCommitActivity_ReturnsNotepadEmoji()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Commit, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.Icon.Should().Be("\U0001F4DD"); // Notepad emoji
        _output.WriteLine($"Commit icon: {activity.Icon}");
    }

    [Fact]
    public void Icon_ForTaskActivity_ReturnsCheckmarkEmoji()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Task, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.Icon.Should().Be("\u2705"); // Checkmark emoji
    }

    [Fact]
    public void Icon_ForPullRequestActivity_ReturnsBranchEmoji()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.PullRequest, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.Icon.Should().Be("\U0001F500"); // Branch/merge emoji
    }

    [Fact]
    public void Icon_ForDeploymentActivity_ReturnsRocketEmoji()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Deployment, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.Icon.Should().Be("\U0001F680"); // Rocket emoji
    }

    [Fact]
    public void BackgroundColor_ForCommitActivity_ReturnsDarkBlueColor()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Commit, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.BackgroundColor.Should().Be("#1E3A5F");
        _output.WriteLine($"Commit background: {activity.BackgroundColor}");
    }

    [Fact]
    public void BackgroundColor_ForTaskActivity_ReturnsDarkGreenColor()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Task, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.BackgroundColor.Should().Be("#14532D");
    }

    [Fact]
    public void BackgroundColor_ForPullRequestActivity_ReturnsDarkPurpleColor()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.PullRequest, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.BackgroundColor.Should().Be("#4C1D95");
    }

    [Fact]
    public void BackgroundColor_ForDeploymentActivity_ReturnsDarkRedColor()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Deployment, "Author", "Desc", "1h ago");

        // Act & Assert
        activity.BackgroundColor.Should().Be("#7F1D1D");
    }

    [Fact]
    public void FormattedText_WithAuthor_ReturnsAuthorAndDescription()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Commit, "John Doe", "Fixed bug", "1h ago");

        // Act & Assert
        activity.FormattedText.Should().Be("John Doe Fixed bug");
        _output.WriteLine($"Formatted text: {activity.FormattedText}");
    }

    [Fact]
    public void FormattedText_WithEmptyAuthor_ReturnsOnlyDescription()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Commit, string.Empty, "System update", "1h ago");

        // Act & Assert
        activity.FormattedText.Should().Be("System update");
    }

    [Fact]
    public void FormattedText_WithNullAuthor_ReturnsOnlyDescription()
    {
        // Arrange
        var activity = new ProjectActivity(ActivityType.Commit, null!, "System update", "1h ago");

        // Act & Assert
        activity.FormattedText.Should().Be("System update");
    }

    [Fact]
    public void ProjectActivity_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var activity1 = new ProjectActivity(ActivityType.Commit, "John", "Desc", "1h ago");
        var activity2 = new ProjectActivity(ActivityType.Commit, "John", "Desc", "1h ago");
        var activity3 = new ProjectActivity(ActivityType.Task, "John", "Desc", "1h ago");

        // Act & Assert
        activity1.Should().Be(activity2);
        activity1.Should().NotBe(activity3);
    }
}
