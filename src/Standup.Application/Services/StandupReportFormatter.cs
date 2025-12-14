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
        sb.AppendLine($"**Summary Type:** {report.CurrentSummaryType}");
        sb.AppendLine();

        foreach (var section in report.Sections)
        {
            sb.AppendLine($"## {section.ClientCode}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(section.Summary))
            {
                sb.AppendLine(section.Summary);
                sb.AppendLine();
            }

            AppendCommitsSection(sb, section);
            AppendPullRequestsSection(sb, section);
            AppendWorkItemsSection(sb, section);
        }

        AppendFooter(sb, report);

        return sb.ToString();
    }

    public static string BuildGroupedReportHtml(GroupedStandupReportDto report)
    {
        var sb = new StringBuilder();
        AppendHtmlHeader(sb);

        sb.AppendLine($"<h1>Standup Report - {HtmlEncode(report.GroupName)}</h1>");
        sb.AppendLine($"<p class=\"meta\"><strong>Period:</strong> {report.PeriodStart:MMM dd} - {report.PeriodEnd:MMM dd, yyyy}</p>");
        sb.AppendLine($"<p class=\"meta\"><strong>Summary Type:</strong> {report.CurrentSummaryType}</p>");

        foreach (var section in report.Sections)
        {
            sb.AppendLine($"<h2>{HtmlEncode(section.ClientCode)}</h2>");

            if (!string.IsNullOrEmpty(section.Summary))
            {
                sb.AppendLine($"<div class=\"summary\">{ConvertMarkdownToHtml(section.Summary)}</div>");
            }

            AppendCommitsSectionHtml(sb, section);
            AppendPullRequestsSectionHtml(sb, section);
            AppendWorkItemsSectionHtml(sb, section);
        }

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

        // Convert headers
        html = Regex.Replace(html, @"^### (.+)$", "<h3>$1</h3>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^## (.+)$", "<h2>$1</h2>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^# (.+)$", "<h1>$1</h1>", RegexOptions.Multiline);

        // Convert bold and italic
        html = Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        html = Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>");

        // Convert bullet lists
        html = Regex.Replace(html, @"^- (.+)$", "<li>$1</li>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"(<li>.*</li>\n?)+", "<ul>$0</ul>");

        // Convert line breaks
        html = html.Replace("\n\n", "</p><p>");
        html = $"<p>{html}</p>";

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
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine("h1 { color: #1a1a1a; border-bottom: 2px solid #0066cc; padding-bottom: 10px; }");
        sb.AppendLine("h2 { color: #0066cc; margin-top: 30px; }");
        sb.AppendLine("h3 { color: #444; }");
        sb.AppendLine(".meta { color: #666; font-size: 0.9em; }");
        sb.AppendLine(".summary { background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 10px 0; }");
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

    private static void AppendCommitsSectionHtml(StringBuilder sb, ClientCodeSection section)
    {
        if (!section.Commits.Any())
        {
            return;
        }

        sb.AppendLine($"<h3>Commits ({section.CommitCount})</h3>");
        sb.AppendLine("<ul>");
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

    private static void AppendPullRequestsSectionHtml(StringBuilder sb, ClientCodeSection section)
    {
        if (!section.PullRequests.Any())
        {
            return;
        }

        sb.AppendLine($"<h3>Pull Requests ({section.PullRequestCount})</h3>");
        sb.AppendLine("<ul>");
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

    private static void AppendWorkItemsSectionHtml(StringBuilder sb, ClientCodeSection section)
    {
        if (!section.WorkItems.Any())
        {
            return;
        }

        sb.AppendLine($"<h3>Work Items ({section.WorkItemCount})</h3>");
        sb.AppendLine("<ul>");
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
