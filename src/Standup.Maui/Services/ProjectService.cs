using Standup.Maui.Models;
using System.Text.Json;

namespace Standup.Maui.Services;

public class ProjectService : IProjectService
{
    private const string ProjectsKey = "standup_projects";
    private const string CurrentProjectKey = "standup_current_project";
    private List<ProjectInstance>? _cachedProjects;

    public async Task<IEnumerable<ProjectInstance>> GetProjectsAsync()
    {
        if (_cachedProjects != null)
            return _cachedProjects;

        var json = await SecureStorage.Default.GetAsync(ProjectsKey);
        if (string.IsNullOrEmpty(json))
        {
            _cachedProjects = new List<ProjectInstance>
            {
                new ProjectInstance
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "JourneyTeam",
                    TenantName = "JourneyTeam",
                    ApiEndpoint = "https://standup-journeyteam.azurewebsites.net",
                    IsDefault = true
                }
            };
            await SaveProjectsAsync();
        }
        else
        {
            _cachedProjects = JsonSerializer.Deserialize<List<ProjectInstance>>(json) ?? new();
        }

        return _cachedProjects;
    }

    public async Task<ProjectInstance?> GetCurrentProjectAsync()
    {
        var currentId = await SecureStorage.Default.GetAsync(CurrentProjectKey);
        var projects = await GetProjectsAsync();

        if (!string.IsNullOrEmpty(currentId))
        {
            var current = projects.FirstOrDefault(p => p.Id == currentId);
            if (current != null)
                return current;
        }

        return projects.FirstOrDefault(p => p.IsDefault) ?? projects.FirstOrDefault();
    }

    public async Task<ProjectInstance> AddProjectAsync(ProjectInstance project)
    {
        var projects = (await GetProjectsAsync()).ToList();

        project.Id = Guid.NewGuid().ToString();
        project.CreatedAt = DateTimeOffset.UtcNow;

        if (project.IsDefault)
        {
            foreach (var p in projects)
                p.IsDefault = false;
        }

        projects.Add(project);
        _cachedProjects = projects;
        await SaveProjectsAsync();

        return project;
    }

    public async Task<ProjectInstance> UpdateProjectAsync(ProjectInstance project)
    {
        var projects = (await GetProjectsAsync()).ToList();
        var index = projects.FindIndex(p => p.Id == project.Id);

        if (index < 0)
            throw new InvalidOperationException($"Project {project.Id} not found");

        if (project.IsDefault)
        {
            foreach (var p in projects)
                p.IsDefault = false;
        }

        projects[index] = project;
        _cachedProjects = projects;
        await SaveProjectsAsync();

        return project;
    }

    public async Task DeleteProjectAsync(string projectId)
    {
        var projects = (await GetProjectsAsync()).ToList();
        projects.RemoveAll(p => p.Id == projectId);
        _cachedProjects = projects;
        await SaveProjectsAsync();
    }

    public async Task SetCurrentProjectAsync(string projectId)
    {
        await SecureStorage.Default.SetAsync(CurrentProjectKey, projectId);
    }

    private async Task SaveProjectsAsync()
    {
        if (_cachedProjects == null)
            return;

        var json = JsonSerializer.Serialize(_cachedProjects);
        await SecureStorage.Default.SetAsync(ProjectsKey, json);
    }
}
