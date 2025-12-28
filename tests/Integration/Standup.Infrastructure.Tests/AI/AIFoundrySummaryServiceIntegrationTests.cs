using FluentAssertions;
using Microsoft.Extensions.Options;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.AI;
using Standup.Infrastructure.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Infrastructure.Tests.AI;

/// <summary>
/// Integration tests that validate AIFoundrySummaryService configuration and functionality.
/// These tests call the real Azure OpenAI service - they require valid credentials.
/// Mark as Skip if running in CI without credentials.
/// </summary>
public class AIFoundrySummaryServiceIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public AIFoundrySummaryServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Validates that the AI service configuration is correct and can connect.
    /// This is the critical test that catches configuration issues.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GenerateSummaryAsync_WithValidConfig_ReturnsNonEmptySummary()
    {
        // Arrange
        var options = GetTestOptions();
        _output.WriteLine($"Testing with Endpoint: {options.Endpoint}");
        _output.WriteLine($"Testing with Deployment: {options.DeploymentName}");
        _output.WriteLine($"UseAzureIdentity: {options.UseAzureIdentity}");
        _output.WriteLine($"HasApiKey: {!string.IsNullOrEmpty(options.ApiKey)}");

        var service = new AIFoundrySummaryService(Options.Create(options));

        var testData = CreateTestStandupData();

        // Act
        var result = await service.GenerateSummaryAsync(testData, new SummaryOptions(Type: SummaryType.Technical));

        // Assert
        _output.WriteLine($"Result length: {result?.Length ?? 0}");
        _output.WriteLine($"Result preview: {result?[..Math.Min(200, result?.Length ?? 0)]}...");

        result.Should().NotBeNullOrEmpty("AI should return a summary for valid data");
        result.Length.Should().BeGreaterThan(50, "Summary should be substantial");
    }

    /// <summary>
    /// Tests all three summary types to ensure each produces different, appropriate output.
    /// </summary>
    /// <param name="summaryType">The summary type to test.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(SummaryType.Technical)]
    [InlineData(SummaryType.Executive)]
    [InlineData(SummaryType.CodeReview)]
    public async Task GenerateSummaryAsync_EachSummaryType_ProducesAppropriateContent(SummaryType summaryType)
    {
        // Arrange
        var options = GetTestOptions();
        var service = new AIFoundrySummaryService(Options.Create(options));
        var testData = CreateTestStandupData();

        _output.WriteLine($"Testing SummaryType: {summaryType}");

        // Act
        var result = await service.GenerateSummaryAsync(
            testData,
            new SummaryOptions(Type: summaryType));

        // Assert
        _output.WriteLine($"[{summaryType}] Result length: {result?.Length ?? 0}");
        _output.WriteLine($"[{summaryType}] Preview: {result?[..Math.Min(300, result?.Length ?? 0)]}...");

        result.Should().NotBeNullOrEmpty($"{summaryType} summary should not be empty");

        // Verify content is appropriate for the summary type
        switch (summaryType)
        {
            case SummaryType.Executive:
                // Executive summaries should mention business value, not code details
                result.Should().NotContain("class", "Executive summary should avoid code terminology");
                break;

            case SummaryType.CodeReview:
                // Code review should focus on quality/security
                (result.Contains("security", StringComparison.OrdinalIgnoreCase) ||
                 result.Contains("quality", StringComparison.OrdinalIgnoreCase) ||
                 result.Contains("review", StringComparison.OrdinalIgnoreCase))
                    .Should().BeTrue("Code review should discuss quality or security");
                break;

            case SummaryType.Technical:
                // Technical should be detailed
                result.Length.Should().BeGreaterThan(100, "Technical summary should be detailed");
                break;
        }
    }

    /// <summary>
    /// Tests that all summary types can be generated in sequence (like the app does).
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task GenerateSummaryAsync_AllTypesInSequence_AllSucceed()
    {
        // Arrange
        var options = GetTestOptions();
        var service = new AIFoundrySummaryService(Options.Create(options));
        var testData = CreateTestStandupData();

        var summaryTypes = new[] { SummaryType.Executive, SummaryType.Technical, SummaryType.CodeReview };
        var results = new Dictionary<SummaryType, string>();
        var failures = new List<(SummaryType Type, Exception Error)>();

        // Act - Generate all types in sequence (like the app does)
        foreach (var summaryType in summaryTypes)
        {
            try
            {
                _output.WriteLine($"Generating {summaryType} summary...");
                var result = await service.GenerateSummaryAsync(
                    testData,
                    new SummaryOptions(Type: summaryType));

                results[summaryType] = result;
                _output.WriteLine($"  SUCCESS: {result.Length} chars");
            }
            catch (Exception ex)
            {
                failures.Add((summaryType, ex));
                _output.WriteLine($"  FAILED: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Assert
        _output.WriteLine($"\nResults: {results.Count}/{summaryTypes.Length} succeeded");

        failures.Should().BeEmpty(
            $"All summary types should succeed. Failures: {string.Join(", ", failures.Select(f => $"{f.Type}: {f.Error.Message}"))}");

        results.Should().HaveCount(3, "All three summary types should be generated");

        foreach (var (type, summary) in results)
        {
            summary.Should().NotBeNullOrEmpty($"{type} summary should not be empty");
        }
    }

    /// <summary>
    /// Gets AI options from environment or uses test defaults.
    /// </summary>
    private static AIFoundryOptions GetTestOptions()
    {
        return new AIFoundryOptions
        {
            Endpoint = Environment.GetEnvironmentVariable("AIFoundry__Endpoint")
                ?? "https://cog-vtht5f2batt7q.openai.azure.com/",
            DeploymentName = Environment.GetEnvironmentVariable("AIFoundry__DeploymentName")
                ?? "gpt-4o",
            ApiKey = Environment.GetEnvironmentVariable("AIFoundry__ApiKey") ?? string.Empty,
            UseAzureIdentity = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AIFoundry__ApiKey"))
        };
    }

    /// <summary>
    /// Creates test standup data with realistic commits, PRs, and work items.
    /// </summary>
    private static StandupData CreateTestStandupData()
    {
        return new StandupData
        {
            Commits = new List<CommitInfo>
            {
                new(
                    Sha: "abc123",
                    Message: "feat: Add user authentication with JWT tokens",
                    Repository: "standup",
                    SourceType: SourceType.GitHub,
                    CommittedAt: DateTimeOffset.UtcNow.AddHours(-2)),
                new(
                    Sha: "def456",
                    Message: "fix: Resolve null reference in report formatter",
                    Repository: "standup",
                    SourceType: SourceType.GitHub,
                    CommittedAt: DateTimeOffset.UtcNow.AddHours(-4)),
                new(
                    Sha: "ghi789",
                    Message: "refactor: Extract AI service configuration to options pattern",
                    Repository: "standup",
                    SourceType: SourceType.GitHub,
                    CommittedAt: DateTimeOffset.UtcNow.AddHours(-6))
            },
            PullRequests = new List<PullRequestInfo>
            {
                new(
                    Id: "42",
                    Title: "Add comprehensive logging to AI service",
                    Repository: "standup",
                    SourceType: SourceType.GitHub,
                    Status: "Open",
                    Url: "https://github.com/test/standup/pull/42",
                    CreatedAt: DateTimeOffset.UtcNow.AddDays(-1),
                    IsDraft: false,
                    ReviewerCount: 2)
            },
            WorkItems = new List<WorkItemInfo>
            {
                new(
                    Id: "WORK-123",
                    Title: "Implement standup report generation",
                    Type: "User Story",
                    Status: WorkItemStatus.InProgress,
                    SourceType: SourceType.AzureDevOps,
                    Url: "https://dev.azure.com/test/standup/_workitems/edit/123")
            }
        };
    }
}
