using System.Text;
using Standup.Domain.Entities;

namespace Standup.Infrastructure.Services;

/// <summary>
/// Builds simple markdown summaries when AI summary is not available.
/// </summary>
public static class SimpleSummaryBuilder
{
    /// <summary>
    /// Builds a simple markdown summary from standup data.
    /// </summary>
    /// <param name="commits">List of commits to include.</param>
    /// <param name="prs">List of pull requests to include.</param>
    /// <param name="workItems">List of work items to include.</param>
    /// <param name="since">Start of the reporting period.</param>
    /// <param name="until">End of the reporting period.</param>
    /// <returns>A markdown-formatted summary string.</returns>
    public static string Build(
        List<CommitInfo> commits,
        List<PullRequestInfo> prs,
        List<WorkItemInfo> workItems,
        DateTimeOffset since,
        DateTimeOffset until)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Standup Report");
        sb.AppendLine($"**Period:** {since:MMM dd} - {until:MMM dd, yyyy}");
        sb.AppendLine();

        if (commits.Count > 0)
        {
            sb.AppendLine("### Commits");
            foreach (var commit in commits.Take(10))
            {
                var message = commit.Message.Split('\n')[0];
                if (message.Length > 80)
                {
                    message = message[..77] + "...";
                }

                sb.AppendLine($"- {message}");
            }

            if (commits.Count > 10)
            {
                sb.AppendLine($"- ... and {commits.Count - 10} more");
            }

            sb.AppendLine();
        }

        if (prs.Count > 0)
        {
            sb.AppendLine("### Pull Requests");
            foreach (var pr in prs)
            {
                sb.AppendLine($"- [{pr.Status}] {pr.Title}");
            }

            sb.AppendLine();
        }

        if (workItems.Count > 0)
        {
            sb.AppendLine("### Work Items");
            foreach (var item in workItems)
            {
                sb.AppendLine($"- [{item.Status}] {item.Title} ({item.Type})");
            }

            sb.AppendLine();
        }

        if (commits.Count == 0 && prs.Count == 0 && workItems.Count == 0)
        {
            sb.AppendLine("*No activity found for this period.*");
        }

        return sb.ToString();
    }
}
