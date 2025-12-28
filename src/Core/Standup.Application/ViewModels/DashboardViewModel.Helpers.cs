using System.Collections.ObjectModel;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Models;
using Standup.Domain.Entities;

namespace Standup.Application.ViewModels;

/// <summary>
/// Helper methods for the Dashboard ViewModel.
/// </summary>
public partial class DashboardViewModel
{
    private void RefreshClientCodeMetrics(List<CommitInfo> filteredCommits)
    {
        // Try to get client code from report sections first
        if (Report?.Sections != null && Report.Sections.Count > 0)
        {
            var clientCodeData = Report.Sections
                .Select(s => new ClientCodeMetrics
                {
                    ClientCode = string.IsNullOrWhiteSpace(s.ClientCode) ? "General" : s.ClientCode,
                    Additions = s.Commits
                        .Where(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd)
                        .Sum(c => c.Additions),
                    Deletions = s.Commits
                        .Where(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd)
                        .Sum(c => c.Deletions),
                    CommitCount = s.Commits
                        .Count(c => c.CommittedAt >= PeriodStart && c.CommittedAt <= PeriodEnd),
                })
                .Where(c => c.CommitCount > 0)
                .OrderByDescending(c => c.CommitCount)
                .ToList();

            ClientCodeMetrics = new ObservableCollection<ClientCodeMetrics>(clientCodeData);
        }
        else if (SelectedGroup?.Repositories != null)
        {
            // Fall back to grouping by project name (from Projects tab) or repo name
            var repoToDisplayName = SelectedGroup.Repositories
                .ToDictionary(r => r.Repository, r => GetProjectDisplayName(r.Repository));

            var clientCodeData = filteredCommits
                .GroupBy(c => repoToDisplayName.TryGetValue(c.Repository, out var name) ? name : GetProjectDisplayName(c.Repository))
                .Select(g => new ClientCodeMetrics
                {
                    ClientCode = g.Key,
                    Additions = g.Sum(c => c.Additions),
                    Deletions = g.Sum(c => c.Deletions),
                    CommitCount = g.Count(),
                })
                .OrderByDescending(c => c.CommitCount)
                .ToList();

            ClientCodeMetrics = new ObservableCollection<ClientCodeMetrics>(clientCodeData);
        }
        else
        {
            ClientCodeMetrics.Clear();
        }
    }

    private void RefreshWarnings()
    {
        CriticalWarnings.Clear();
        Warnings.Clear();
        InfoWarnings.Clear();

        // Check report sections for fetch errors
        if (Report?.Sections != null)
        {
            foreach (var section in Report.Sections)
            {
                var status = section.SourceStatus;
                if (status == null)
                {
                    continue;
                }

                var clientCode = string.IsNullOrWhiteSpace(section.ClientCode) ? "General" : section.ClientCode;

                // Critical: Fetch errors
                if (status.CommitsStatus == FetchStatus.Error)
                {
                    CriticalWarnings.Add(new GroupWarning
                    {
                        Severity = WarningSeverity.Critical,
                        ClientCode = clientCode,
                        Message = "Commit fetch failed",
                        Details = status.CommitsError ?? "Unknown error",
                        FixTarget = "Groups",
                    });
                }

                if (status.PullRequestsStatus == FetchStatus.Error)
                {
                    CriticalWarnings.Add(new GroupWarning
                    {
                        Severity = WarningSeverity.Critical,
                        ClientCode = clientCode,
                        Message = "PR fetch failed",
                        Details = status.PullRequestsError ?? "Unknown error",
                        FixTarget = "Settings",
                    });
                }

                // Warning: No PAT (consolidate PRs and Work Items)
                if (status.PullRequestsStatus == FetchStatus.NoPat || status.WorkItemsStatus == FetchStatus.NoPat)
                {
                    var missingFor = new List<string>();
                    if (status.PullRequestsStatus == FetchStatus.NoPat)
                    {
                        missingFor.Add("PRs");
                    }

                    if (status.WorkItemsStatus == FetchStatus.NoPat)
                    {
                        missingFor.Add("Work Items");
                    }

                    Warnings.Add(new GroupWarning
                    {
                        Severity = WarningSeverity.Warning,
                        ClientCode = clientCode,
                        Message = "No PAT configured",
                        Details = $"{string.Join(" and ", missingFor)} unavailable",
                        FixTarget = "Settings",
                    });
                }

                // Info: No activity in period
                if (section.CommitCount == 0 && section.PullRequestCount == 0)
                {
                    InfoWarnings.Add(new GroupWarning
                    {
                        Severity = WarningSeverity.Info,
                        ClientCode = clientCode,
                        Message = "No activity",
                        Details = "0 commits in selected period",
                    });
                }
            }
        }

        // Check repository configuration issues from selected group
        if (SelectedGroup?.Repositories != null)
        {
            foreach (var repo in SelectedGroup.Repositories)
            {
                var clientCode = string.IsNullOrWhiteSpace(repo.ClientCode) ? "General" : repo.ClientCode;

                if (string.IsNullOrEmpty(repo.LocalPath))
                {
                    Warnings.Add(new GroupWarning
                    {
                        Severity = WarningSeverity.Warning,
                        ClientCode = clientCode,
                        RepositoryName = repo.Repository,
                        Message = "No local path configured",
                        Details = repo.Repository,
                        FixTarget = "Groups",
                    });
                }

                if (!repo.IsActive)
                {
                    InfoWarnings.Add(new GroupWarning
                    {
                        Severity = WarningSeverity.Info,
                        ClientCode = clientCode,
                        RepositoryName = repo.Repository,
                        Message = "Repository disabled",
                        Details = repo.Repository,
                    });
                }
            }
        }
    }

