using System.Text;
using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Application.Services;

/// <summary>
/// Builds HTML content for standup reports.
/// </summary>
public static class StandupHtmlBuilder
{
    private static readonly string[] ProjectColors =
    {
        "#3B82F6", // Blue
        "#10B981", // Green
        "#F59E0B", // Amber
        "#8B5CF6", // Purple
        "#EF4444", // Red
        "#06B6D4", // Cyan
        "#EC4899", // Pink
        "#6366F1", // Indigo
    };

    /// <summary>
    /// Appends the HTML document header with styles.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    public static void AppendHeader(StringBuilder sb)
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

    /// <summary>
    /// Appends the HTML document footer.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="report">The report for totals.</param>
    public static void AppendFooter(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("<div class=\"totals\">");
        sb.AppendLine($"<strong>Totals:</strong> {report.TotalCommits} commits, {report.TotalPullRequests} PRs, {report.TotalWorkItems} work items");
        sb.AppendLine("</div>");
        sb.AppendLine($"<p class=\"footer\">Generated at {report.GeneratedAt:HH:mm on MMM dd, yyyy}</p>");
        sb.AppendLine("</body></html>");
    }

    /// <summary>
    /// Appends consolidated standup section in HTML format.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="highlights">The highlights to display.</param>
    public static void AppendConsolidatedStandup(StringBuilder sb, List<(string ClientCode, string Highlight)> highlights)
    {
        sb.AppendLine("<div style=\"background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 12px; margin-bottom: 25px;\">");
        sb.AppendLine("<h2 style=\"color: white; margin-top: 0; border: none;\">📋 Quick Standup</h2>");

        if (highlights.Count == 0)
        {
            sb.AppendLine("<p><em>No significant activity to highlight.</em></p>");
        }
        else
        {
            sb.AppendLine("<ul style=\"list-style: none; padding: 0; margin: 0;\">");
            foreach (var highlight in highlights)
            {
                sb.AppendLine("<li style=\"margin: 10px 0; padding: 8px 12px; background: rgba(255,255,255,0.1); border-radius: 6px; border-left: 4px solid rgba(255,255,255,0.5);\">");
                sb.AppendLine($"<strong style=\"color: #ffd700;\">{StandupReportFormatter.HtmlEncode(highlight.ClientCode)}</strong> - {StandupReportFormatter.HtmlEncode(highlight.Highlight)}");
                sb.AppendLine("</li>");
            }

            sb.AppendLine("</ul>");
        }

        sb.AppendLine("</div>");
    }

    /// <summary>
    /// Appends team overview section in HTML format.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="report">The report data.</param>
    /// <param name="getQuickSummary">Function to get quick summary for a section.</param>
    public static void AppendTeamOverview(
        StringBuilder sb,
        GroupedStandupReportDto report,
        Func<ClientCodeSection, string> getQuickSummary)
    {
        sb.AppendLine("<h2 style=\"color: #0066cc; border-bottom: 2px solid #0066cc; padding-bottom: 5px;\">Team Standup Overview</h2>");
        sb.AppendLine("<div class=\"overview-section\" style=\"background: #e8f4fd; padding: 15px; border-radius: 8px; margin-bottom: 20px;\">");

        foreach (var section in report.Sections)
        {
            var quickSummary = getQuickSummary(section);
            var statusIndicator = GetSourceStatusIndicator(section.SourceStatus);
            sb.AppendLine($"<p style=\"margin: 8px 0;\"><strong>{StandupReportFormatter.HtmlEncode(section.ClientCode)}</strong> <span style=\"color: #666;\">({section.CommitCount} commits, {section.PullRequestCount} PRs)</span>{statusIndicator} - {StandupReportFormatter.HtmlEncode(quickSummary)}</p>");
        }

        sb.AppendLine("</div>");
    }

