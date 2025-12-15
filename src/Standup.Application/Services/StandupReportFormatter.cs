using System.Text;
using System.Text.RegularExpressions;
using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Application.Services;

public static class StandupReportFormatter
{
    public static string BuildGroupedReportMarkdown(GroupedStandupReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Standup Report - {report.GroupName}");
        sb.AppendLine($"**Period:** {report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}");
        sb.AppendLine();

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
        AppendHtmlHeader(sb);

        sb.AppendLine($"<h1>Standup Report - {HtmlEncode(report.GroupName)}</h1>");
        sb.AppendLine($"<p class=\"meta\"><strong>Period:</strong> {report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}</p>");

        // Section 1: Team Standup Quick Overview
        AppendTeamOverviewHtml(sb, report);

        // Section 2: Technical Details per Project
        AppendTechnicalDetailsHtml(sb, report);

        // Section 3: Executive Summaries (client-shareable)
        AppendExecutiveSummariesHtml(sb, report);

        AppendHtmlFooter(sb, report);

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
            sb.AppendLine($"## {section.ClientCode}");
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

    private static void AppendTeamOverviewMarkdown(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("## Team Standup Overview");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            var quickSummary = GetQuickSummary(section);
            var statusIndicator = GetSourceStatusMarkdown(section.SourceStatus);
            sb.AppendLine($"**{section.ClientCode}** ({section.CommitCount} commits, {section.PullRequestCount} PRs){statusIndicator} - {quickSummary}");
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

    private static string GetSourceStatusHtml(DataSourceStatus? status)
    {
        if (status == null)
        {
            return string.Empty;
        }

        var indicators = new List<string>();

        if (status.CommitsStatus == FetchStatus.Error)
        {
            indicators.Add("<span style=\"color: #dc3545;\" title=\"Error fetching commits\">⚠️</span>");
        }
        else if (status.CommitsStatus == FetchStatus.Success)
        {
            indicators.Add("<span style=\"color: #28a745;\" title=\"Commits fetched\">✅</span>");
        }

        if (status.PullRequestsStatus == FetchStatus.Error)
        {
            indicators.Add("<span style=\"color: #dc3545;\" title=\"Error fetching PRs\">⚠️</span>");
        }
        else if (status.PullRequestsStatus == FetchStatus.NoPat)
        {
            indicators.Add("<span style=\"color: #6c757d;\" title=\"No PAT - PRs unavailable\">🔒</span>");
        }
        else if (status.PullRequestsStatus == FetchStatus.Success)
        {
            indicators.Add("<span style=\"color: #28a745;\" title=\"PRs fetched\">✅</span>");
        }

        if (status.WorkItemsStatus == FetchStatus.Error)
        {
            indicators.Add("<span style=\"color: #dc3545;\" title=\"Error fetching work items\">⚠️</span>");
        }
        else if (status.WorkItemsStatus == FetchStatus.NoPat)
        {
            indicators.Add("<span style=\"color: #6c757d;\" title=\"No PAT - work items unavailable\">🔒</span>");
        }
        else if (status.WorkItemsStatus == FetchStatus.Success)
        {
            indicators.Add("<span style=\"color: #28a745;\" title=\"Work items fetched\">✅</span>");
        }

        return indicators.Count > 0 ? $" <span style=\"font-size: 0.8em;\">{string.Join(" ", indicators)}</span>" : string.Empty;
    }

    private static void AppendTechnicalDetailsMarkdown(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("## Technical Details");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            sb.AppendLine($"### {section.ClientCode}");
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
                sb.AppendLine($"### {section.ClientCode}");
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
        if (section.Commits.Any())
        {
            var firstCommit = section.Commits.First().Message.Split('\n')[0];
            return firstCommit.Length > 80 ? firstCommit[..77] + "..." : firstCommit;
        }

        return "No activity";
    }

    private static readonly string[] ProjectColors =
    {
        "#3B82F6", // Blue
        "#10B981", // Green
        "#F59E0B", // Amber
        "#8B5CF6", // Purple
        "#EF4444", // Red
        "#06B6D4", // Cyan
        "#EC4899", // Pink
        "#6366F1" // Indigo
    };

    private static string GetProjectColor(int index)
    {
        return ProjectColors[index % ProjectColors.Length];
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

    private static void AppendTeamOverviewHtml(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("<h2 style=\"color: #0066cc; border-bottom: 2px solid #0066cc; padding-bottom: 5px;\">Team Standup Overview</h2>");
        sb.AppendLine("<div class=\"overview-section\" style=\"background: #e8f4fd; padding: 15px; border-radius: 8px; margin-bottom: 20px;\">");

        foreach (var section in report.Sections)
        {
            var quickSummary = GetQuickSummary(section);
            var statusIndicator = GetSourceStatusHtml(section.SourceStatus);
            sb.AppendLine($"<p style=\"margin: 8px 0;\"><strong>{HtmlEncode(section.ClientCode)}</strong> <span style=\"color: #666;\">({section.CommitCount} commits, {section.PullRequestCount} PRs)</span>{statusIndicator} - {HtmlEncode(quickSummary)}</p>");
        }

        sb.AppendLine("</div>");
    }

    private static void AppendTechnicalDetailsHtml(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("<h2 style=\"color: #10B981; margin-top: 40px;\">Technical Details</h2>");

        for (var i = 0; i < report.Sections.Count; i++)
        {
            var section = report.Sections[i];
            var color = GetProjectColor(i);

            // Project container with colored border
            sb.AppendLine($"<div style=\"border: 2px solid {color}; border-radius: 12px; margin: 20px 0; overflow: hidden;\">");

            // Prominent project header banner
            sb.AppendLine($"<div style=\"background: {color}; color: white; padding: 15px 20px;\">");
            sb.AppendLine($"<h3 style=\"color: white; margin: 0; font-size: 1.3em;\">📁 {HtmlEncode(section.ClientCode)}</h3>");
            sb.AppendLine($"<p style=\"margin: 5px 0 0 0; opacity: 0.9; font-size: 0.9em;\">{section.CommitCount} commits | {section.PullRequestCount} PRs | {section.WorkItemCount} work items</p>");
            sb.AppendLine("</div>");

            // Content area with padding
            sb.AppendLine("<div style=\"padding: 15px 20px;\">");

            // Show Technical summary if available
            if (section.AllSummaries?.TryGetValue(SummaryType.Technical, out var techSummary) == true)
            {
                sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid {color};\">{ConvertMarkdownToHtml(techSummary)}</div>");
            }

            // Show Code Review summary if available
            if (section.AllSummaries?.TryGetValue(SummaryType.CodeReview, out var codeReview) == true)
            {
                sb.AppendLine("<h4 style=\"color: #8B5CF6; margin-top: 20px;\">🔍 Code Review</h4>");
                sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid #8B5CF6;\">{ConvertMarkdownToHtml(codeReview)}</div>");
            }

            // Fall back to generic summary if no specific types
            if (section.AllSummaries == null || section.AllSummaries.Count == 0)
            {
                if (!string.IsNullOrEmpty(section.Summary))
                {
                    sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid {color};\">{ConvertMarkdownToHtml(section.Summary)}</div>");
                }
            }

            AppendCommitsSectionHtml(sb, section, color);
            AppendPullRequestsSectionHtml(sb, section, color);
            AppendWorkItemsSectionHtml(sb, section, color);

            sb.AppendLine("</div>"); // Close content area
            sb.AppendLine("</div>"); // Close project container
        }
    }

    private static void AppendExecutiveSummariesHtml(StringBuilder sb, GroupedStandupReportDto report)
    {
        var hasExecutive = report.Sections.Any(s =>
            s.AllSummaries?.ContainsKey(SummaryType.Executive) == true);

        if (!hasExecutive)
        {
            return;
        }

        sb.AppendLine("<hr style=\"margin: 30px 0;\">");
        sb.AppendLine("<h2 style=\"color: #3B82F6;\">Executive Summary <span style=\"font-size: 0.7em; color: #666; font-weight: normal;\">(Client-Shareable)</span></h2>");

        foreach (var section in report.Sections)
        {
            if (section.AllSummaries?.TryGetValue(SummaryType.Executive, out var execSummary) == true)
            {
                sb.AppendLine($"<h3>{HtmlEncode(section.ClientCode)}</h3>");
                sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid #3B82F6; background: #f0f7ff;\">{ConvertMarkdownToHtml(execSummary)}</div>");
            }
        }
    }

    private static void AppendCommitsSection(StringBuilder sb, ClientCodeSection section, int maxCommits = 10)
    {
        if (!section.Commits.Any())
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
        if (!section.PullRequests.Any())
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
        if (!section.WorkItems.Any())
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

    private static void AppendHtmlHeader(StringBuilder sb)
    {
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; }");
        sb.AppendLine("h1 { color: #1a1a1a; border-bottom: 2px solid #0066cc; padding-bottom: 10px; }");
        sb.AppendLine("h2 { color: #0066cc; margin-top: 30px; }");
        sb.AppendLine("h3 { color: #444; }");
        sb.AppendLine(".meta { color: #666; font-size: 0.9em; }");
        sb.AppendLine(".summary { background: #f5f5f5; padding: 15px; padding-left: 20px; border-radius: 8px; margin: 10px 0; }");
        sb.AppendLine(".summary ul { padding-left: 25px; margin: 10px 0; }");
        sb.AppendLine(".summary li { margin: 8px 0; }");
        sb.AppendLine("ul { padding-left: 20px; }");
        sb.AppendLine("li { margin: 5px 0; }");
        sb.AppendLine(".status { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 0.8em; font-weight: bold; }");
        sb.AppendLine(".status-open { background: #fff3cd; color: #856404; }");
        sb.AppendLine(".status-merged, .status-completed { background: #d4edda; color: #155724; }");
        sb.AppendLine(".status-active, .status-inprogress { background: #cce5ff; color: #004085; }");
        sb.AppendLine(".totals { background: #e9ecef; padding: 15px; border-radius: 8px; margin-top: 30px; }");
        sb.AppendLine(".footer { color: #999; font-size: 0.8em; margin-top: 20px; text-align: center; }");
        sb.AppendLine("</style></head><body>");
    }

    private static void AppendCommitsSectionHtml(StringBuilder sb, ClientCodeSection section, string color)
    {
        if (!section.Commits.Any())
        {
            return;
        }

        sb.AppendLine($"<h4 style=\"color: {color}; margin-top: 15px;\">📝 Commits ({section.CommitCount})</h4>");
        sb.AppendLine("<ul style=\"margin-top: 5px;\">");
        foreach (var commit in section.Commits.Take(10))
        {
            var message = commit.Message.Split('\n')[0];
            if (message.Length > 80)
            {
                message = message[..77] + "...";
            }

            sb.AppendLine($"<li>{HtmlEncode(message)}</li>");
        }

        if (section.CommitCount > 10)
        {
            sb.AppendLine($"<li><em>... and {section.CommitCount - 10} more</em></li>");
        }

        sb.AppendLine("</ul>");
    }

    private static void AppendPullRequestsSectionHtml(StringBuilder sb, ClientCodeSection section, string color)
    {
        if (!section.PullRequests.Any())
        {
            return;
        }

        sb.AppendLine($"<h4 style=\"color: {color}; margin-top: 15px;\">🔀 Pull Requests ({section.PullRequestCount})</h4>");
        sb.AppendLine("<ul style=\"margin-top: 5px;\">");
        foreach (var pr in section.PullRequests)
        {
            var statusClass = pr.Status.ToLowerInvariant() switch
            {
                "merged" => "status-merged",
                "open" => "status-open",
                _ => "status-active"
            };
            sb.AppendLine($"<li><span class=\"status {statusClass}\">{HtmlEncode(pr.Status)}</span> {HtmlEncode(pr.Title)}</li>");
        }

        sb.AppendLine("</ul>");
    }

    private static void AppendWorkItemsSectionHtml(StringBuilder sb, ClientCodeSection section, string color)
    {
        if (!section.WorkItems.Any())
        {
            return;
        }

        sb.AppendLine($"<h4 style=\"color: {color}; margin-top: 15px;\">📋 Work Items ({section.WorkItemCount})</h4>");
        sb.AppendLine("<ul style=\"margin-top: 5px;\">");
        foreach (var item in section.WorkItems)
        {
            var statusClass = item.Status switch
            {
                WorkItemStatus.Closed or WorkItemStatus.Resolved => "status-completed",
                WorkItemStatus.Active or WorkItemStatus.InProgress => "status-inprogress",
                _ => "status-open"
            };
            sb.AppendLine($"<li><span class=\"status {statusClass}\">{item.Status}</span> {HtmlEncode(item.Title)} <em>({HtmlEncode(item.Type)})</em></li>");
        }

        sb.AppendLine("</ul>");
    }

    private static void AppendHtmlFooter(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("<div class=\"totals\">");
        sb.AppendLine($"<strong>Totals:</strong> {report.TotalCommits} commits, {report.TotalPullRequests} PRs, {report.TotalWorkItems} work items");
        sb.AppendLine("</div>");
        sb.AppendLine($"<p class=\"footer\">Generated at {report.GeneratedAt:HH:mm on MMM dd, yyyy}</p>");
        sb.AppendLine("</body></html>");
    }
}