    private void NotifyCalculatedPropertiesChanged()
    {
        OnPropertyChanged(nameof(NetChange));
        OnPropertyChanged(nameof(ActiveProjectCount));
        OnPropertyChanged(nameof(TotalWarningCount));
        OnPropertyChanged(nameof(HasCriticalWarnings));
        OnPropertyChanged(nameof(HasAnyWarnings));
        OnPropertyChanged(nameof(IsGroupHealthy));
        OnPropertyChanged(nameof(AllWarnings));
    }

    /// <summary>
    /// Generates top contributors stats from the filtered commits.
    /// </summary>
    /// <param name="filteredCommits">The filtered commits to process.</param>
    private void RefreshTopContributors(List<CommitInfo> filteredCommits)
    {
        if (filteredCommits.Count == 0)
        {
            TopContributors.Clear();
            return;
        }

        var contributorData = filteredCommits
            .GroupBy(c => c.AuthorEmail ?? c.Author ?? "Unknown")
            .Select(g => new ContributorStats
            {
                Name = g.First().Author ?? g.Key,
                Email = g.Key,
                CommitCount = g.Count(),
                Additions = g.Sum(c => c.Additions),
                Deletions = g.Sum(c => c.Deletions),
            })
            .OrderByDescending(c => c.CommitCount)
            .Take(5)
            .ToList();

        TopContributors = new ObservableCollection<ContributorStats>(contributorData);
    }

    /// <summary>
    /// Gets the project display name for a repository.
    /// Falls back to repo name if no project mapping exists.
    /// </summary>
    /// <param name="repoName">The repository name.</param>
    /// <returns>The project display name.</returns>
    private string GetProjectDisplayName(string repoName)
    {
        if (_repoToProjectName.TryGetValue(repoName, out var projectName))
        {
            return projectName;
        }

        return repoName;
    }

    /// <summary>
    /// Generates per-project daily activity data for the multi-line trend chart.
    /// Groups commits by project and date, calculating total activity (additions + deletions).
    /// </summary>
    /// <param name="commits">The filtered commits to process.</param>
    private void GenerateProjectDailyActivities(List<CommitInfo> commits)
    {
        var newProjectData = new Dictionary<string, ObservableCollection<ProjectDailyActivity>>();

        if (SelectedGroup?.Repositories == null || commits.Count == 0)
        {
            ProjectDailyActivities = newProjectData;
            return;
        }

        // Create mapping from repo to project display name
        var repoToDisplayName = SelectedGroup.Repositories
            .ToDictionary(r => r.Repository, r => GetProjectDisplayName(r.Repository));

        // Group commits by project and date
        var projectDateGroups = commits
            .GroupBy(c => repoToDisplayName.TryGetValue(c.Repository, out var name) ? name : GetProjectDisplayName(c.Repository))
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(c => c.CommittedAt.Date)
                      .Select(dg => new ProjectDailyActivity
                      {
                          ProjectName = g.Key,
                          Date = dg.Key,
                          TotalActivity = dg.Sum(c => c.Additions + c.Deletions),
                          CommitCount = dg.Count(),
                      })
                      .OrderBy(d => d.Date)
                      .ToList());

        // Convert to ObservableCollections
        foreach (var kvp in projectDateGroups)
        {
            newProjectData[kvp.Key] = new ObservableCollection<ProjectDailyActivity>(kvp.Value);
        }

        ProjectDailyActivities = newProjectData;

        Log.Debug(
            "Dashboard: Generated daily activity for {ProjectCount} projects",
            newProjectData.Count);
    }
}
