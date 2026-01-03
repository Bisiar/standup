// <copyright file="IntegrationDataTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="IntegrationData"/> entity.
/// </summary>
public sealed class IntegrationDataTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationDataTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public IntegrationDataTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void IntegrationData_WithNoParameters_HasEmptyCollections()
    {
        // Act
        var data = new IntegrationData();

        // Assert
        data.Emails.Should().NotBeNull().And.BeEmpty();
        data.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void IntegrationData_WithNullParameters_HasEmptyCollections()
    {
        // Act
        var data = new IntegrationData(null, null);

        // Assert
        data.Emails.Should().NotBeNull().And.BeEmpty();
        data.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void IntegrationData_WithEmails_StoresEmailsCorrectly()
    {
        // Arrange
        var emails = new List<ClientEmail>
        {
            new ClientEmail(
                EmailId: "email-1",
                ClientCode: "ACME",
                Subject: "Project Update",
                Preview: "Here's the latest update...",
                SenderName: "John Doe",
                SenderEmail: "john@acme.com",
                ReceivedAt: DateTimeOffset.UtcNow),
            new ClientEmail(
                EmailId: "email-2",
                ClientCode: "ACME",
                Subject: "Meeting Request",
                Preview: "Can we schedule...",
                SenderName: "Jane Smith",
                SenderEmail: "jane@acme.com",
                ReceivedAt: DateTimeOffset.UtcNow.AddHours(-2)),
        };

        // Act
        var data = new IntegrationData(Emails: emails);

        // Assert
        data.Emails.Should().HaveCount(2);
        data.Emails[0].Subject.Should().Be("Project Update");
        _output.WriteLine($"Stored {data.Emails.Count} emails");
    }

    [Fact]
    public void IntegrationData_WithMetadata_StoresMetadataCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "TotalCount", 100 },
            { "FetchTime", DateTimeOffset.UtcNow },
            { "Source", "Office365" }
        };

        // Act
        var data = new IntegrationData(Metadata: metadata);

        // Assert
        data.Metadata.Should().HaveCount(3);
        data.Metadata["TotalCount"].Should().Be(100);
        data.Metadata["Source"].Should().Be("Office365");
        _output.WriteLine($"Stored metadata with {data.Metadata.Count} entries");
    }

    [Fact]
    public void IntegrationData_WithBothEmailsAndMetadata_StoresBothCorrectly()
    {
        // Arrange
        var emails = new List<ClientEmail>
        {
            new ClientEmail("e1", "ACME", "Test", "Preview", "Sender", "sender@test.com", DateTimeOffset.UtcNow)
        };
        var metadata = new Dictionary<string, object>
        {
            { "Count", 1 }
        };

        // Act
        var data = new IntegrationData(emails, metadata);

        // Assert
        data.Emails.Should().HaveCount(1);
        data.Metadata.Should().HaveCount(1);
    }

    [Fact]
    public void IntegrationData_DefaultInstances_HaveEmptyCollections()
    {
        // Arrange
        var data1 = new IntegrationData();
        var data2 = new IntegrationData();

        // Act & Assert - Both have empty lists (but different list instances)
        data1.Emails.Should().BeEmpty();
        data2.Emails.Should().BeEmpty();
        data1.Metadata.Should().BeEmpty();
        data2.Metadata.Should().BeEmpty();
    }
}
