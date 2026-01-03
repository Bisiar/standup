// <copyright file="ClientEmailTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Domain.Entities;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Domain.Tests.Entities;

/// <summary>
/// Tests for the <see cref="ClientEmail"/> entity.
/// </summary>
public sealed class ClientEmailTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientEmailTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public ClientEmailTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ClientEmail_WithRequiredProperties_SetsValuesCorrectly()
    {
        // Arrange
        var receivedAt = DateTimeOffset.UtcNow;

        // Act
        var email = new ClientEmail(
            EmailId: "email-123",
            ClientCode: "ACME",
            Subject: "Project Update",
            Preview: "Here is the latest update on the project...",
            SenderName: "John Doe",
            SenderEmail: "john@acme.com",
            ReceivedAt: receivedAt);

        // Assert
        email.EmailId.Should().Be("email-123");
        email.ClientCode.Should().Be("ACME");
        email.Subject.Should().Be("Project Update");
        email.Preview.Should().Be("Here is the latest update on the project...");
        email.SenderName.Should().Be("John Doe");
        email.SenderEmail.Should().Be("john@acme.com");
        email.ReceivedAt.Should().Be(receivedAt);
        _output.WriteLine($"Email from {email.SenderName}: {email.Subject}");
    }

    [Fact]
    public void ClientEmail_OptionalProperties_DefaultToExpectedValues()
    {
        // Arrange & Act
        var email = new ClientEmail(
            EmailId: "email-1",
            ClientCode: "XYZ",
            Subject: "Test",
            Preview: "Preview text",
            SenderName: "Sender",
            SenderEmail: "sender@test.com",
            ReceivedAt: DateTimeOffset.UtcNow);

        // Assert
        email.IsImportant.Should().BeFalse();
        email.HasAttachments.Should().BeFalse();
        email.ConversationId.Should().BeNull();
        email.AiSummary.Should().BeNull();
        email.SuggestedTasks.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ClientEmail_WithAllOptionalProperties_SetsValuesCorrectly()
    {
        // Arrange
        var tasks = new List<SuggestedTask>
        {
            new SuggestedTask("Task 1", "Description 1", "High", "email-123")
        };

        // Act
        var email = new ClientEmail(
            EmailId: "email-123",
            ClientCode: "ACME",
            Subject: "Urgent: Action Required",
            Preview: "Please review the attached document...",
            SenderName: "Jane Smith",
            SenderEmail: "jane@acme.com",
            ReceivedAt: DateTimeOffset.UtcNow,
            IsImportant: true,
            HasAttachments: true,
            ConversationId: "conv-456",
            AiSummary: "Request to review and approve the proposal",
            SuggestedTasks: tasks);

        // Assert
        email.IsImportant.Should().BeTrue();
        email.HasAttachments.Should().BeTrue();
        email.ConversationId.Should().Be("conv-456");
        email.AiSummary.Should().Be("Request to review and approve the proposal");
        email.SuggestedTasks.Should().HaveCount(1);
        _output.WriteLine($"Important email with {email.SuggestedTasks.Count} suggested tasks");
    }

    [Fact]
    public void TruncatedSubject_WhenShort_ReturnsFullSubject()
    {
        // Arrange
        var email = new ClientEmail(
            "e1", "ACME", "Short Subject", "Preview", "Sender", "s@t.com", DateTimeOffset.UtcNow);

        // Act & Assert
        email.TruncatedSubject.Should().Be("Short Subject");
    }

    [Fact]
    public void TruncatedSubject_WhenExactly60Chars_ReturnsFullSubject()
    {
        // Arrange
        var subject = new string('A', 60);
        var email = new ClientEmail(
            "e1", "ACME", subject, "Preview", "Sender", "s@t.com", DateTimeOffset.UtcNow);

        // Act & Assert
        email.TruncatedSubject.Should().Be(subject);
    }

    [Fact]
    public void TruncatedSubject_WhenLong_TruncatesWithEllipsis()
    {
        // Arrange
        var subject = new string('A', 80);
        var email = new ClientEmail(
            "e1", "ACME", subject, "Preview", "Sender", "s@t.com", DateTimeOffset.UtcNow);

        // Act
        var truncated = email.TruncatedSubject;

        // Assert
        truncated.Should().HaveLength(60);
        truncated.Should().EndWith("...");
        _output.WriteLine($"Truncated subject: {truncated}");
    }

    [Fact]
    public void TruncatedPreview_WhenShort_ReturnsFullPreview()
    {
        // Arrange
        var email = new ClientEmail(
            "e1", "ACME", "Subject", "Short preview", "Sender", "s@t.com", DateTimeOffset.UtcNow);

        // Act & Assert
        email.TruncatedPreview.Should().Be("Short preview");
    }

    [Fact]
    public void TruncatedPreview_WhenExactly150Chars_ReturnsFullPreview()
    {
        // Arrange
        var preview = new string('B', 150);
        var email = new ClientEmail(
            "e1", "ACME", "Subject", preview, "Sender", "s@t.com", DateTimeOffset.UtcNow);

        // Act & Assert
        email.TruncatedPreview.Should().Be(preview);
    }

    [Fact]
    public void TruncatedPreview_WhenLong_TruncatesWithEllipsis()
    {
        // Arrange
        var preview = new string('B', 200);
        var email = new ClientEmail(
            "e1", "ACME", "Subject", preview, "Sender", "s@t.com", DateTimeOffset.UtcNow);

        // Act
        var truncated = email.TruncatedPreview;

        // Assert
        truncated.Should().HaveLength(150);
        truncated.Should().EndWith("...");
        _output.WriteLine($"Truncated preview length: {truncated.Length}");
    }

    [Fact]
    public void ClientEmail_RecordEquality_DifferentIds_NotEqual()
    {
        // Arrange
        var receivedAt = DateTimeOffset.UtcNow;
        var email1 = new ClientEmail("e1", "ACME", "Test", "Preview", "Sender", "s@t.com", receivedAt);
        var email2 = new ClientEmail("e2", "ACME", "Test", "Preview", "Sender", "s@t.com", receivedAt);

        // Act & Assert - Records with different IDs should not be equal
        email1.Should().NotBe(email2);
    }
}
