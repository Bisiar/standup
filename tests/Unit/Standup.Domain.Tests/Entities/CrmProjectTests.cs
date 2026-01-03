// <copyright file="CrmProjectTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="CrmProject"/> entity.
/// </summary>
public sealed class CrmProjectTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrmProjectTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public CrmProjectTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CrmProject_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Arrange & Act
        var project = new CrmProject(
            CrmProjectId: "proj-123",
            ClientCode: "ACME",
            ProjectName: "Website Redesign",
            ClientName: "ACME Corp",
            Status: "Active");

        // Assert
        project.CrmProjectId.Should().Be("proj-123");
        project.ClientCode.Should().Be("ACME");
        project.ProjectName.Should().Be("Website Redesign");
        project.ClientName.Should().Be("ACME Corp");
        project.Status.Should().Be("Active");
        _output.WriteLine($"Created project: {project.ProjectName}");
    }

    [Fact]
    public void CrmProject_OptionalProperties_DefaultToNull()
    {
        // Arrange & Act
        var project = new CrmProject(
            CrmProjectId: "proj-123",
            ClientCode: "ACME",
            ProjectName: "Test",
            ClientName: "ACME Corp",
            Status: "Active");

        // Assert
        project.StartDate.Should().BeNull();
        project.EndDate.Should().BeNull();
        project.CurrentPhase.Should().BeNull();
        project.ProjectManager.Should().BeNull();
        project.PercentComplete.Should().BeNull();
        project.BudgetHours.Should().BeNull();
        project.HoursUsed.Should().BeNull();
    }

    [Fact]
    public void CrmProject_WithInitProperties_SetsValuesCorrectly()
    {
        // Arrange & Act
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var project = new CrmProject(
            CrmProjectId: "proj-456",
            ClientCode: "XYZ",
            ProjectName: "Mobile App",
            ClientName: "XYZ Inc",
            Status: "In Progress")
        {
            StartDate = startDate,
            EndDate = endDate,
            CurrentPhase = "Development",
            ProjectManager = "Jane Doe",
            PercentComplete = 45.5m,
            BudgetHours = 1000m,
            HoursUsed = 455m
        };

        // Assert
        project.StartDate.Should().Be(startDate);
        project.EndDate.Should().Be(endDate);
        project.CurrentPhase.Should().Be("Development");
        project.ProjectManager.Should().Be("Jane Doe");
        project.PercentComplete.Should().Be(45.5m);
        project.BudgetHours.Should().Be(1000m);
        project.HoursUsed.Should().Be(455m);
        _output.WriteLine($"Project phase: {project.CurrentPhase}, {project.PercentComplete}% complete");
    }

    [Fact]
    public void IsOnTrack_WhenHoursUsedLessThanBudget_ReturnsTrue()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 100m,
            HoursUsed = 50m
        };

        // Act & Assert
        project.IsOnTrack.Should().BeTrue();
        _output.WriteLine($"Budget: {project.BudgetHours}, Used: {project.HoursUsed}, OnTrack: {project.IsOnTrack}");
    }

    [Fact]
    public void IsOnTrack_WhenHoursUsedEqualsBudget_ReturnsTrue()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 100m,
            HoursUsed = 100m
        };

        // Act & Assert
        project.IsOnTrack.Should().BeTrue();
    }

    [Fact]
    public void IsOnTrack_WhenHoursUsedExceedsBudget_ReturnsFalse()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 100m,
            HoursUsed = 150m
        };

        // Act & Assert
        project.IsOnTrack.Should().BeFalse();
        _output.WriteLine($"Over budget! Budget: {project.BudgetHours}, Used: {project.HoursUsed}");
    }

    [Fact]
    public void IsOnTrack_WhenBudgetHoursNull_ReturnsTrue()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = null,
            HoursUsed = 50m
        };

        // Act & Assert
        project.IsOnTrack.Should().BeTrue();
    }

    [Fact]
    public void IsOnTrack_WhenHoursUsedNull_ReturnsTrue()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 100m,
            HoursUsed = null
        };

        // Act & Assert
        project.IsOnTrack.Should().BeTrue();
    }

    [Fact]
    public void BudgetUtilization_CalculatesPercentageCorrectly()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 200m,
            HoursUsed = 50m
        };

        // Act & Assert
        project.BudgetUtilization.Should().Be(25m);
        _output.WriteLine($"Budget utilization: {project.BudgetUtilization}%");
    }

    [Fact]
    public void BudgetUtilization_WhenBudgetZero_ReturnsNull()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 0m,
            HoursUsed = 50m
        };

        // Act & Assert
        project.BudgetUtilization.Should().BeNull();
    }

    [Fact]
    public void BudgetUtilization_WhenBudgetNull_ReturnsNull()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = null,
            HoursUsed = 50m
        };

        // Act & Assert
        project.BudgetUtilization.Should().BeNull();
    }

    [Fact]
    public void BudgetUtilization_WhenHoursUsedNull_ReturnsZeroPercent()
    {
        // Arrange
        var project = new CrmProject("1", "ACME", "Test", "Client", "Active")
        {
            BudgetHours = 100m,
            HoursUsed = null
        };

        // Act & Assert
        project.BudgetUtilization.Should().Be(0m);
    }

    [Fact]
    public void CrmProject_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var project1 = new CrmProject("1", "ACME", "Test", "Client", "Active");
        var project2 = new CrmProject("1", "ACME", "Test", "Client", "Active");
        var project3 = new CrmProject("2", "ACME", "Test", "Client", "Active");

        // Act & Assert
        project1.Should().Be(project2);
        project1.Should().NotBe(project3);
    }
}
