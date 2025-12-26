using Standup.Application.DTOs;
using Standup.Domain.Enums;

namespace Standup.Application.Services;

/// <summary>
/// Service for merging standup reports together.
/// </summary>
public static class ReportMergeService
{
    /// <summary>
    /// Merges a newly generated single-repo report into an existing grouped report.
    /// </summary>
    /// <param name="existingReport">The existing report to merge into.</param>
    /// <param name="newReport">The new report to merge.</param>
    /// <returns>The merged report.</returns>
    public static GroupedStandupReportDto MergeReports(
        GroupedStandupReportDto existingReport,
        GroupedStandupReportDto newReport)
    {
        if (newReport.Sections.Count == 0)
        {
            return existingReport;
        }

        var newSection = newReport.Sections[0];
        var existingSections = existingReport.Sections.ToList();

        // Check if there's already a section with this client code
        var existingIndex = existingSections.FindIndex(s => s.ClientCode == newSection.ClientCode);

        if (existingIndex >= 0)
        {
            existingSections[existingIndex] = MergeSections(existingSections[existingIndex], newSection);
        }
        else
        {
            // Add as new section, maintaining alphabetical order by client code
            existingSections.Add(newSection);
            existingSections = existingSections.OrderBy(s => s.ClientCode).ToList();
        }

        // Recalculate totals
        var totalCommits = existingSections.Sum(s => s.Commits.Count);
        var totalPRs = existingSections.Sum(s => s.PullRequests.Count);
        var totalWorkItems = existingSections.Sum(s => s.WorkItems.Count);

        return existingReport with
        {
            Sections = existingSections,
            TotalCommits = totalCommits,
            TotalPullRequests = totalPRs,
            TotalWorkItems = totalWorkItems,
        };
    }

    /// <summary>
    /// Merges additional summary types into an existing report section.
    /// </summary>
    /// <param name="existingSection">The existing section.</param>
    /// <param name="newSection">The new section with additional summaries.</param>
    /// <returns>The merged section.</returns>
    public static ClientCodeSection MergeSummaries(
        ClientCodeSection existingSection,
        ClientCodeSection newSection)
    {
        var mergedSummaries = existingSection.AllSummaries != null
            ? new Dictionary<SummaryType, string>(existingSection.AllSummaries)
            : new Dictionary<SummaryType, string>();

        if (newSection.AllSummaries != null)
        {
            foreach (var kvp in newSection.AllSummaries)
            {
                mergedSummaries[kvp.Key] = kvp.Value;
            }
        }

        return existingSection with { AllSummaries = mergedSummaries };
    }

    /// <summary>
    /// Merges two sections with the same client code.
    /// </summary>
    private static ClientCodeSection MergeSections(
        ClientCodeSection existingSection,
        ClientCodeSection newSection)
    {
        var mergedCommits = existingSection.Commits.Concat(newSection.Commits)
            .DistinctBy(c => c.Sha)
            .OrderByDescending(c => c.CommittedAt)
            .ToList();

        var mergedPRs = existingSection.PullRequests.Concat(newSection.PullRequests)
            .DistinctBy(p => p.Id)
            .ToList();

        var mergedWorkItems = existingSection.WorkItems.Concat(newSection.WorkItems)
            .DistinctBy(w => w.Id)
            .ToList();

        // Merge summaries
        var mergedSummaries = existingSection.AllSummaries != null
            ? new Dictionary<SummaryType, string>(existingSection.AllSummaries)
            : new Dictionary<SummaryType, string>();

        if (newSection.AllSummaries != null)
        {
            foreach (var kvp in newSection.AllSummaries)
            {
                // Append new summary to existing if present
                if (mergedSummaries.TryGetValue(kvp.Key, out var existing))
                {
                    mergedSummaries[kvp.Key] = $"{existing}\n\n{kvp.Value}";
                }
                else
                {
                    mergedSummaries[kvp.Key] = kvp.Value;
                }
            }
        }

        return existingSection with
        {
            Commits = mergedCommits,
            PullRequests = mergedPRs,
            WorkItems = mergedWorkItems,
            AllSummaries = mergedSummaries,
            Summary = newSection.Summary ?? existingSection.Summary,
        };
    }
}
