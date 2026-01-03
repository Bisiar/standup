// <copyright file="StandupReportFormatterTests.cs" company="Standup">
// Copyright (c) Standup. All rights reserved.
// </copyright>

using FluentAssertions;
using Standup.Application.DTOs;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Application.Tests.Services;

/// <summary>
/// Tests for the <see cref="StandupReportFormatter"/> static class.
/// </summary>
public sealed class StandupReportFormatterTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandupReportFormatterTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public StandupReportFormatterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region GetDisplayClientCode Tests

    [Fact]
    public void GetDisplayClientCode_WithClientCode_ReturnsClientCode()
    {
        // Act
        var result = StandupReportFormatter.GetDisplayClientCode("ACME");

        // Assert
        result.Should().Be("ACME");
    }

    [Fact]
    public void GetDisplayClientCode_WithNull_ReturnsGeneral()
    {
        // Act
        var result = StandupReportFormatter.GetDisplayClientCode(null);

        // Assert
        result.Should().Be("General");
    }

    [Fact]
    public void GetDisplayClientCode_WithEmptyString_ReturnsGeneral()
    {
        // Act
        var result = StandupReportFormatter.GetDisplayClientCode(string.Empty);

        // Assert
        result.Should().Be("General");
    }

    [Fact]
    public void GetDisplayClientCode_WithWhitespace_ReturnsGeneral()
    {
        // Act
        var result = StandupReportFormatter.GetDisplayClientCode("   ");

        // Assert
        result.Should().Be("General");
    }

    #endregion

    #region HtmlEncode Tests

    [Fact]
    public void HtmlEncode_WithPlainText_ReturnsUnchanged()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("Hello World");

        // Assert
        result.Should().Be("Hello World");
    }

    [Fact]
    public void HtmlEncode_WithAmpersand_EncodesCorrectly()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("Tom & Jerry");

        // Assert
        result.Should().Be("Tom &amp; Jerry");
    }

    [Fact]
    public void HtmlEncode_WithLessThan_EncodesCorrectly()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("a < b");

        // Assert
        result.Should().Be("a &lt; b");
    }

    [Fact]
    public void HtmlEncode_WithGreaterThan_EncodesCorrectly()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("a > b");

        // Assert
        result.Should().Be("a &gt; b");
    }

    [Fact]
    public void HtmlEncode_WithDoubleQuote_EncodesCorrectly()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("He said \"Hello\"");

        // Assert
        result.Should().Be("He said &quot;Hello&quot;");
    }

    [Fact]
    public void HtmlEncode_WithSingleQuote_EncodesCorrectly()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("It's fine");

        // Assert
        result.Should().Be("It&#39;s fine");
    }

    [Fact]
    public void HtmlEncode_WithNull_ReturnsEmptyString()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void HtmlEncode_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void HtmlEncode_WithAllSpecialChars_EncodesAll()
    {
        // Act
        var result = StandupReportFormatter.HtmlEncode("<script>alert('XSS\"&');</script>");

        // Assert
        result.Should().Be("&lt;script&gt;alert(&#39;XSS&quot;&amp;&#39;);&lt;/script&gt;");
        _output.WriteLine($"Encoded: {result}");
    }

    #endregion

    #region ConvertMarkdownToHtml Tests

    [Fact]
    public void ConvertMarkdownToHtml_WithNull_ReturnsEmptyString()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithH1Header_ConvertsToH1Tag()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("# Header 1");

        // Assert
        result.Should().Contain("<h1>Header 1</h1>");
        _output.WriteLine($"H1 result: {result}");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithH2Header_ConvertsToH2Tag()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("## Header 2");

        // Assert
        result.Should().Contain("<h2>Header 2</h2>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithH3Header_ConvertsToH3Tag()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("### Header 3");

        // Assert
        result.Should().Contain("<h3>Header 3</h3>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithH4Header_ConvertsToH4Tag()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("#### Header 4");

        // Assert
        result.Should().Contain("<h4>Header 4</h4>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithH5Header_ConvertsToH5Tag()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("##### Header 5");

        // Assert
        result.Should().Contain("<h5>Header 5</h5>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithH6Header_ConvertsToH6Tag()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("###### Header 6");

        // Assert
        result.Should().Contain("<h6>Header 6</h6>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithBoldText_ConvertsToStrong()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("This is **bold** text");

        // Assert
        result.Should().Contain("<strong>bold</strong>");
        _output.WriteLine($"Bold result: {result}");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithItalicText_ConvertsToEm()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("This is *italic* text");

        // Assert
        result.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithBulletListDash_ConvertsToLi()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("- Item 1");

        // Assert
        result.Should().Contain("<li>Item 1</li>");
        result.Should().Contain("<ul>");
        _output.WriteLine($"Bullet result: {result}");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithBulletListAsterisk_ConvertsToLi()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("* Item 1");

        // Assert
        result.Should().Contain("<li>Item 1</li>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithNumberedList_ConvertsToLi()
    {
        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml("1. First item");

        // Assert
        result.Should().Contain("<li>First item</li>");
    }

    #endregion

    #region BuildGroupedReportMarkdown Tests

    [Fact]
    public void BuildGroupedReportMarkdown_WithEmptyReport_ReturnsBasicStructure()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("# Standup Report - Test Group");
        result.Should().Contain("**Period:**");
        result.Should().Contain("## \U0001F4CB Today's Standup");
        result.Should().Contain("**Totals:**");
        _output.WriteLine($"Markdown report length: {result.Length}");
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithSections_IncludesSectionData()
    {
        // Arrange
        var report = CreateReportWithSection();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("ACME");
        result.Should().Contain("## Technical Details");
        result.Should().Contain("## Team Standup Overview");
        _output.WriteLine(result);
    }

    #endregion

    #region BuildGroupedReportHtml Tests

    [Fact]
    public void BuildGroupedReportHtml_WithEmptyReport_ReturnsValidHtml()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportHtml(report);

        // Assert
        result.Should().Contain("<html");
        result.Should().Contain("<head>");
        result.Should().Contain("<body");
        result.Should().Contain("</html>");
        result.Should().Contain("<h1>Standup Report - Test Group</h1>");
        _output.WriteLine($"HTML report length: {result.Length}");
    }

    [Fact]
    public void BuildGroupedReportHtml_EncodesGroupNameProperly()
    {
        // Arrange
        var report = CreateReportWithXssGroupName();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportHtml(report);

        // Assert
        result.Should().Contain("&lt;script&gt;");
        result.Should().NotContain("<script>alert");
    }

    #endregion

    #region BuildAllSummariesMarkdown Tests

    [Fact]
    public void BuildAllSummariesMarkdown_WithEmptyReport_ReturnsBasicStructure()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var result = StandupReportFormatter.BuildAllSummariesMarkdown(report);

        // Assert
        result.Should().Contain("# Standup Report - Test Group");
        result.Should().Contain("**Totals:**");
        result.Should().Contain("commits");
        result.Should().Contain("PRs");
        result.Should().Contain("work items");
    }

    [Fact]
    public void BuildAllSummariesMarkdown_WithMultipleSummaryTypes_IncludesAll()
    {
        // Arrange
        var report = CreateReportWithMultipleSummaries();

        // Act
        var result = StandupReportFormatter.BuildAllSummariesMarkdown(report);

        // Assert
        result.Should().Contain("### Technical Summary");
        result.Should().Contain("### Executive Summary");
        _output.WriteLine(result);
    }

    #endregion

    #region Complex Markdown Conversion Tests

    [Fact]
    public void ConvertMarkdownToHtml_WithMultipleHeaders_ConvertsAll()
    {
        // Arrange
        var markdown = """
            # Title
            ## Section 1
            ### Subsection
            #### Sub-subsection
            """;

        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml(markdown);

        // Assert
        result.Should().Contain("<h1>Title</h1>");
        result.Should().Contain("<h2>Section 1</h2>");
        result.Should().Contain("<h3>Subsection</h3>");
        result.Should().Contain("<h4>Sub-subsection</h4>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithNestedListItems_ConvertsToUl()
    {
        // Arrange
        var markdown = """
            - First item
            - Second item
            - Third item
            """;

        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml(markdown);

        // Assert
        result.Should().Contain("<ul>");
        result.Should().Contain("<li>First item</li>");
        result.Should().Contain("<li>Second item</li>");
        result.Should().Contain("<li>Third item</li>");
        result.Should().Contain("</ul>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithMixedFormatting_ConvertsBoldAndItalic()
    {
        // Arrange
        var markdown = "This has **bold** and *italic* text together";

        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml(markdown);

        // Assert
        result.Should().Contain("<strong>bold</strong>");
        result.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void ConvertMarkdownToHtml_WithIndentedBullets_ConvertsThem()
    {
        // Arrange
        var markdown = "  - Indented item";

        // Act
        var result = StandupReportFormatter.ConvertMarkdownToHtml(markdown);

        // Assert
        result.Should().Contain("<li>Indented item</li>");
    }

    #endregion

    #region Report With Various Content Tests

    [Fact]
    public void BuildGroupedReportMarkdown_WithPullRequests_IncludesPRSection()
    {
        // Arrange
        var report = CreateReportWithPullRequests();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("Pull Requests");
        result.Should().Contain("Feature: Add login");
        result.Should().Contain("Active");
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithWorkItems_IncludesWorkItemSection()
    {
        // Arrange
        var report = CreateReportWithWorkItems();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("Work Items");
        result.Should().Contain("Implement authentication");
        result.Should().Contain("Task");
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithManyCommits_TruncatesCommitList()
    {
        // Arrange
        var report = CreateReportWithManyCommits(15);

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("... and 5 more");
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithLongCommitMessage_TruncatesMessage()
    {
        // Arrange
        var longMessage = new string('a', 100); // Longer than 80 chars
        var report = CreateReportWithSpecificCommitMessage(longMessage);

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("...");
        result.Should().NotContain(longMessage);
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithNoActivity_ShowsNoUpdatesMessage()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("No significant updates");
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithExecutiveSummary_IncludesInExecutiveSection()
    {
        // Arrange
        var report = CreateReportWithExecutiveSummary();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert
        result.Should().Contain("Executive Summary");
        result.Should().Contain("Business value delivered");
    }

    #endregion

    #region HTML Report Tests

    [Fact]
    public void BuildGroupedReportHtml_WithSections_IncludesFormattedSections()
    {
        // Arrange
        var report = CreateReportWithSection();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportHtml(report);

        // Assert
        result.Should().Contain("<html");
        result.Should().Contain("ACME");
        result.Should().Contain("</html>");
    }

    [Fact]
    public void BuildGroupedReportHtml_IncludesStylesheet()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportHtml(report);

        // Assert
        result.Should().Contain("<style>");
        result.Should().Contain("</style>");
    }

    [Fact]
    public void BuildGroupedReportHtml_WithPeriod_DisplaysFormattedDates()
    {
        // Arrange
        var report = CreateEmptyReport();

        // Act
        var result = StandupReportFormatter.BuildGroupedReportHtml(report);

        // Assert
        result.Should().Contain("Period:");
    }

    #endregion

    #region FetchStatus Tests

    [Fact]
    public void BuildGroupedReportMarkdown_WithCommitFetchError_ShowsWarningIndicator()
    {
        // Arrange
        var report = CreateReportWithFetchStatus(FetchStatus.Error, FetchStatus.Success, FetchStatus.Success);

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert - Should contain warning emoji for commits error
        result.Should().Contain("commits");
    }

    [Fact]
    public void BuildGroupedReportMarkdown_WithNoPatForPRs_ShowsLockIndicator()
    {
        // Arrange
        var report = CreateReportWithFetchStatus(FetchStatus.Success, FetchStatus.NoPat, FetchStatus.Success);

        // Act
        var result = StandupReportFormatter.BuildGroupedReportMarkdown(report);

        // Assert - Should contain lock emoji for NoPat
        result.Should().Contain("PRs");
    }

    #endregion

    #region Additional Helper Methods

    private static GroupedStandupReportDto CreateReportWithPullRequests()
    {
        var prs = new List<PullRequestInfo>
        {
            new PullRequestInfo(
                Id: "1",
                Title: "Feature: Add login",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                Status: "Active",
                Url: "https://dev.azure.com/pr/1",
                CreatedAt: DateTimeOffset.UtcNow.AddDays(-1))
        };

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: "Added authentication",
                Commits: new List<CommitInfo>(),
                PullRequests: prs,
                WorkItems: new List<WorkItemInfo>())
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 1,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithWorkItems()
    {
        var workItems = new List<WorkItemInfo>
        {
            new WorkItemInfo(
                Id: "123",
                Title: "Implement authentication",
                Type: "Task",
                Status: WorkItemStatus.Active,
                SourceType: SourceType.AzureDevOps,
                Url: "https://dev.azure.com/wi/123",
                AssignedTo: "John Doe")
        };

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: "Working on auth",
                Commits: new List<CommitInfo>(),
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: workItems)
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 1);
    }

    private static GroupedStandupReportDto CreateReportWithManyCommits(int count)
    {
        var commits = Enumerable.Range(0, count)
            .Select(i => new CommitInfo(
                Sha: $"sha{i:D3}",
                Message: $"Commit message {i}",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow.AddHours(-i),
                Author: "John Doe"))
            .ToList();

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: "Many commits",
                Commits: commits,
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>())
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: count,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithSpecificCommitMessage(string message)
    {
        var commits = new List<CommitInfo>
        {
            new CommitInfo(
                Sha: "abc123",
                Message: message,
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow,
                Author: "John Doe")
        };

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: "Test",
                Commits: commits,
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>())
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 1,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithExecutiveSummary()
    {
        var summaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Executive, "Business value delivered to stakeholders" }
        };

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: null,
                Commits: new List<CommitInfo>
                {
                    new CommitInfo("sha1", "Fix bug", "repo", SourceType.AzureDevOps, DateTimeOffset.UtcNow, Author: "Author")
                },
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>(),
                AllSummaries: summaries)
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 1,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithFetchStatus(
        FetchStatus commitsStatus,
        FetchStatus prsStatus,
        FetchStatus workItemsStatus)
    {
        var sourceStatus = new DataSourceStatus(
            commitsStatus,
            prsStatus,
            workItemsStatus);

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: "Test",
                Commits: new List<CommitInfo>(),
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>(),
                SourceStatus: sourceStatus)
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    #endregion

    #region Helper Methods

    private static GroupedStandupReportDto CreateEmptyReport()
    {
        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: new List<ClientCodeSection>(),
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithXssGroupName()
    {
        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "<script>alert('XSS')</script>",
            Sections: new List<ClientCodeSection>(),
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithSection()
    {
        var commits = new List<CommitInfo>
        {
            new CommitInfo(
                Sha: "abc123",
                Message: "Fix login bug",
                Repository: "test-repo",
                SourceType: SourceType.AzureDevOps,
                CommittedAt: DateTimeOffset.UtcNow.AddHours(-2),
                Author: "John Doe")
        };

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: "Fixed several bugs and added new features",
                Commits: commits,
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>())
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 1,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    private static GroupedStandupReportDto CreateReportWithMultipleSummaries()
    {
        var summaries = new Dictionary<SummaryType, string>
        {
            { SummaryType.Technical, "Technical details about code changes" },
            { SummaryType.Executive, "High-level summary for stakeholders" }
        };

        var sections = new List<ClientCodeSection>
        {
            new ClientCodeSection(
                ClientCode: "ACME",
                Summary: null,
                Commits: new List<CommitInfo>(),
                PullRequests: new List<PullRequestInfo>(),
                WorkItems: new List<WorkItemInfo>(),
                AllSummaries: summaries)
        };

        return new GroupedStandupReportDto(
            Id: "report-1",
            GroupName: "Test Group",
            Sections: sections,
            PeriodStart: DateTimeOffset.UtcNow.AddDays(-1),
            PeriodEnd: DateTimeOffset.UtcNow,
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalCommits: 0,
            TotalPullRequests: 0,
            TotalWorkItems: 0);
    }

    #endregion
}
