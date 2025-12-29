using System.Text;
using System.Text.RegularExpressions;
using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Application.Services;

public static class StandupReportFormatter
{
    /// <summary>
    /// Gets the display name for a ClientCode, defaulting to "General" if empty.
    /// </summary>
    /// <param name="clientCode">The client code to display.</param>
    /// <returns>The client code or "General" if empty.</returns>
    public static string GetDisplayClientCode(string? clientCode)
    {
        return string.IsNullOrWhiteSpace(clientCode) ? "General" : clientCode;
    }

    public static string BuildGroupedReportMarkdown(GroupedStandupReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Standup Report - {report.GroupName}");
        sb.AppendLine($"**Period:** {report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}");
        sb.AppendLine();

        // Section 0: Consolidated Standup (quick highlights across all projects)
        AppendConsolidatedStandupMarkdown(sb, report);

        // Section 1: Team Standup Quick Overview (for team meetings)
        AppendTeamOverviewMarkdown(sb, report);

        // Section 2: Technical Details per Project
        AppendTechnicalDetailsMarkdown(sb, report);

        // Section 3: Executive Summaries (client-shareable)
        AppendExecutiveSummariesMarkdown(sb, report);

        AppendFooter(sb, report);

        return sb.ToString();
    }

    public static string BuildGroupedReportHtml(GroupedStandupReportDto report)
    {
        var sb = new StringBuilder();
        StandupHtmlBuilder.AppendHeader(sb);

        sb.AppendLine($"<h1>Standup Report - {HtmlEncode(report.GroupName)}</h1>");
        sb.AppendLine($"<p class=\"meta\"><strong>Period:</strong> {report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}</p>");

        // Section 0: Consolidated Standup (quick highlights across all projects)
        var highlights = GetConsolidatedHighlights(report);
        StandupHtmlBuilder.AppendConsolidatedStandup(sb, highlights);

        // Section 1: Team Standup Quick Overview
        StandupHtmlBuilder.AppendTeamOverview(sb, report, GetQuickSummary);

        // Section 2: Technical Details per Project
        StandupHtmlBuilder.AppendTechnicalDetails(sb, report);

        // Section 3: Executive Summaries (client-shareable)
        StandupHtmlBuilder.AppendExecutiveSummaries(sb, report);

        StandupHtmlBuilder.AppendFooter(sb, report);

        return sb.ToString();
    }

    public static string BuildAllSummariesMarkdown(GroupedStandupReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Standup Report - {report.GroupName}");
        sb.AppendLine($"**Period:** {report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            sb.AppendLine($"## {GetDisplayClientCode(section.ClientCode)}");
            sb.AppendLine();

            if (section.AllSummaries != null && section.AllSummaries.Count > 0)
            {
                foreach (var (type, summary) in section.AllSummaries.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"### {type} Summary");
                    sb.AppendLine(summary);
                    sb.AppendLine();
                }
            }
            else if (!string.IsNullOrEmpty(section.Summary))
            {
                sb.AppendLine(section.Summary);
                sb.AppendLine();
            }

            AppendCommitsSection(sb, section, maxCommits: 10);
        }

        sb.AppendLine("---");
        sb.AppendLine($"**Totals:** {report.TotalCommits} commits, {report.TotalPullRequests} PRs, {report.TotalWorkItems} work items");
        sb.AppendLine($"*Generated at {report.GeneratedAt:HH:mm on MMM dd, yyyy}*");

