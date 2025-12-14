using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;

namespace Standup.Domain.Tests.Entities;

public class EmailSettingsTests
{
    [Fact]
    public void EmailSettings_HasDefaultValues()
    {
        // Act
        var settings = new EmailSettings();

        // Assert
        settings.FromAddress.Should().BeEmpty();
        settings.FromDisplayName.Should().BeEmpty();
        settings.SendViaGraph.Should().BeTrue();
    }

    [Fact]
    public void EmailSettings_CanSetCustomValues()
    {
        // Act
        var settings = new EmailSettings(
            FromAddress: "noreply@example.com",
            FromDisplayName: "Standup Bot",
            SendViaGraph: false);

        // Assert
        settings.FromAddress.Should().Be("noreply@example.com");
        settings.FromDisplayName.Should().Be("Standup Bot");
        settings.SendViaGraph.Should().BeFalse();
    }

    [Fact]
    public void EmailSettings_IsRecord_SupportsEquality()
    {
        // Arrange
        var settings1 = new EmailSettings(
            FromAddress: "test@example.com",
            FromDisplayName: "Test Sender",
            SendViaGraph: true);

        var settings2 = new EmailSettings(
            FromAddress: "test@example.com",
            FromDisplayName: "Test Sender",
            SendViaGraph: true);

        // Assert
        settings1.Should().Be(settings2);
        (settings1 == settings2).Should().BeTrue();
    }

    [Fact]
    public void EmailSettings_DifferentValues_AreNotEqual()
    {
        // Arrange
        var settings1 = new EmailSettings(
            FromAddress: "sender1@example.com",
            FromDisplayName: "Sender One");

        var settings2 = new EmailSettings(
            FromAddress: "sender2@example.com",
            FromDisplayName: "Sender Two");

        // Assert
        settings1.Should().NotBe(settings2);
        (settings1 != settings2).Should().BeTrue();
    }

    [Fact]
    public void EmailSettings_SupportsDeconstruction()
    {
        // Arrange
        var settings = new EmailSettings(
            FromAddress: "admin@example.com",
            FromDisplayName: "Admin Team",
            SendViaGraph: false);

        // Act
        var (fromAddress, fromDisplayName, sendViaGraph) = settings;

        // Assert
        fromAddress.Should().Be("admin@example.com");
        fromDisplayName.Should().Be("Admin Team");
        sendViaGraph.Should().BeFalse();
    }

    [Fact]
    public void EmailSettings_WithGraphEnabled_UsesGraphApi()
    {
        // Act
        var settings = new EmailSettings(
            FromAddress: "bot@example.com",
            FromDisplayName: "Bot",
            SendViaGraph: true);

        // Assert
        settings.SendViaGraph.Should().BeTrue();
    }

    [Fact]
    public void EmailSettings_WithGraphDisabled_UsesSmtp()
    {
        // Act
        var settings = new EmailSettings(
            FromAddress: "smtp@example.com",
            FromDisplayName: "SMTP Sender",
            SendViaGraph: false);

        // Assert
        settings.SendViaGraph.Should().BeFalse();
    }

    [Theory]
    [InlineData("noreply@example.com", "No Reply")]
    [InlineData("standup@company.com", "Standup Automation")]
    [InlineData("devops@org.com", "DevOps Team")]
    public void EmailSettings_SupportsDifferentSenderConfigurations(string fromAddress, string displayName)
    {
        // Act
        var settings = new EmailSettings(
            FromAddress: fromAddress,
            FromDisplayName: displayName);

        // Assert
        settings.FromAddress.Should().Be(fromAddress);
        settings.FromDisplayName.Should().Be(displayName);
    }

    [Fact]
    public void EmailSettings_SupportsWithExpression()
    {
        // Arrange
        var original = new EmailSettings(
            FromAddress: "original@example.com",
            FromDisplayName: "Original Name",
            SendViaGraph: true);

        // Act
        var updated = original with { FromDisplayName = "Updated Name" };

        // Assert
        updated.FromAddress.Should().Be(original.FromAddress);
        updated.FromDisplayName.Should().Be("Updated Name");
        updated.SendViaGraph.Should().Be(original.SendViaGraph);
        original.FromDisplayName.Should().Be("Original Name"); // Original unchanged
    }

    [Fact]
    public void EmailSettings_WithEmptyValues_StoresEmptyStrings()
    {
        // Act
        var settings = new EmailSettings(
            FromAddress: string.Empty,
            FromDisplayName: string.Empty,
            SendViaGraph: false);

        // Assert
        settings.FromAddress.Should().BeEmpty();
        settings.FromDisplayName.Should().BeEmpty();
    }
}
