using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Application.Models;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;

namespace Standup.Application.ViewModels;

public partial class RepositoryConfigViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IStandupApiClient _apiClient;
    private readonly GroupService _groupService;
    private readonly CredentialService _credentialService;
    private readonly IEncryptionService _encryptionService;

    private RepositoryGroup? _currentGroup;

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private ObservableCollection<SourceRepositoryDto> _repositories = new();

    // Group mode properties
    [ObservableProperty]
    private bool _isGroupMode;

    [ObservableProperty]
    private string _pageTitle = "Repository Configuration";

    [ObservableProperty]
    private ObservableCollection<SelectableProject> _selectableProjects = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAddingRepository;

    [ObservableProperty]
    private SourceType _newSourceType = SourceType.GitHub;

    [ObservableProperty]
    private string _newOrganization = string.Empty;

    [ObservableProperty]
    private string _newProject = string.Empty;

    [ObservableProperty]
    private string _newRepository = string.Empty;

    [ObservableProperty]
    private string _newAuthorIdentifier = string.Empty;

    [ObservableProperty]
    private string _newPat = string.Empty;

    [ObservableProperty]
    private string _newClientCode = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Event raised when the modal should close.
    /// </summary>
    public event EventHandler? OnClose;

    public RepositoryConfigViewModel(
        IProjectService projectService,
        IStandupApiClient apiClient,
        GroupService groupService,
        CredentialService credentialService,
        IEncryptionService encryptionService)
    {
        _projectService = projectService;
        _apiClient = apiClient;
        _groupService = groupService;
        _credentialService = credentialService;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Sets the group to configure repositories for.
    /// </summary>
    /// <param name="group">The repository group to configure.</param>
    public async void SetGroup(RepositoryGroup group)
    {
        Log.Information("RepositoryConfigViewModel.SetGroup called for {GroupName}", group.Name);
        _currentGroup = group;
        IsGroupMode = true;
        PageTitle = $"{group.Name} - Select Repositories";
        await LoadSelectableProjectsAsync();
    }

    private async Task LoadSelectableProjectsAsync()
    {
        SelectableProjects.Clear();
        if (_currentGroup == null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            // Get all configured projects
            var allProjects = await _projectService.GetProjectsAsync();

            foreach (var project in allProjects)
            {
                // Check if this project is already in the group
                var existingInGroup = _currentGroup.Repositories.FirstOrDefault(r =>
                    r.SourceType == project.SourceType &&
                    r.Organization == project.SourceOrganization &&
                    r.Repository == project.SourceRepository);

                var isSelected = existingInGroup != null;
                var clientCode = existingInGroup?.ClientCode ?? string.Empty;

                var selectable = new SelectableProject(project, isSelected, clientCode, OnProjectSelectionChangedAsync);
                SelectableProjects.Add(selectable);
            }

            Log.Information("Loaded {Count} projects for group selection", SelectableProjects.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load projects");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OnProjectSelectionChangedAsync(SelectableProject selectable)
    {
        if (_currentGroup == null)
        {
            return;
        }

        try
        {
            if (selectable.IsSelected)
            {
                // Add to group - create GroupedRepository from ProjectInstance
                var project = selectable.Project;

                // Check if already exists
                var existing = _currentGroup.Repositories.FirstOrDefault(r =>
                    r.SourceType == project.SourceType &&
                    r.Organization == project.SourceOrganization &&
                    r.Repository == project.SourceRepository);

                if (existing == null)
                {
                    var repository = new GroupedRepository
                    {
                        ClientCode = selectable.ClientCode.ToUpperInvariant(),
                        SourceType = project.SourceType,
                        Organization = project.SourceOrganization ?? string.Empty,
                        Project = project.SourceProject,
                        Repository = project.SourceRepository ?? string.Empty,
                        EncryptedPat = !string.IsNullOrEmpty(project.SourcePat)
                            ? await _encryptionService.EncryptAsync(project.SourcePat)
                            : null,
                        AuthorIdentifier = project.AuthorIdentifier,
                        IsActive = true
                    };

                    await _groupService.AddRepositoryToGroupAsync(_currentGroup.Id, repository);
                    _currentGroup.Repositories.Add(repository);
                    Log.Information("Added {Repo} to group {Group}", project.Name, _currentGroup.Name);
                    StatusMessage = $"Added {project.Name}";
                }
            }
            else
            {
                // Remove from group
                var project = selectable.Project;
                var existing = _currentGroup.Repositories.FirstOrDefault(r =>
                    r.SourceType == project.SourceType &&
                    r.Organization == project.SourceOrganization &&
                    r.Repository == project.SourceRepository);

                if (existing != null)
                {
                    await _groupService.RemoveRepositoryFromGroupAsync(_currentGroup.Id, existing.Id);
                    _currentGroup.Repositories.Remove(existing);
                    Log.Information("Removed {Repo} from group {Group}", project.Name, _currentGroup.Name);
                    StatusMessage = $"Removed {project.Name}";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update group membership");
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleAll()
    {
        if (SelectableProjects.Count == 0)
        {
            return;
        }

        // If all are selected, deselect all; otherwise select all
        var allSelected = SelectableProjects.All(p => p.IsSelected);
        foreach (var project in SelectableProjects)
        {
            project.IsSelected = !allSelected;
        }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        Log.Information("RepositoryConfigViewModel.Close called");
        OnClose?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            CurrentProject = await _projectService.GetCurrentProjectAsync();

            if (CurrentProject != null)
            {
                _apiClient.SetProject(CurrentProject.ApiEndpoint, CurrentProject.AccessToken);

                if (!string.IsNullOrEmpty(CurrentProject.UserId))
                {
                    var repos = await _apiClient.GetRepositoriesAsync(CurrentProject.UserId);
                    Repositories.Clear();
                    foreach (var repo in repos)
                    {
                        Repositories.Add(repo);
                    }
                }
            }
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
    private void ShowAddRepository()
    {
        IsAddingRepository = true;
        NewSourceType = SourceType.AzureDevOps;
        NewOrganization = string.Empty;
        NewProject = string.Empty;
        NewRepository = string.Empty;
        NewAuthorIdentifier = string.Empty;
        NewPat = string.Empty;
    }

    [RelayCommand]
    private async Task AddRepositoryAsync()
    {
        if (CurrentProject == null || string.IsNullOrEmpty(CurrentProject.UserId))
        {
            StatusMessage = "Please configure your user ID in settings first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewOrganization) || string.IsNullOrWhiteSpace(NewRepository))
        {
            StatusMessage = "Organization and repository are required.";
            return;
        }

        IsLoading = true;
        try
        {
            var dto = new CreateSourceRepositoryDto(
                SourceType: NewSourceType,
                Organization: NewOrganization,
                Project: NewSourceType == SourceType.AzureDevOps ? NewProject : null,
                Repository: NewRepository,
                DisplayName: null,
                AuthorIdentifier: NewAuthorIdentifier,
                PersonalAccessToken: string.IsNullOrEmpty(NewPat) ? null : NewPat);

            var added = await _apiClient.AddRepositoryAsync(CurrentProject.UserId, dto);
            Repositories.Add(added);
            IsAddingRepository = false;
            StatusMessage = "Repository added successfully!";
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
    private void CancelAddRepository()
    {
        IsAddingRepository = false;
    }
}