        return sb.ToString();
    }

    public static string ConvertMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var html = markdown;

        // Convert headers (order matters - longest first)
        html = Regex.Replace(html, @"^###### (.+)$", "<h6>$1</h6>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^##### (.+)$", "<h5>$1</h5>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^#### (.+)$", "<h4>$1</h4>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^### (.+)$", "<h3>$1</h3>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^## (.+)$", "<h2>$1</h2>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^# (.+)$", "<h1>$1</h1>", RegexOptions.Multiline);

        // Convert bold and italic (bold first to avoid conflict)
        html = Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        html = Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>");

        // Convert bullet lists (both - and * styles, with optional leading whitespace)
        html = Regex.Replace(html, @"^\s*[\-\*] (.+)$", "<li>$1</li>", RegexOptions.Multiline);

        // Convert numbered lists (with optional leading whitespace)
        html = Regex.Replace(html, @"^\s*\d+\. (.+)$", "<li>$1</li>", RegexOptions.Multiline);

        // Wrap consecutive <li> elements in <ul> tags
        html = Regex.Replace(html, @"((?:<li>.*?</li>\s*)+)", "<ul>$1</ul>", RegexOptions.Singleline);

        // Clean up any empty paragraphs and normalize spacing
        html = Regex.Replace(html, @"\n\s*\n", "</p><p>");
        html = $"<p>{html}</p>";

        // Clean up empty <p></p> tags
        html = Regex.Replace(html, @"<p>\s*</p>", string.Empty);

        return html;
    }

    public static string HtmlEncode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private static void AppendConsolidatedStandupMarkdown(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("## 📋 Today's Standup");
        sb.AppendLine();

        var highlights = GetConsolidatedHighlights(report);

        if (highlights.Count == 0)
        {
            sb.AppendLine("*No significant updates for this period.*");
        }
        else
        {
            foreach (var highlight in highlights)
            {
                // Use "General" for empty client codes (defensive fallback)
                var displayCode = string.IsNullOrWhiteSpace(highlight.ClientCode) ? "General" : highlight.ClientCode;
                sb.AppendLine($"- **{displayCode}** - {highlight.Highlight}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static List<(string ClientCode, string Highlight)> GetConsolidatedHighlights(GroupedStandupReportDto report)
    {
        const int maxTotalHighlights = 6;
        var highlights = new List<(string ClientCode, string Highlight)>();

        // Filter sections with activity (ClientCode is optional - will show as "General" if empty)
        var activeSections = report.Sections
            .Where(s => s.Commits.Count > 0 || s.PullRequests.Count > 0 || s.WorkItems.Count > 0)
            .ToList();

        foreach (var section in activeSections)
        {
            if (highlights.Count >= maxTotalHighlights)
            {
                break;
            }

            var sectionHighlights = ExtractHighlightsFromSection(section);
            var highlight = sectionHighlights.FirstOrDefault();
            if (!string.IsNullOrEmpty(highlight))
            {
                highlights.Add((section.ClientCode, highlight));
            }
        }

        return highlights;
    }

    private static List<string> ExtractHighlightsFromSection(ClientCodeSection section)
    {
        var highlights = new List<string>();

        // Try Technical summary first
        if (section.AllSummaries?.TryGetValue(SummaryType.Technical, out var techSummary) == true)
        {
            highlights.AddRange(ExtractBulletPoints(techSummary));
        }

        // Fall back to Executive summary
        if (highlights.Count == 0 && section.AllSummaries?.TryGetValue(SummaryType.Executive, out var execSummary) == true)
        {
            highlights.AddRange(ExtractBulletPoints(execSummary));
        }

        // Fall back to generic summary
        if (highlights.Count == 0 && !string.IsNullOrEmpty(section.Summary))
        {
            highlights.AddRange(ExtractBulletPoints(section.Summary));
        }

        // Last resort: use commit messages
        if (highlights.Count == 0 && section.Commits.Count > 0)
        {
            var commitHighlights = section.Commits
                .Select(c => c.Message.Split('\n')[0])
                .Where(m => m.Length > 10 && !m.StartsWith("Merge", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .Select(m => m.Length > 80 ? m[..77] + "..." : m);
            highlights.AddRange(commitHighlights);
        }

        if (highlights.Count == 0)
        {
            highlights.Add($"{section.CommitCount} commits, {section.PullRequestCount} PRs");
        }

        return highlights;
    }

    private static readonly HashSet<string> GenericTermBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Code Changes",
        "Standup Update",
        "Pull Requests",
        "Work Items",
        "Blockers",
        "Next Steps",
        "Summary",
        "Overview",
        "Changes",
        "Updates",
        "Tasks",
        "Items",
        "Notes",
        "Details",
        "Description",
        "Repository",
    };

    private static List<string> ExtractBulletPoints(string text)
    {
        var highlights = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return highlights;
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#') || trimmed.Length < 10 || trimmed.EndsWith(':'))
            {
                continue;
            }

            var isBullet = trimmed.StartsWith('-') || trimmed.StartsWith('*') || trimmed.StartsWith('•');
            var isNumbered = Regex.IsMatch(trimmed, @"^\d+\.");

            if (isBullet || isNumbered)
            {
                var content = Regex.Replace(trimmed, @"^[-*•]\s*|^\d+\.\s*", string.Empty).Trim();

                // Remove markdown bold markers (handles **text** and *text*)
                content = Regex.Replace(content, @"\*\*([^*]+)\*\*", "$1");
                content = Regex.Replace(content, @"\*([^*]+)\*", "$1");

                // Remove any trailing/leading asterisks that weren't matched
                content = content.Trim('*', ' ');

                // Skip generic section headers
                if (IsGenericTerm(content))
                {
                    continue;
                }

                if (content.Length >= 10 && !content.EndsWith(':'))
                {
                    highlights.Add(content);
                }
            }
        }

        if (highlights.Count == 0)
        {
            var firstSentence = ExtractFirstSentence(text);
            if (!string.IsNullOrEmpty(firstSentence) && firstSentence.Length >= 10 && !IsGenericTerm(firstSentence))
            {
                highlights.Add(firstSentence);
            }
        }

        return highlights;
    }

    private static bool IsGenericTerm(string content)
    {
        // Check exact match (case-insensitive)
        if (GenericTermBlacklist.Contains(content))
        {
            return true;
        }

        // Check if it's just a generic term followed by a colon or asterisk
        var normalized = content.TrimEnd(':', '*', ' ');
        if (GenericTermBlacklist.Contains(normalized))
        {
            return true;
        }

        // Check if it starts with a generic term followed by colon (e.g., "Repository: some-repo")
        foreach (var term in GenericTermBlacklist)
        {
            if (content.StartsWith(term + ":", StringComparison.OrdinalIgnoreCase) ||
                content.StartsWith(term + " ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendTeamOverviewMarkdown(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("## Team Standup Overview");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            var quickSummary = GetQuickSummary(section);
            var statusIndicator = GetSourceStatusMarkdown(section.SourceStatus);
            sb.AppendLine($"**{GetDisplayClientCode(section.ClientCode)}** ({section.CommitCount} commits, {section.PullRequestCount} PRs){statusIndicator} - {quickSummary}");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static string GetSourceStatusMarkdown(DataSourceStatus? status)
    {
        if (status == null)
        {
            return string.Empty;
        }

        var indicators = new List<string>();

        if (status.CommitsStatus == FetchStatus.Error)
        {
            indicators.Add("⚠️ commits");
        }

        if (status.PullRequestsStatus == FetchStatus.Error)
        {
            indicators.Add("⚠️ PRs");
        }
        else if (status.PullRequestsStatus == FetchStatus.NoPat)
        {
            indicators.Add("🔒 PRs");
        }

        if (status.WorkItemsStatus == FetchStatus.Error)
        {
            indicators.Add("⚠️ work items");
        }
        else if (status.WorkItemsStatus == FetchStatus.NoPat)
        {
            indicators.Add("🔒 work items");
        }

        return indicators.Count > 0 ? $" [{string.Join(", ", indicators)}]" : string.Empty;
    }

    private static void AppendTechnicalDetailsMarkdown(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("## Technical Details");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            sb.AppendLine($"### {GetDisplayClientCode(section.ClientCode)}");
            sb.AppendLine();

            // Show Technical summary if available
            if (section.AllSummaries?.TryGetValue(SummaryType.Technical, out var techSummary) == true)
            {
                sb.AppendLine(techSummary);
                sb.AppendLine();
            }

            // Show Code Review summary if available
            if (section.AllSummaries?.TryGetValue(SummaryType.CodeReview, out var codeReview) == true)
            {
                sb.AppendLine("#### Code Review");
                sb.AppendLine(codeReview);
                sb.AppendLine();
            }

            // Fall back to generic summary if no specific types
            if (section.AllSummaries == null || section.AllSummaries.Count == 0)
            {
                if (!string.IsNullOrEmpty(section.Summary))
                {
                    sb.AppendLine(section.Summary);
                    sb.AppendLine();
                }
            }

            AppendCommitsSection(sb, section);
            AppendPullRequestsSection(sb, section);
            AppendWorkItemsSection(sb, section);
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void AppendExecutiveSummariesMarkdown(StringBuilder sb, GroupedStandupReportDto report)
    {
        var hasExecutive = report.Sections.Any(s =>
            s.AllSummaries?.ContainsKey(SummaryType.Executive) == true);

        if (!hasExecutive)
        {
            return;
        }

        sb.AppendLine("## Executive Summary (Client-Shareable)");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            if (section.AllSummaries?.TryGetValue(SummaryType.Executive, out var execSummary) == true)
            {
                sb.AppendLine($"### {GetDisplayClientCode(section.ClientCode)}");
                sb.AppendLine();
                sb.AppendLine(execSummary);
                sb.AppendLine();
            }
        }
    }

    private static string GetQuickSummary(ClientCodeSection section)
    {
        // Try to get first sentence from Technical summary
        if (section.AllSummaries?.TryGetValue(SummaryType.Technical, out var techSummary) == true)
        {
            return ExtractFirstSentence(techSummary);
        }

        // Fall back to Executive summary
        if (section.AllSummaries?.TryGetValue(SummaryType.Executive, out var execSummary) == true)
        {
            return ExtractFirstSentence(execSummary);
        }

        // Fall back to generic summary
        if (!string.IsNullOrEmpty(section.Summary))
        {
            return ExtractFirstSentence(section.Summary);
        }

        // Last resort: summarize from commits
        if (section.Commits.Count > 0)
        {
            var firstCommit = section.Commits.First().Message.Split('\n')[0];
            return firstCommit.Length > 80 ? firstCommit[..77] + "..." : firstCommit;
        }

        return "No activity";
    }

    private static string ExtractFirstSentence(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Remove markdown headers and bold markers
        var cleaned = Regex.Replace(text, @"^#+\s*", string.Empty, RegexOptions.Multiline);
        cleaned = Regex.Replace(cleaned, @"\*\*([^*]+)\*\*", "$1");

        // Remove common section headers like "Code Changes:", "Standup Update:", etc.
        cleaned = Regex.Replace(cleaned, @"^(Code Changes|Standup Update|Summary|Overview|Changes):\s*", string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);

        // Remove numbered list prefixes like "1. ", "2. ", etc.
        cleaned = Regex.Replace(cleaned, @"^\d+\.\s*", string.Empty, RegexOptions.Multiline);

        // Remove bullet points
        cleaned = Regex.Replace(cleaned, @"^[-*•]\s*", string.Empty, RegexOptions.Multiline);

        // Remove empty lines and trim
        cleaned = Regex.Replace(cleaned, @"\n\s*\n", "\n");
        cleaned = cleaned.Trim();

        // Skip lines that are just section headers or very short
        var lines = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty or very short lines
            if (trimmedLine.Length < 10)
            {
                continue;
            }

            // Skip lines that look like headers (end with colon)
            if (trimmedLine.EndsWith(':'))
            {
                continue;
            }

            // Found a good line - extract first sentence
            var match = Regex.Match(trimmedLine, @"^[^.!?]+[.!?]");
            if (match.Success)
            {
                var sentence = match.Value.Trim();
                return sentence.Length > 120 ? sentence[..117] + "..." : sentence;
            }

            // No sentence ending, use the whole line
            return trimmedLine.Length > 120 ? trimmedLine[..117] + "..." : trimmedLine;
        }

        // Fallback: take first non-empty content
        var firstLine = cleaned.Split('\n')[0].Trim();
        return firstLine.Length > 120 ? firstLine[..117] + "..." : firstLine;
    }

    private static void AppendCommitsSection(StringBuilder sb, ClientCodeSection section, int maxCommits = 10)
    {
        if (section.Commits.Count == 0)
        {
            return;
        }

        sb.AppendLine($"### Commits ({section.CommitCount})");
        foreach (var commit in section.Commits.Take(maxCommits))
        {
            var message = commit.Message.Split('\n')[0];
            if (message.Length > 80)
            {
                message = message[..77] + "...";
            }

            sb.AppendLine($"- {message}");
        }

        if (section.CommitCount > maxCommits)
        {
            sb.AppendLine($"- ... and {section.CommitCount - maxCommits} more");
        }

        sb.AppendLine();
    }

    private static void AppendPullRequestsSection(StringBuilder sb, ClientCodeSection section)
    {
        if (section.PullRequests.Count == 0)
        {
            return;
        }

        sb.AppendLine($"### Pull Requests ({section.PullRequestCount})");
        foreach (var pr in section.PullRequests)
        {
            sb.AppendLine($"- [{pr.Status}] {pr.Title}");
        }

        sb.AppendLine();
    }

    private static void AppendWorkItemsSection(StringBuilder sb, ClientCodeSection section)
    {
        if (section.WorkItems.Count == 0)
        {
            return;
        }

        sb.AppendLine($"### Work Items ({section.WorkItemCount})");
        foreach (var item in section.WorkItems)
        {
            sb.AppendLine($"- [{item.Status}] {item.Title} ({item.Type})");
        }

        sb.AppendLine();
    }

    private static void AppendFooter(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("---");
        sb.AppendLine($"**Totals:** {report.TotalCommits} commits, {report.TotalPullRequests} PRs, {report.TotalWorkItems} work items");
        sb.AppendLine($"*Generated at {report.GeneratedAt:HH:mm on MMM dd, yyyy}*");
    }
}
