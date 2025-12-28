// <copyright file="GroupListViewModel.MasterDetail.cs" company="Bisiar">
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
/// Master-detail view functionality for GroupListViewModel.
/// </summary>
public partial class GroupListViewModel
{
    /// <summary>
    /// Gets the relative time string for a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to convert.</param>
    /// <returns>A human-readable relative time string.</returns>
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
    /// Event raised when user wants to navigate to standup for a group.
    /// </summary>
    public event EventHandler<RepositoryGroup>? OnNavigateToStandup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredGroups))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private GroupStats _selectedGroupStats = GroupStats.Empty;

    [ObservableProperty]
    private ObservableCollection<ActivityItem> _groupRecentActivity = new();

    /// <summary>
    /// Gets the filtered groups based on search text.
    /// </summary>
    public ObservableCollection<RepositoryGroup> FilteredGroups
    {
        get
        {
            var filtered = Groups.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(g =>
                    (g.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (g.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    g.Repositories.Any(r =>
                        r.Repository?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return new ObservableCollection<RepositoryGroup>(filtered.OrderBy(g => g.Name));
        }
    }

    /// <summary>
    /// Gets the total group count.
    /// </summary>
    public int TotalGroupCount => Groups.Count;

    /// <summary>
    /// Gets the count of groups included in generation.
    /// </summary>
    public int IncludedGroupCount => Groups.Count(g => g.IncludeInGeneration);

    /// <summary>
    /// Gets the count of groups excluded from generation.
    /// </summary>
    public int ExcludedGroupCount => Groups.Count(g => !g.IncludeInGeneration);

    /// <summary>
    /// Sets a group as the default group (starred).
    /// </summary>
    /// <param name="group">The group to set as default.</param>
    /// <returns>A task representing the async operation.</returns>
    [RelayCommand]
    private async Task SetDefaultGroupAsync(RepositoryGroup group)
    {
        Log.Information("SetDefaultGroup called for group: {GroupName}", group.Name);

        // Toggle off if already default
        if (group.IsDefault)
        {
            group.IsDefault = false;
            await _groupService.UpdateGroupAsync(group);
        }
        else
        {
            // Clear existing default
            foreach (var g in Groups.Where(g => g.IsDefault))
            {
                g.IsDefault = false;
            }

            group.IsDefault = true;
            await _groupService.SetDefaultGroupAsync(group.Id);
        }

        // Refresh the UI
        OnPropertyChanged(nameof(FilteredGroups));
        OnPropertyChanged(nameof(SelectedGroup));
        await LoadGroupsAsync();
    }

    /// <summary>
    /// Command to navigate to standup generation for a group.
    /// </summary>
    /// <param name="group">The group to generate standup for.</param>
    [RelayCommand]
    private void NavigateToStandup(RepositoryGroup group)
    {
        Log.Information("NavigateToStandup called for group: {GroupName}", group.Name);
        OnNavigateToStandup?.Invoke(this, group);
    }

    /// <summary>
    /// Updates the stats for the currently selected group.
    /// </summary>
    private void UpdateSelectedGroupStats()
    {
        if (SelectedGroup == null)
        {
            SelectedGroupStats = GroupStats.Empty;
            GroupRecentActivity.Clear();
            return;
        }

        var repoCount = SelectedGroup.Repositories.Count;
        var reposWithPat = SelectedGroup.Repositories.Count(r => !string.IsNullOrEmpty(r.EncryptedPat));

        // For now, commit count is a placeholder - would need to integrate with source provider
        // to get actual commit counts across all repos in the group
        SelectedGroupStats = new GroupStats(
            RepositoryCount: repoCount,
            CommitCount7d: 0,
            ReposWithPat: reposWithPat);

        Log.Debug(
            "Updated stats for group {GroupName}: Repos={RepoCount}, WithPat={WithPat}",
            SelectedGroup.Name,
            repoCount,
            reposWithPat);
    }

    /// <summary>
    /// Called when SelectedGroup changes to update stats.
    /// </summary>
    partial void OnSelectedGroupChanged(RepositoryGroup? value)
    {
        UpdateSelectedGroupStats();
        OnPropertyChanged(nameof(FilteredGroups));
    }
}
