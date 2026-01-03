// <copyright file="ConnectedServiceTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.Models;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Models;

/// <summary>
/// Tests for the <see cref="ConnectedService"/> record.
/// </summary>
public sealed class ConnectedServiceTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedServiceTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ConnectedServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConnectedService_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Act
        var service = new ConnectedService(
            Name: "Azure DevOps",
            Icon: "\uE753",
            Status: ServiceStatus.Connected);

        // Assert
        service.Name.Should().Be("Azure DevOps");
        service.Icon.Should().Be("\uE753");
        service.Status.Should().Be(ServiceStatus.Connected);
        _output.WriteLine($"Service: {service.Name}, Status: {service.Status}");
    }

    [Theory]
    [InlineData(ServiceStatus.Connected, "Connected")]
    [InlineData(ServiceStatus.Syncing, "Syncing")]
    [InlineData(ServiceStatus.Error, "Error")]
    public void StatusText_ReturnsCorrectString(ServiceStatus status, string expectedText)
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", status);

        // Act & Assert
        service.StatusText.Should().Be(expectedText);
        _output.WriteLine($"Status: {status} => {service.StatusText}");
    }

    [Fact]
    public void StatusText_ForDisconnectedStatus_ReturnsUnknown()
    {
        // Arrange - Disconnected is in enum but not in switch statement
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Disconnected);

        // Act & Assert
        service.StatusText.Should().Be("Unknown");
    }

    [Fact]
    public void StatusBackground_ForConnectedStatus_ReturnsDarkGreenColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Connected);

        // Act & Assert
        service.StatusBackground.Should().Be("#065F46");
        _output.WriteLine($"Connected background: {service.StatusBackground}");
    }

    [Fact]
    public void StatusBackground_ForSyncingStatus_ReturnsDarkBlueColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Syncing);

        // Act & Assert
        service.StatusBackground.Should().Be("#1E3A5F");
    }

    [Fact]
    public void StatusBackground_ForErrorStatus_ReturnsDarkRedColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Error);

        // Act & Assert
        service.StatusBackground.Should().Be("#7F1D1D");
    }

    [Fact]
    public void StatusBackground_ForDisconnectedStatus_ReturnsGrayColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Disconnected);

        // Act & Assert
        service.StatusBackground.Should().Be("#334155");
    }

    [Fact]
    public void StatusTextColor_ForConnectedStatus_ReturnsLightGreenColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Connected);

        // Act & Assert
        service.StatusTextColor.Should().Be("#34D399");
        _output.WriteLine($"Connected text color: {service.StatusTextColor}");
    }

    [Fact]
    public void StatusTextColor_ForSyncingStatus_ReturnsLightBlueColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Syncing);

        // Act & Assert
        service.StatusTextColor.Should().Be("#60A5FA");
    }

    [Fact]
    public void StatusTextColor_ForErrorStatus_ReturnsLightRedColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Error);

        // Act & Assert
        service.StatusTextColor.Should().Be("#FCA5A5");
    }

    [Fact]
    public void StatusTextColor_ForDisconnectedStatus_ReturnsGrayColor()
    {
        // Arrange
        var service = new ConnectedService("Test", "Icon", ServiceStatus.Disconnected);

        // Act & Assert
        service.StatusTextColor.Should().Be("#94A3B8");
    }

    [Fact]
    public void ConnectedService_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var service1 = new ConnectedService("Test", "Icon", ServiceStatus.Connected);
        var service2 = new ConnectedService("Test", "Icon", ServiceStatus.Connected);
        var service3 = new ConnectedService("Different", "Icon", ServiceStatus.Connected);

        // Act & Assert
        service1.Should().Be(service2);
        service1.Should().NotBe(service3);
    }

    [Fact]
    public void ConnectedService_AllStatuses_HaveColors()
    {
        // Test all status values have colors defined
        foreach (ServiceStatus status in Enum.GetValues<ServiceStatus>())
        {
            var service = new ConnectedService("Test", "Icon", status);
            service.StatusBackground.Should().NotBeNullOrEmpty($"Status {status} should have background color");
            service.StatusTextColor.Should().NotBeNullOrEmpty($"Status {status} should have text color");
            _output.WriteLine($"{status}: bg={service.StatusBackground}, text={service.StatusTextColor}");
        }
    }
}
