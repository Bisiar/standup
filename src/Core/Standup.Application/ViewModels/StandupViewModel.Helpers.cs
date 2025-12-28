// -----------------------------------------------------------------------
// <copyright file="StandupViewModel.Helpers.cs" company="JourneyTeam">
//     Copyright (c) JourneyTeam. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Standup.Application.DTOs;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

/// <summary>
/// Partial class containing helper methods for StandupViewModel.
/// </summary>
public partial class StandupViewModel
{
    /// <summary>
    /// Generates a report for a single repository on-the-fly when it's toggled on.
    /// </summary>
    /// <param name="repo">The repository to generate data for.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task GenerateRepoOnTheFlyInternalAsync(GroupedRepository repo)
    {
        IsGenerating = true;
        StatusMessage = $"Generating for {repo.ClientCode}/{repo.Repository}...";

        try
        {
            var tempGroup = new RepositoryGroup
            {
                Id = "temp-single-repo",
                Name = "Temp",
                Repositories = new List<GroupedRepository> { repo },
            };

            var summaryTypes = GetAvailableSummaryTypesInReport().ToList();
            if (summaryTypes.Count == 0)
            {
                summaryTypes = GetSelectedSummaryTypes().ToList();
            }

            var singleRepoReport = await _localStandupService.GenerateGroupedStandupAsync(
                tempGroup,
                async r => await _groupService.GetDecryptedPatForRepositoryAsync(r),
                PeriodStart,
                PeriodEnd,
                summaryTypes.FirstOrDefault());

            foreach (var summaryType in summaryTypes.Skip(1))
            {
                var additionalReport = await _localStandupService.GenerateGroupedStandupAsync(
                    tempGroup,
                    async r => await _groupService.GetDecryptedPatForRepositoryAsync(r),
                    PeriodStart,
                    PeriodEnd,
                    summaryType);

                var mergedSections = singleRepoReport.Sections.Select(s =>
                {
                    var newSection = additionalReport.Sections.FirstOrDefault(n => n.ClientCode == s.ClientCode);
                    return newSection == null ? s : ReportMergeService.MergeSummaries(s, newSection);
                }).ToList();

                singleRepoReport = singleRepoReport with { Sections = mergedSections };
            }

            GroupedReport = ReportMergeService.MergeReports(GroupedReport!, singleRepoReport);
            UpdateReportDisplay(GroupedReport);
            OnPropertyChanged(nameof(AllCommits));
            OnPropertyChanged(nameof(AllPullRequests));
            OnPropertyChanged(nameof(AllWorkItems));
            StatusMessage = $"Added {repo.ClientCode}/{repo.Repository} to report";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// Generates the full grouped standup report with multiple summary types.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task GenerateGroupedStandupInternalAsync()
    {
        if (SelectedGroup == null || SelectedGroup.Repositories.Count == 0)
        {
            StatusMessage = "Please add repositories to the group first.";
            return;
        }

        // Get the list of summary types to generate based on toggles
        var typesToGenerate = GetSelectedSummaryTypes().ToList();
        if (typesToGenerate.Count == 0)
        {
            StatusMessage = "Please select at least one summary type.";
            return;
        }

        // Reset progress
        GenerationProgress = 0;

        // Calculate progress weight per summary type
        var typeCount = typesToGenerate.Count;
        var progressPerType = 1.0 / typeCount;
        var currentTypeIndex = 0;

        // Create progress handler that updates both progress bar and status message
        var progress = new Progress<(double Progress, string Message)>(update =>
        {
            // Scale progress based on which summary type we're generating
            GenerationProgress = (currentTypeIndex * progressPerType) + (update.Progress * progressPerType);
            StatusMessage = update.Message;
        });

        // Generate first type to get the raw data
        var firstType = typesToGenerate[0];
        StatusMessage = $"Starting {firstType} summary generation...";

        GroupedReport = await _localStandupService.GenerateGroupedStandupAsync(
            SelectedGroup,
            async repo => await _groupService.GetDecryptedPatForRepositoryAsync(repo),
            PeriodStart,
            PeriodEnd,
            firstType,
            progress);

        // Generate additional types if selected
        foreach (var summaryType in typesToGenerate.Skip(1))
        {
            currentTypeIndex++;
            StatusMessage = $"Starting {summaryType} summary generation...";

            var additionalReport = await _localStandupService.GenerateGroupedStandupAsync(
                SelectedGroup,
                async repo => await _groupService.GetDecryptedPatForRepositoryAsync(repo),
                PeriodStart,
                PeriodEnd,
                summaryType,
                progress);

            // Merge the new summaries into existing sections
            var mergedSections = GroupedReport.Sections.Select(s =>
            {
                var newSection = additionalReport.Sections.FirstOrDefault(n => n.ClientCode == s.ClientCode);
                return newSection == null ? s : ReportMergeService.MergeSummaries(s, newSection);
            }).ToList();

            GroupedReport = GroupedReport with { Sections = mergedSections };
        }

        // Clear legacy report
        LatestReport = null;

        // Mark progress as complete
        GenerationProgress = 1.0;

        // Update the display content (both markdown and HTML)
        UpdateReportDisplay(GroupedReport);

        // Check for AI failures and show appropriate status
        var typeNames = string.Join(", ", typesToGenerate);
        var aiFailureCount = GroupedReport.Sections
            .Where(s => s.AllSummaries != null)
            .SelectMany(s => s.AllSummaries!.Values)
            .Count(v => v.Contains("AI Summary Unavailable"));

        if (aiFailureCount > 0)
        {
            StatusMessage = $"Generated with {aiFailureCount} AI failures - {GroupedReport.Sections.Count} clients, {GroupedReport.TotalCommits} commits ({typeNames})";
        }
        else
        {
            StatusMessage = $"Generated at {GroupedReport.GeneratedAt:HH:mm} - {GroupedReport.Sections.Count} clients, {GroupedReport.TotalCommits} commits ({typeNames})";
        }

        // Switch to report view and notify property changes
        CurrentViewMode = 1;
        OnPropertyChanged(nameof(HasReport));
        OnPropertyChanged(nameof(AllCommits));
        OnPropertyChanged(nameof(AllPullRequests));
        OnPropertyChanged(nameof(AllWorkItems));
    }
}
