// <copyright file="ProjectTaskTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="ProjectTask"/> class.
/// </summary>
public sealed class ProjectTaskTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectTaskTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ProjectTaskTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProjectTask_WithConstructorParameters_SetsValuesCorrectly()
    {
        // Arrange
        var dueDate = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var task = new ProjectTask(
            id: "task-123",
            title: "Implement login feature",
            assigneeInitials: "JD",
            assigneeName: "John Doe",
            priority: TaskPriority.High,
            dueDate: dueDate);

        // Assert
        task.Id.Should().Be("task-123");
        task.Title.Should().Be("Implement login feature");
        task.AssigneeInitials.Should().Be("JD");
        task.AssigneeName.Should().Be("John Doe");
        task.Priority.Should().Be(TaskPriority.High);
        task.DueDate.Should().Be(dueDate);
        _output.WriteLine($"Task: {task.Title}, Priority: {task.Priority}");
    }

    [Fact]
    public void ProjectTask_IsOverdue_DefaultsToFalse()
    {
        // Arrange & Act
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Medium, DateTimeOffset.UtcNow);

        // Assert
        task.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void ProjectTask_IsOverdue_CanBeSetToTrue()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Medium, DateTimeOffset.UtcNow);

        // Act
        task.IsOverdue = true;

        // Assert
        task.IsOverdue.Should().BeTrue();
    }

    [Theory]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.Low)]
    public void PriorityText_ReturnsCorrectString(TaskPriority priority)
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", priority, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityText.Should().Be(priority.ToString());
    }

    [Fact]
    public void PriorityColor_ForHighPriority_ReturnsRedColor()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.High, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityColor.Should().Be("#EF4444");
        _output.WriteLine($"High priority color: {task.PriorityColor}");
    }

    [Fact]
    public void PriorityColor_ForMediumPriority_ReturnsAmberColor()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Medium, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityColor.Should().Be("#F59E0B");
    }

    [Fact]
    public void PriorityColor_ForLowPriority_ReturnsGreenColor()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Low, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityColor.Should().Be("#22C55E");
    }

    [Fact]
    public void PriorityBackground_ForHighPriority_ReturnsDarkRedColor()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.High, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityBackground.Should().Be("#7F1D1D");
    }

    [Fact]
    public void PriorityBackground_ForMediumPriority_ReturnsDarkAmberColor()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Medium, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityBackground.Should().Be("#78350F");
    }

    [Fact]
    public void PriorityBackground_ForLowPriority_ReturnsDarkGreenColor()
    {
        // Arrange
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Low, DateTimeOffset.UtcNow);

        // Act & Assert
        task.PriorityBackground.Should().Be("#14532D");
    }

    [Fact]
    public void DueDateText_WhenNotOverdue_ShowsCalendarEmoji()
    {
        // Arrange
        var dueDate = DateTimeOffset.UtcNow.AddDays(5);
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Medium, dueDate)
        {
            IsOverdue = false
        };

        // Act & Assert
        task.DueDateText.Should().StartWith("\U0001F4C5"); // Calendar emoji
        task.DueDateText.Should().Contain(dueDate.ToString("MMM dd"));
        _output.WriteLine($"Due date text: {task.DueDateText}");
    }

    [Fact]
    public void DueDateText_WhenOverdue_ShowsWarningEmoji()
    {
        // Arrange
        var dueDate = DateTimeOffset.UtcNow.AddDays(-3);
        var task = new ProjectTask("1", "Test", "JD", "John", TaskPriority.Medium, dueDate)
        {
            IsOverdue = true
        };

        // Act & Assert
        task.DueDateText.Should().StartWith("\u26A0\uFE0F"); // Warning emoji
        task.DueDateText.Should().Contain(dueDate.ToString("MMM dd"));
        _output.WriteLine($"Overdue text: {task.DueDateText}");
    }

    [Fact]
    public void ProjectTask_PropertiesCanBeModified()
    {
        // Arrange
        var task = new ProjectTask("1", "Original", "XX", "Nobody", TaskPriority.Low, DateTimeOffset.UtcNow);

        // Act
        task.Id = "2";
        task.Title = "Updated";
        task.AssigneeInitials = "JD";
        task.AssigneeName = "John Doe";
        task.Priority = TaskPriority.High;
        task.DueDate = DateTimeOffset.UtcNow.AddDays(30);

        // Assert
        task.Id.Should().Be("2");
        task.Title.Should().Be("Updated");
        task.AssigneeInitials.Should().Be("JD");
        task.AssigneeName.Should().Be("John Doe");
        task.Priority.Should().Be(TaskPriority.High);
    }
}
