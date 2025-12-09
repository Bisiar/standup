using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Maui.Models;
using Standup.Maui.Services;
using System.Collections.ObjectModel;

namespace Standup.Maui.ViewModels;

public partial class ProjectListViewModel : ObservableObject
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private ObservableCollection<ProjectInstance> _projects = new();

    [ObservableProperty]
    private ProjectInstance? _selectedProject;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAddingProject;

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newTenantName = string.Empty;

    [ObservableProperty]
    private string _newApiEndpoint = string.Empty;

    public ProjectListViewModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        IsLoading = true;
        try
        {
            var projects = await _projectService.GetProjectsAsync();
            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            SelectedProject = await _projectService.GetCurrentProjectAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectProjectAsync(ProjectInstance project)
    {
        await _projectService.SetCurrentProjectAsync(project.Id);
        SelectedProject = project;
    }

    [RelayCommand]
    private void ShowAddProject()
    {
        IsAddingProject = true;
        NewProjectName = string.Empty;
        NewTenantName = string.Empty;
        NewApiEndpoint = string.Empty;
    }

    [RelayCommand]
    private async Task AddProjectAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName) || string.IsNullOrWhiteSpace(NewApiEndpoint))
            return;

        var project = new ProjectInstance
        {
            Name = NewProjectName,
            TenantName = NewTenantName,
            ApiEndpoint = NewApiEndpoint
        };

        var added = await _projectService.AddProjectAsync(project);
        Projects.Add(added);
        IsAddingProject = false;
    }

    [RelayCommand]
    private void CancelAddProject()
    {
        IsAddingProject = false;
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(ProjectInstance project)
    {
        await _projectService.DeleteProjectAsync(project.Id);
        Projects.Remove(project);
    }
}