    /// <summary>
    /// Appends technical details section in HTML format.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="report">The report data.</param>
    public static void AppendTechnicalDetails(StringBuilder sb, GroupedStandupReportDto report)
    {
        sb.AppendLine("<h2 style=\"color: #10B981; margin-top: 40px;\">Technical Details</h2>");

        for (var i = 0; i < report.Sections.Count; i++)
        {
            var section = report.Sections[i];
            var color = GetProjectColor(i);

            sb.AppendLine($"<div style=\"border: 2px solid {color}; border-radius: 12px; margin: 20px 0; overflow: hidden;\">");
            sb.AppendLine($"<div style=\"background: {color}; color: white; padding: 15px 20px;\">");
            sb.AppendLine($"<h3 style=\"color: white; margin: 0; font-size: 1.3em;\">📁 {StandupReportFormatter.HtmlEncode(section.ClientCode)}</h3>");
            sb.AppendLine($"<p style=\"margin: 5px 0 0 0; opacity: 0.9; font-size: 0.9em;\">{section.CommitCount} commits | {section.PullRequestCount} PRs | {section.WorkItemCount} work items</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div style=\"padding: 15px 20px;\">");

            if (section.AllSummaries?.TryGetValue(SummaryType.Technical, out var techSummary) == true)
            {
                sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid {color};\">{StandupReportFormatter.ConvertMarkdownToHtml(techSummary)}</div>");
            }

            if (section.AllSummaries?.TryGetValue(SummaryType.CodeReview, out var codeReview) == true)
            {
                sb.AppendLine("<h4 style=\"color: #8B5CF6; margin-top: 20px;\">🔍 Code Review</h4>");
                sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid #8B5CF6;\">{StandupReportFormatter.ConvertMarkdownToHtml(codeReview)}</div>");
            }

            if (section.AllSummaries == null || section.AllSummaries.Count == 0)
            {
                if (!string.IsNullOrEmpty(section.Summary))
                {
                    sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid {color};\">{StandupReportFormatter.ConvertMarkdownToHtml(section.Summary)}</div>");
                }
            }

            AppendCommitsSection(sb, section, color);
            AppendPullRequestsSection(sb, section, color);
            AppendWorkItemsSection(sb, section, color);

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }
    }

    /// <summary>
    /// Appends executive summaries section in HTML format.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="report">The report data.</param>
    public static void AppendExecutiveSummaries(StringBuilder sb, GroupedStandupReportDto report)
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
                sb.AppendLine($"<h3>{StandupReportFormatter.HtmlEncode(section.ClientCode)}</h3>");
                sb.AppendLine($"<div class=\"summary\" style=\"border-left: 4px solid #3B82F6; background: #f0f7ff;\">{StandupReportFormatter.ConvertMarkdownToHtml(execSummary)}</div>");
            }
        }
    }

    /// <summary>
    /// Gets the HTML indicator for data source status.
    /// </summary>
    /// <param name="status">The data source status.</param>
    /// <returns>HTML string for status indicator.</returns>
    public static string GetSourceStatusIndicator(DataSourceStatus? status)
    {
        if (status == null)
        {
            return string.Empty;
        }

        var indicators = new List<string>();

        if (status.PullRequestsStatus == FetchStatus.NoPat)
        {
            indicators.Add("<span style=\"color: #f59e0b; font-size: 0.8em;\" title=\"PRs unavailable - no PAT\">⚠️ PRs</span>");
        }
        else if (status.PullRequestsStatus == FetchStatus.Error)
        {
            indicators.Add("<span style=\"color: #ef4444; font-size: 0.8em;\" title=\"PR fetch failed\">❌ PRs</span>");
        }

        if (status.WorkItemsStatus == FetchStatus.NoPat)
        {
            indicators.Add("<span style=\"color: #f59e0b; font-size: 0.8em;\" title=\"Work items unavailable - no PAT\">⚠️ Items</span>");
        }
        else if (status.WorkItemsStatus == FetchStatus.Error)
        {
            indicators.Add("<span style=\"color: #ef4444; font-size: 0.8em;\" title=\"Work item fetch failed\">❌ Items</span>");
        }

        if (status.CommitsStatus == FetchStatus.Error)
        {
            indicators.Add("<span style=\"color: #ef4444; font-size: 0.8em;\" title=\"Commit fetch failed\">❌ Commits</span>");
        }

        if (indicators.Count == 0)
        {
            return string.Empty;
        }

        return " " + string.Join(" ", indicators);
    }

    private static string GetProjectColor(int index)
    {
        return ProjectColors[index % ProjectColors.Length];
    }

    private static void AppendCommitsSection(StringBuilder sb, ClientCodeSection section, string color)
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

            sb.AppendLine($"<li>{StandupReportFormatter.HtmlEncode(message)}</li>");
        }

        if (section.CommitCount > 10)
        {
            sb.AppendLine($"<li><em>... and {section.CommitCount - 10} more</em></li>");
        }

        sb.AppendLine("</ul>");
    }

    private static void AppendPullRequestsSection(StringBuilder sb, ClientCodeSection section, string color)
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
                _ => "status-active",
            };
            sb.AppendLine($"<li><span class=\"status {statusClass}\">{StandupReportFormatter.HtmlEncode(pr.Status)}</span> {StandupReportFormatter.HtmlEncode(pr.Title)}</li>");
        }

        sb.AppendLine("</ul>");
    }

    private static void AppendWorkItemsSection(StringBuilder sb, ClientCodeSection section, string color)
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
                _ => "status-open",
            };
            sb.AppendLine($"<li><span class=\"status {statusClass}\">{item.Status}</span> {StandupReportFormatter.HtmlEncode(item.Title)} <em>({StandupReportFormatter.HtmlEncode(item.Type)})</em></li>");
        }

        sb.AppendLine("</ul>");
    }
}
