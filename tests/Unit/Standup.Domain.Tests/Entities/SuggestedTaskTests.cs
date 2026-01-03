// <copyright file="SuggestedTaskTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="SuggestedTask"/> entity.
/// </summary>
public sealed class SuggestedTaskTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuggestedTaskTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public SuggestedTaskTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SuggestedTask_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Act
        var task = new SuggestedTask(
            Title: "Review proposal",
            Description: "Review and approve the project proposal by end of day",
            Priority: "High",
            SourceEmailId: "email-123");

        // Assert
        task.Title.Should().Be("Review proposal");
        task.Description.Should().Be("Review and approve the project proposal by end of day");
        task.Priority.Should().Be("High");
        task.SourceEmailId.Should().Be("email-123");
        _output.WriteLine($"Task: {task.Title}, Priority: {task.Priority}");
    }

    [Fact]
    public void IsHighPriority_WhenPriorityIsHigh_ReturnsTrue()
    {
        // Arrange
        var task = new SuggestedTask("Test", "Description", "High", "email-1");

        // Act & Assert
        task.IsHighPriority.Should().BeTrue();
    }

    [Fact]
    public void IsHighPriority_WhenPriorityIsHighCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var taskLower = new SuggestedTask("Test", "Description", "high", "email-1");
        var taskUpper = new SuggestedTask("Test", "Description", "HIGH", "email-1");
        var taskMixed = new SuggestedTask("Test", "Description", "HiGh", "email-1");

        // Act & Assert
        taskLower.IsHighPriority.Should().BeTrue();
        taskUpper.IsHighPriority.Should().BeTrue();
        taskMixed.IsHighPriority.Should().BeTrue();
    }

    [Fact]
    public void IsHighPriority_WhenPriorityIsMedium_ReturnsFalse()
    {
        // Arrange
        var task = new SuggestedTask("Test", "Description", "Medium", "email-1");

        // Act & Assert
        task.IsHighPriority.Should().BeFalse();
    }

    [Fact]
    public void IsHighPriority_WhenPriorityIsLow_ReturnsFalse()
    {
        // Arrange
        var task = new SuggestedTask("Test", "Description", "Low", "email-1");

        // Act & Assert
        task.IsHighPriority.Should().BeFalse();
    }

    [Fact]
    public void TruncatedDescription_WhenShort_ReturnsFullDescription()
    {
        // Arrange
        var task = new SuggestedTask("Test", "Short description", "Medium", "email-1");

        // Act & Assert
        task.TruncatedDescription.Should().Be("Short description");
    }

    [Fact]
    public void TruncatedDescription_WhenExactly100Chars_ReturnsFullDescription()
    {
        // Arrange
        var description = new string('X', 100);
        var task = new SuggestedTask("Test", description, "Medium", "email-1");

        // Act & Assert
        task.TruncatedDescription.Should().Be(description);
    }

    [Fact]
    public void TruncatedDescription_WhenLong_TruncatesWithEllipsis()
    {
        // Arrange
        var description = new string('X', 150);
        var task = new SuggestedTask("Test", description, "Medium", "email-1");

        // Act
        var truncated = task.TruncatedDescription;

        // Assert
        truncated.Should().HaveLength(100);
        truncated.Should().EndWith("...");
        _output.WriteLine($"Truncated description length: {truncated.Length}");
    }

    [Fact]
    public void SuggestedTask_AllPriorityLevels_Work()
    {
        // Arrange
        var highTask = new SuggestedTask("High", "Desc", "High", "e1");
        var mediumTask = new SuggestedTask("Medium", "Desc", "Medium", "e1");
        var lowTask = new SuggestedTask("Low", "Desc", "Low", "e1");

        // Assert
        highTask.Priority.Should().Be("High");
        mediumTask.Priority.Should().Be("Medium");
        lowTask.Priority.Should().Be("Low");
    }

    [Fact]
    public void SuggestedTask_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var task1 = new SuggestedTask("Test", "Desc", "High", "e1");
        var task2 = new SuggestedTask("Test", "Desc", "High", "e1");
        var task3 = new SuggestedTask("Different", "Desc", "High", "e1");

        // Act & Assert
        task1.Should().Be(task2);
        task1.Should().NotBe(task3);
    }
}
