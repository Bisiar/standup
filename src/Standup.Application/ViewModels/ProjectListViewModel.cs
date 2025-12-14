using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Standup.Application.Interfaces;
using Standup.Application.Models;

namespace Standup.Application.ViewModels;

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

    // Edit project properties
    [ObservableProperty]
    private bool _isEditingProject;

    [ObservableProperty]
    private ProjectInstance? _editingProject;

    [ObservableProperty]
    private string _editProjectName = string.Empty;

    [ObservableProperty]
    private string _editTenantName = string.Empty;

    [ObservableProperty]
    private string _editSourcePat = string.Empty;

    [ObservableProperty]
    private string _editAuthorIdentifier = string.Empty;

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
        {
            return;
        }

        var project = new ProjectInstance(
            Id: Guid.NewGuid().ToString(),
            Name: NewProjectName,
            TenantName: NewTenantName,
            ApiEndpoint: NewApiEndpoint);

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

    [RelayCommand]
    private void EditProject(ProjectInstance project)
    {
        EditingProject = project;
        EditProjectName = project.Name;
        EditTenantName = project.TenantName;
        EditSourcePat = project.SourcePat ?? string.Empty;
        EditAuthorIdentifier = project.AuthorIdentifier ?? string.Empty;
        IsEditingProject = true;
    }

    [RelayCommand]
    private async Task SaveEditProjectAsync()
    {
        if (EditingProject == null || string.IsNullOrWhiteSpace(EditProjectName))
        {
            return;
        }

        // Create updated project with new values
        var updatedProject = EditingProject with
        {
            Name = EditProjectName,
            TenantName = EditTenantName,
            SourcePat = string.IsNullOrWhiteSpace(EditSourcePat) ? null : EditSourcePat,
            AuthorIdentifier = string.IsNullOrWhiteSpace(EditAuthorIdentifier) ? null : EditAuthorIdentifier
        };

        await _projectService.UpdateProjectAsync(updatedProject);

        // Update in local list
        var index = Projects.IndexOf(EditingProject);
        if (index >= 0)
        {
            Projects[index] = updatedProject;
        }

        IsEditingProject = false;
        EditingProject = null;
    }

    [RelayCommand]
    private void CancelEditProject()
    {
        IsEditingProject = false;
        EditingProject = null;
        EditProjectName = string.Empty;
        EditTenantName = string.Empty;
        EditSourcePat = string.Empty;
        EditAuthorIdentifier = string.Empty;
    }
}
