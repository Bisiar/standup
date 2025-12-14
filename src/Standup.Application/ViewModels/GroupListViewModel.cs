using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

public partial class GroupListViewModel : ObservableObject
{
    private readonly GroupService _groupService;
    private readonly CredentialService _credentialService;
    private readonly IEncryptionService _encryptionService;
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private ObservableCollection<RepositoryGroup> _groups = new();

    [ObservableProperty]
    private RepositoryGroup? _selectedGroup;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAddingGroup;

    [ObservableProperty]
    private string _newGroupName = string.Empty;

    [ObservableProperty]
    private string _newGroupDescription = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Add Repository Form Properties
    [ObservableProperty]
    private bool _isAddingRepository;

    [ObservableProperty]
    private string? _addingToGroupId;

    [ObservableProperty]
    private string _newRepoClientCode = string.Empty;

    [ObservableProperty]
    private SourceType _newRepoSourceType = SourceType.AzureDevOps;

    [ObservableProperty]
    private string _newRepoOrganization = string.Empty;

    [ObservableProperty]
    private string? _newRepoProject;

    [ObservableProperty]
    private string _newRepoRepository = string.Empty;

    [ObservableProperty]
    private string? _newRepoPat;

    [ObservableProperty]
    private string? _newRepoAuthorIdentifier;

    [ObservableProperty]
    private string? _newRepoLocalPath;

    [ObservableProperty]
    private bool _isLocalRepo;

    [ObservableProperty]
    private string? _detectedRepoInfo;

    public GroupListViewModel(
        GroupService groupService,
        CredentialService credentialService,
        IEncryptionService encryptionService,
        IProjectService projectService)
    {
        _groupService = groupService;
        _credentialService = credentialService;
        _encryptionService = encryptionService;
        _projectService = projectService;
    }

    /// <summary>
    /// Event raised when user wants to add a repository to a group.
    /// </summary>
    public event EventHandler<RepositoryGroup>? OnShowAddRepository;

    /// <summary>
    /// Gets distinct client codes across all groups for use as suggestions.
    /// </summary>
    /// <returns>Up to 10 distinct client codes ordered alphabetically.</returns>
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Coming soon")]
    public IEnumerable<string> GetRecentClientCodes()
    {
        return Groups
            .SelectMany(g => g.Repositories)
            .Select(r => r.ClientCode)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .Take(10);
    }

    private static (SourceType SourceType, string? Org, string? Project, string? Repo) ParseGitRemoteUrl(string gitConfig)
    {
        // Parse git config to find remote "origin" URL
        // Patterns:
        // Azure DevOps: https://dev.azure.com/{org}/{project}/_git/{repo}
        // Azure DevOps: https://{org}@dev.azure.com/{org}/{project}/_git/{repo}
        // GitHub: git@github.com:{org}/{repo}.git
        // GitHub: https://github.com/{org}/{repo}.git
        var lines = gitConfig.Split('\n');
        var inOriginSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed == "[remote \"origin\"]")
            {
                inOriginSection = true;
                continue;
            }

            if (inOriginSection && trimmed.StartsWith("["))
            {
                // Left the origin section
                break;
            }

