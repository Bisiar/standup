// <copyright file="ProjectPhaseTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="ProjectPhase"/> record.
/// </summary>
public sealed class ProjectPhaseTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPhaseTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ProjectPhaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProjectPhase_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Act
        var phase = new ProjectPhase(
            Name: "Development",
            Dates: "Jan 1 - Mar 31",
            Status: PhaseStatus.InProgress);

        // Assert
        phase.Name.Should().Be("Development");
        phase.Dates.Should().Be("Jan 1 - Mar 31");
        phase.Status.Should().Be(PhaseStatus.InProgress);
        _output.WriteLine($"Phase: {phase.Name} ({phase.Status})");
    }

    [Fact]
    public void ProjectPhase_OptionalEffortProperties_DefaultToNull()
    {
        // Arrange & Act
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.Pending);

        // Assert
        phase.EffortEstimated.Should().BeNull();
        phase.EffortCompleted.Should().BeNull();
        phase.EffortRemaining.Should().BeNull();
    }

    [Fact]
    public void ProjectPhase_WithEffortData_SetsValuesCorrectly()
    {
        // Act
        var phase = new ProjectPhase("Development", "Jan - Mar", PhaseStatus.InProgress)
        {
            EffortEstimated = 200m,
            EffortCompleted = 120m,
            EffortRemaining = 80m
        };

        // Assert
        phase.EffortEstimated.Should().Be(200m);
        phase.EffortCompleted.Should().Be(120m);
        phase.EffortRemaining.Should().Be(80m);
        _output.WriteLine($"Effort: {phase.EffortCompleted}/{phase.EffortEstimated} hours");
    }

    [Fact]
    public void EffortDisplay_WithCompletedEffort_ReturnsFormattedString()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.InProgress)
        {
            EffortCompleted = 150m
        };

        // Act & Assert
        phase.EffortDisplay.Should().Be("150h completed");
        _output.WriteLine($"Effort display: {phase.EffortDisplay}");
    }

    [Fact]
    public void EffortDisplay_WithZeroEffort_ReturnsEmptyString()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.Pending)
        {
            EffortCompleted = 0m
        };

        // Act & Assert
        phase.EffortDisplay.Should().BeEmpty();
    }

    [Fact]
    public void EffortDisplay_WithNullEffort_ReturnsEmptyString()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.Pending)
        {
            EffortCompleted = null
        };

        // Act & Assert
        phase.EffortDisplay.Should().BeEmpty();
    }

    [Fact]
    public void EffortDisplay_WithNegativeEffort_ReturnsEmptyString()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.Pending)
        {
            EffortCompleted = -10m
        };

        // Act & Assert
        phase.EffortDisplay.Should().BeEmpty();
    }

    [Fact]
    public void HasEffortData_WithPositiveEffort_ReturnsTrue()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.InProgress)
        {
            EffortCompleted = 50m
        };

        // Act & Assert
        phase.HasEffortData.Should().BeTrue();
    }

    [Fact]
    public void HasEffortData_WithZeroOrNullEffort_ReturnsFalse()
    {
        // Arrange
        var phaseZero = new ProjectPhase("Test", "Dates", PhaseStatus.Pending)
        {
            EffortCompleted = 0m
        };
        var phaseNull = new ProjectPhase("Test", "Dates", PhaseStatus.Pending)
        {
            EffortCompleted = null
        };

        // Act & Assert
        phaseZero.HasEffortData.Should().BeFalse();
        phaseNull.HasEffortData.Should().BeFalse();
    }

    [Fact]
    public void IsComplete_WhenStatusComplete_ReturnsTrue()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.Complete);

        // Act & Assert
        phase.IsComplete.Should().BeTrue();
        phase.IsInProgress.Should().BeFalse();
        phase.IsPending.Should().BeFalse();
    }

    [Fact]
    public void IsInProgress_WhenStatusInProgress_ReturnsTrue()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.InProgress);

        // Act & Assert
        phase.IsInProgress.Should().BeTrue();
        phase.IsComplete.Should().BeFalse();
        phase.IsPending.Should().BeFalse();
    }

    [Fact]
    public void IsPending_WhenStatusPending_ReturnsTrue()
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", PhaseStatus.Pending);

        // Act & Assert
        phase.IsPending.Should().BeTrue();
        phase.IsComplete.Should().BeFalse();
        phase.IsInProgress.Should().BeFalse();
    }

    [Theory]
    [InlineData(PhaseStatus.Complete, "Complete")]
    [InlineData(PhaseStatus.InProgress, "In Progress")]
    [InlineData(PhaseStatus.Pending, "Pending")]
    public void StatusText_ReturnsCorrectString(PhaseStatus status, string expectedText)
    {
        // Arrange
        var phase = new ProjectPhase("Test", "Dates", status);

        // Act & Assert
        phase.StatusText.Should().Be(expectedText);
        _output.WriteLine($"Status: {status} => {phase.StatusText}");
    }

    [Fact]
    public void ProjectPhase_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var phase1 = new ProjectPhase("Test", "Dates", PhaseStatus.InProgress);
        var phase2 = new ProjectPhase("Test", "Dates", PhaseStatus.InProgress);
        var phase3 = new ProjectPhase("Different", "Dates", PhaseStatus.InProgress);

        // Act & Assert
        phase1.Should().Be(phase2);
        phase1.Should().NotBe(phase3);
    }
}
