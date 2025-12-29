// <copyright file="ProjectListViewModel.MasterDetail.cs" company="Bisiar">
// Copyright (c) Bisiar. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.Models;
using Standup.Domain.Entities;

namespace Standup.Application.ViewModels;

/// <summary>
/// Master-detail view functionality for ProjectListViewModel.
/// </summary>
public partial class ProjectListViewModel
{
    // Master-Detail UI properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GroupedProjects))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFilterAll))]
    [NotifyPropertyChangedFor(nameof(IsFilterConfigured))]
    [NotifyPropertyChangedFor(nameof(IsFilterNeedsPat))]
    [NotifyPropertyChangedFor(nameof(GroupedProjects))]
    private string _currentFilter = "All";

    [ObservableProperty]
    private ProjectStats _selectedProjectStats = ProjectStats.Empty;

    [ObservableProperty]
    private ObservableCollection<ActivityItem> _recentActivity = new();

    private static string GetRelativeTime(DateTimeOffset timestamp)
    {
        var diff = DateTimeOffset.UtcNow - timestamp;
        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}m ago";
        }

        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}h ago";
        }

        if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}d ago";
        }

        return timestamp.ToString("MMM dd");
    }

    /// <summary>
    /// Gets the projects grouped by organization/tenant for display.
    /// </summary>
    public ObservableCollection<ProjectGroup> GroupedProjects
    {
        get
        {
            var filtered = Projects.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (p.SourceRepository?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    p.TenantName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            // Apply status filter
            filtered = CurrentFilter switch
            {
                "Configured" => filtered.Where(p => !string.IsNullOrEmpty(p.SourcePat)),
                "NeedsPat" => filtered.Where(p => string.IsNullOrEmpty(p.SourcePat)),
                _ => filtered
            };

            // Group by organization/tenant
            var groups = filtered
                .GroupBy(p => p.TenantName ?? "Unknown")
                .OrderBy(g => g.Key)
                .Select(g => new ProjectGroup(g.Key, g.OrderBy(p => p.Name)))
                .ToList();

            return new ObservableCollection<ProjectGroup>(groups);
        }
    }

    /// <summary>
    /// Gets a value indicating whether gets whether the "All" filter is active.
    /// </summary>
    public bool IsFilterAll => CurrentFilter == "All";

    /// <summary>
    /// Gets a value indicating whether gets whether the "Configured" filter is active.
    /// </summary>
    public bool IsFilterConfigured => CurrentFilter == "Configured";

    /// <summary>
    /// Gets a value indicating whether gets whether the "Needs PAT" filter is active.
    /// </summary>
    public bool IsFilterNeedsPat => CurrentFilter == "NeedsPat";

    /// <summary>
    /// Gets the total project count.
    /// </summary>
    public int TotalProjectCount => Projects.Count;

    /// <summary>
    /// Gets the count of configured projects (with PAT).
    /// </summary>
    public int ConfiguredProjectCount => Projects.Count(p => !string.IsNullOrEmpty(p.SourcePat));

    /// <summary>
    /// Gets the count of projects needing PAT.
    /// </summary>
    public int NeedsPatProjectCount => Projects.Count(p => string.IsNullOrEmpty(p.SourcePat));

    /// <summary>
    /// Sets the current filter.
    /// </summary>
    /// <param name="filter">The filter to set.</param>
    [RelayCommand]
    private void SetFilter(string filter)
    {
        CurrentFilter = filter;
        OnPropertyChanged(nameof(GroupedProjects));
    }

    /// <summary>
    /// Loads statistics for the selected project.
    /// </summary>
    private async Task LoadProjectStatsAsync(ProjectInstance project)
    {
        if (_sourceProviderFactory == null || _encryptionService == null)
        {
            SelectedProjectStats = ProjectStats.Empty;
            RecentActivity.Clear();
            return;
        }

        if (string.IsNullOrEmpty(project.SourcePat))
        {
            SelectedProjectStats = ProjectStats.Empty;
            RecentActivity.Clear();
            return;
        }

        try
        {
            var provider = _sourceProviderFactory.GetProvider(project.SourceType);
            var sourceRepo = new SourceRepository
            {
                Id = project.Id,
                SourceType = project.SourceType,
                Organization = project.SourceOrganization ?? string.Empty,
                Project = project.SourceProject,
                Repository = project.SourceRepository ?? string.Empty,
                AuthorIdentifier = project.AuthorIdentifier ?? string.Empty,
                EncryptedPat = await _encryptionService.EncryptAsync(project.SourcePat),
            };

            var since30d = DateTimeOffset.UtcNow.AddDays(-30);
            var until = DateTimeOffset.UtcNow;

            // Fetch data in parallel
            var commitsTask = provider.GetCommitsAsync(sourceRepo, since30d, until);
            var prsTask = provider.GetOpenPullRequestsAsync(sourceRepo);
            var inProgressTask = provider.GetInProgressWorkItemsAsync(sourceRepo);
            var completedTask = provider.GetCompletedWorkItemsAsync(sourceRepo, since30d, until);

            await Task.WhenAll(commitsTask, prsTask, inProgressTask, completedTask);

            var commits = (await commitsTask).ToList();
            var prs = await prsTask;
            var inProgress = await inProgressTask;
            var completed = await completedTask;

            SelectedProjectStats = new ProjectStats(
                CommitCount30d: commits.Count,
                TasksDone: completed.Count(),
                InProgress: inProgress.Count(),
                OpenPRs: prs.Count());

            // Build recent activity from commits
            RecentActivity.Clear();
            var recentCommits = commits
                .OrderByDescending(c => c.CommittedAt)
                .Take(5)
                .Select(c => new ActivityItem(
                    Description: c.Message.Split('\n')[0],
                    TimeAgo: GetRelativeTime(c.CommittedAt),
                    Type: ActivityType.Commit,
                    Timestamp: c.CommittedAt));

            foreach (var item in recentCommits)
            {
                RecentActivity.Add(item);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load project stats for {ProjectName}", project.Name);
            SelectedProjectStats = ProjectStats.Empty;
        }
    }
}