            if (inOriginSection && trimmed.StartsWith("url = "))
            {
                var url = trimmed.Substring(6).Trim();
                return ParseRemoteUrl(url);
            }
        }

        return (SourceType.GitHub, null, null, null);
    }

    private static (SourceType SourceType, string? Org, string? Project, string? Repo) ParseRemoteUrl(string url)
    {
        Log.Debug("Parsing remote URL: {Url}", url);

        // Azure DevOps patterns
        var adoPattern1 = @"https://dev\.azure\.com/([^/]+)/([^/]+)/_git/(.+?)(?:\.git)?$";
        var adoPattern2 = @"https://[^@]+@dev\.azure\.com/([^/]+)/([^/]+)/_git/(.+?)(?:\.git)?$";
        var adoPattern3 = @"([^/]+)@vs-ssh\.visualstudio\.com:v3/([^/]+)/([^/]+)/(.+?)(?:\.git)?$";

        // GitHub patterns
        var ghPattern1 = @"git@github\.com:([^/]+)/(.+?)(?:\.git)?$";
        var ghPattern2 = @"https://github\.com/([^/]+)/(.+?)(?:\.git)?$";

        // Try Azure DevOps patterns
        var match = System.Text.RegularExpressions.Regex.Match(url, adoPattern1);
        if (match.Success)
        {
            return (SourceType.AzureDevOps, match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
        }

        match = System.Text.RegularExpressions.Regex.Match(url, adoPattern2);
        if (match.Success)
        {
            return (SourceType.AzureDevOps, match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
        }

        match = System.Text.RegularExpressions.Regex.Match(url, adoPattern3);
        if (match.Success)
        {
            return (SourceType.AzureDevOps, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value);
        }

        // Try GitHub patterns
        match = System.Text.RegularExpressions.Regex.Match(url, ghPattern1);
        if (match.Success)
        {
            return (SourceType.GitHub, match.Groups[1].Value, null, match.Groups[2].Value);
        }

        match = System.Text.RegularExpressions.Regex.Match(url, ghPattern2);
        if (match.Success)
        {
            return (SourceType.GitHub, match.Groups[1].Value, null, match.Groups[2].Value);
        }

        Log.Warning("Could not parse remote URL: {Url}", url);
        return (SourceType.GitHub, null, null, null);
    }

    [RelayCommand]
    private async Task LoadGroupsAsync()
    {
        IsLoading = true;
        try
        {
            var groups = await _groupService.GetGroupsAsync();
            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }

            SelectedGroup = await _groupService.GetDefaultGroupAsync() ?? Groups.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectGroupAsync(RepositoryGroup group)
    {
        await _groupService.SetDefaultGroupAsync(group.Id);
        SelectedGroup = group;
    }

    [RelayCommand]
    private void ShowAddGroup()
    {
        IsAddingGroup = true;
        NewGroupName = string.Empty;
        NewGroupDescription = string.Empty;
    }

    [RelayCommand]
    private async Task AddGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGroupName))
        {
            StatusMessage = "Group name is required.";
            return;
        }

        IsLoading = true;
        try
        {
            var group = new RepositoryGroup
            {
                Name = NewGroupName,
                Description = NewGroupDescription
            };

            var added = await _groupService.AddGroupAsync(group);
            Groups.Add(added);
            IsAddingGroup = false;
            StatusMessage = "Group added successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CancelAddGroup()
    {
        IsAddingGroup = false;
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(RepositoryGroup group)
    {
        await _groupService.DeleteGroupAsync(group.Id);
        Groups.Remove(group);
    }

    [RelayCommand]
    private void ShowAddRepository(RepositoryGroup group)
    {
        Log.Information("ShowAddRepository called for group: {GroupId} - {GroupName}", group.Id, group.Name);
        AddingToGroupId = group.Id;
        IsAddingRepository = true;
        ResetAddRepositoryForm();
        OnShowAddRepository?.Invoke(this, group);
    }

    private void ResetAddRepositoryForm()
    {
        NewRepoClientCode = string.Empty;
        NewRepoSourceType = SourceType.AzureDevOps;
        NewRepoOrganization = string.Empty;
        NewRepoProject = null;
        NewRepoRepository = string.Empty;
        NewRepoPat = null;
        NewRepoAuthorIdentifier = null;
        NewRepoLocalPath = null;
        IsLocalRepo = false;
        DetectedRepoInfo = null;
    }

    [RelayCommand]
    private void CancelAddRepository()
    {
        Log.Information("CancelAddRepository called");
        IsAddingRepository = false;
        AddingToGroupId = null;
        ResetAddRepositoryForm();
    }

    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        Log.Information(
            "AddRepositoryAsync called - GroupId: {GroupId}, ClientCode: {ClientCode}, Repo: {Repo}",
            AddingToGroupId,
            NewRepoClientCode,
            NewRepoRepository);

        if (string.IsNullOrWhiteSpace(AddingToGroupId))
        {
            StatusMessage = "No group selected";
            Log.Warning("AddRepository failed: No group selected");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewRepoClientCode))
        {
            StatusMessage = "Client code is required";
            Log.Warning("AddRepository failed: Client code is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewRepoRepository))
        {
            StatusMessage = "Repository name is required";
            Log.Warning("AddRepository failed: Repository name is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewRepoOrganization))
        {
            StatusMessage = "Organization is required";
            Log.Warning("AddRepository failed: Organization is required");
            return;
        }

        // Check for duplicate repository in the group
        var targetGroup = Groups.FirstOrDefault(g => g.Id == AddingToGroupId);
        if (targetGroup != null)
        {
            var isDuplicate = targetGroup.Repositories.Any(r =>
                r.Organization.Equals(NewRepoOrganization, StringComparison.OrdinalIgnoreCase) &&
                r.Repository.Equals(NewRepoRepository, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                StatusMessage = "This repository already exists in the group";
                Log.Warning("AddRepository failed: Duplicate repository {Org}/{Repo}", NewRepoOrganization, NewRepoRepository);
                return;
            }
        }

        IsLoading = true;
        StatusMessage = "Adding repository...";

        try
        {
            string? encryptedPat = null;

            // If PAT is provided, save it BOTH to org-level credential AND repo-level
            // This ensures GetDecryptedPatForRepositoryAsync can find it via either path
            if (!string.IsNullOrWhiteSpace(NewRepoPat))
            {
                // Save to org-level credential so all repos in this org can use it
                await _credentialService.SaveCredentialAsync(NewRepoSourceType, NewRepoOrganization, NewRepoPat);
                Log.Information("PAT saved to org-level credential for {SourceType}/{Org}", NewRepoSourceType, NewRepoOrganization);

                // Also encrypt for repo-level (for repo-specific PAT override scenarios)
                encryptedPat = _encryptionService.Encrypt(NewRepoPat);
                Log.Information("PAT also encrypted at repo-level for repository");
            }

            var repository = new GroupedRepository
            {
                ClientCode = NewRepoClientCode.ToUpperInvariant(),
                SourceType = NewRepoSourceType,
                Organization = NewRepoOrganization,
                Project = NewRepoProject,
                Repository = NewRepoRepository,
                EncryptedPat = encryptedPat,
                AuthorIdentifier = NewRepoAuthorIdentifier,
                LocalPath = NewRepoLocalPath,
                IsActive = true
            };

            Log.Information("Adding repository to group {GroupId}: {Repo}", AddingToGroupId, repository.Repository);
            await _groupService.AddRepositoryToGroupAsync(AddingToGroupId, repository);

            // Also create a project entry for this repository
            var projectName = $"{NewRepoOrganization} {NewRepoRepository}";
            var existingProjects = await _projectService.GetProjectsAsync();
            var projectExists = existingProjects.Any(p =>
                p.SourceOrganization?.Equals(NewRepoOrganization, StringComparison.OrdinalIgnoreCase) == true &&
                p.SourceRepository?.Equals(NewRepoRepository, StringComparison.OrdinalIgnoreCase) == true);

            if (!projectExists)
            {
                var project = new ProjectInstance(
                    Id: string.Empty, // Will be assigned by service
                    Name: projectName,
                    TenantName: NewRepoOrganization,
                    ApiEndpoint: string.Empty,
                    SourceType: NewRepoSourceType,
                    SourceOrganization: NewRepoOrganization,
                    SourceProject: NewRepoProject,
                    SourceRepository: NewRepoRepository,
                    SourcePat: NewRepoPat,
                    AuthorIdentifier: NewRepoAuthorIdentifier,
                    UseLocalGeneration: !string.IsNullOrEmpty(NewRepoLocalPath));

                await _projectService.AddProjectAsync(project);
                Log.Information("Project created for repository: {ProjectName}", projectName);
            }
            else
            {
                Log.Information("Project already exists for {Org}/{Repo}, skipping creation", NewRepoOrganization, NewRepoRepository);
            }

            StatusMessage = "Repository added successfully!";
            IsAddingRepository = false;
            AddingToGroupId = null;
            ResetAddRepositoryForm();

            // Reload groups to ensure we have fresh data
            await LoadGroupsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add repository to group {GroupId}", AddingToGroupId);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Event raised when user wants to browse for a local repository folder.
    /// The View layer should handle this by showing a folder picker.
    /// </summary>
    public event Func<Task<string?>>? OnBrowseForFolder;

    [RelayCommand]
    private async Task BrowseForLocalRepoAsync()
    {
        Log.Information("BrowseForLocalRepo called");

        try
        {
            // Try to get folder path via event (View layer handles the actual picker)
            string? folderPath = null;
            if (OnBrowseForFolder != null)
            {
                folderPath = await OnBrowseForFolder.Invoke();
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                Log.Information("No folder selected");
                return;
            }

            Log.Information("Folder selected: {Path}", folderPath);
            NewRepoLocalPath = folderPath;
            IsLocalRepo = true;

            // Try to parse git config to auto-detect repo info
            var gitConfigPath = Path.Combine(folderPath, ".git", "config");
            if (File.Exists(gitConfigPath))
            {
                var gitConfig = await File.ReadAllTextAsync(gitConfigPath);
                var (sourceType, org, project, repo) = ParseGitRemoteUrl(gitConfig);

                if (!string.IsNullOrEmpty(org) && !string.IsNullOrEmpty(repo))
                {
                    NewRepoSourceType = sourceType;
                    NewRepoOrganization = org;
                    NewRepoProject = project;
                    NewRepoRepository = repo;
                    DetectedRepoInfo = $"Detected: {sourceType} - {org}/{(project != null ? project + "/" : string.Empty)}{repo}";
                    Log.Information("Auto-detected repo: {Info}", DetectedRepoInfo);
                    StatusMessage = DetectedRepoInfo;
                }
                else
                {
                    DetectedRepoInfo = "Could not auto-detect remote. Please enter details manually.";
                    StatusMessage = DetectedRepoInfo;
                    Log.Warning("Could not parse git remote URL from config");
                }
            }
            else
            {
                DetectedRepoInfo = "No .git folder found. Is this a git repository?";
                StatusMessage = DetectedRepoInfo;
                Log.Warning("No .git/config found at {Path}", gitConfigPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error browsing for local repo");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteRepositoryAsync(GroupedRepository repository)
    {
        if (SelectedGroup == null)
        {
            return;
        }

        await _groupService.RemoveRepositoryFromGroupAsync(SelectedGroup.Id, repository.Id);
        SelectedGroup.Repositories.Remove(repository);

        // Notify UI to refresh
        OnPropertyChanged(nameof(SelectedGroup));
    }

    /// <summary>
    /// Remove a repository from its parent group.
    /// This command finds the parent group by searching all groups.
    /// </summary>
    [RelayCommand]
    private async Task RemoveRepositoryAsync(GroupedRepository repository)
    {
        Log.Information("RemoveRepository called for {Repo} (Id: {Id})", repository.Repository, repository.Id);

        // Find the parent group that contains this repository
        var parentGroup = Groups.FirstOrDefault(g => g.Repositories.Any(r => r.Id == repository.Id));
        if (parentGroup == null)
        {
            Log.Warning("Could not find parent group for repository {RepoId}", repository.Id);
            StatusMessage = "Error: Could not find parent group";
            return;
        }

        try
        {
            await _groupService.RemoveRepositoryFromGroupAsync(parentGroup.Id, repository.Id);
            parentGroup.Repositories.Remove(repository);

            // Notify UI to refresh
            OnPropertyChanged(nameof(Groups));
            StatusMessage = $"Removed {repository.Repository}";
            Log.Information("Repository {Repo} removed from group {GroupName}", repository.Repository, parentGroup.Name);

            // Reload to ensure sync
            await LoadGroupsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove repository {Repo}", repository.Repository);
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
