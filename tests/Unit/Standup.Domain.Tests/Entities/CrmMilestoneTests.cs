// <copyright file="CrmMilestoneTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="CrmMilestone"/> entity.
/// </summary>
public sealed class CrmMilestoneTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrmMilestoneTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public CrmMilestoneTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CrmMilestone_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Arrange
        var dueDate = new DateTime(2024, 6, 30);

        // Act
        var milestone = new CrmMilestone(
            MilestoneId: "ms-123",
            ProjectId: "proj-456",
            Name: "Phase 1 Complete",
            DueDate: dueDate);

        // Assert
        milestone.MilestoneId.Should().Be("ms-123");
        milestone.ProjectId.Should().Be("proj-456");
        milestone.Name.Should().Be("Phase 1 Complete");
        milestone.DueDate.Should().Be(dueDate);
        _output.WriteLine($"Created milestone: {milestone.Name}, Due: {milestone.DueDate:yyyy-MM-dd}");
    }

    [Fact]
    public void CrmMilestone_OptionalProperties_DefaultToNull()
    {
        // Arrange & Act
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow);

        // Assert
        milestone.Status.Should().BeNull();
        milestone.Description.Should().BeNull();
        milestone.PercentComplete.Should().BeNull();
        milestone.EffortEstimated.Should().BeNull();
        milestone.EffortCompleted.Should().BeNull();
        milestone.EffortRemaining.Should().BeNull();
    }

    [Fact]
    public void CrmMilestone_WithInitProperties_SetsValuesCorrectly()
    {
        // Arrange & Act
        var milestone = new CrmMilestone("ms-1", "proj-1", "Development Phase", DateTime.UtcNow.AddDays(30))
        {
            Status = "In Progress",
            Description = "Main development work",
            PercentComplete = 60m,
            EffortEstimated = 200m,
            EffortCompleted = 120m,
            EffortRemaining = 80m
        };

        // Assert
        milestone.Status.Should().Be("In Progress");
        milestone.Description.Should().Be("Main development work");
        milestone.PercentComplete.Should().Be(60m);
        milestone.EffortEstimated.Should().Be(200m);
        milestone.EffortCompleted.Should().Be(120m);
        milestone.EffortRemaining.Should().Be(80m);
        _output.WriteLine($"Milestone: {milestone.Name}, {milestone.PercentComplete}% complete");
    }

    [Fact]
    public void IsOverdue_WhenPastDueDateAndNotCompleted_ReturnsTrue()
    {
        // Arrange
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow.AddDays(-5))
        {
            Status = "In Progress"
        };

        // Act & Assert
        milestone.IsOverdue.Should().BeTrue();
        _output.WriteLine($"Milestone '{milestone.Name}' is overdue (due: {milestone.DueDate:yyyy-MM-dd})");
    }

    [Fact]
    public void IsOverdue_WhenPastDueDateButCompleted_ReturnsFalse()
    {
        // Arrange
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow.AddDays(-5))
        {
            Status = "Completed"
        };

        // Act & Assert
        milestone.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenFutureDueDate_ReturnsFalse()
    {
        // Arrange
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow.AddDays(10))
        {
            Status = "In Progress"
        };

        // Act & Assert
        milestone.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WhenNullStatus_AndOverdue_ReturnsTrue()
    {
        // Arrange
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow.AddDays(-5))
        {
            Status = null
        };

        // Act & Assert
        milestone.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void DaysUntilDue_WhenFutureDueDate_ReturnsPositive()
    {
        // Arrange
        var daysInFuture = 15;
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow.AddDays(daysInFuture));

        // Act & Assert
        milestone.DaysUntilDue.Should().BeCloseTo(daysInFuture, 1);
        _output.WriteLine($"Days until due: {milestone.DaysUntilDue}");
    }

    [Fact]
    public void DaysUntilDue_WhenPastDueDate_ReturnsNegative()
    {
        // Arrange
        var daysInPast = 10;
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow.AddDays(-daysInPast));

        // Act & Assert
        milestone.DaysUntilDue.Should().BeCloseTo(-daysInPast, 1);
        _output.WriteLine($"Days until due (overdue): {milestone.DaysUntilDue}");
    }

    [Fact]
    public void DaysUntilDue_WhenDueToday_ReturnsZero()
    {
        // Arrange
        var milestone = new CrmMilestone("ms-1", "proj-1", "Test", DateTime.UtcNow);

        // Act & Assert
        milestone.DaysUntilDue.Should().BeCloseTo(0, 1);
    }

    [Fact]
    public void CrmMilestone_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var dueDate = new DateTime(2024, 6, 30);
        var milestone1 = new CrmMilestone("ms-1", "proj-1", "Test", dueDate);
        var milestone2 = new CrmMilestone("ms-1", "proj-1", "Test", dueDate);
        var milestone3 = new CrmMilestone("ms-2", "proj-1", "Test", dueDate);

        // Act & Assert
        milestone1.Should().Be(milestone2);
        milestone1.Should().NotBe(milestone3);
    }
}
