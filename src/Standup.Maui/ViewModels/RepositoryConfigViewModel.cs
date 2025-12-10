using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.DTOs;
using Standup.Domain.Enums;
using Standup.Maui.Models;
using Standup.Maui.Services;
using System.Collections.ObjectModel;

namespace Standup.Maui.ViewModels;

public partial class RepositoryConfigViewModel : ObservableObject
{
    private readonly IProjectService _projectService;
    private readonly IStandupApiClient _apiClient;

    [ObservableProperty]
    private ProjectInstance? _currentProject;

    [ObservableProperty]
    private ObservableCollection<SourceRepositoryDto> _repositories = new();

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
    private string _statusMessage = string.Empty;

    public RepositoryConfigViewModel(IProjectService projectService, IStandupApiClient apiClient)
    {
        _projectService = projectService;
        _apiClient = apiClient;
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
        NewSourceType = SourceType.GitHub;
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
