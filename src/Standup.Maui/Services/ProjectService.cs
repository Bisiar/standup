using System.Text.Json;
using Serilog;
using Standup.Application.Interfaces;
using Standup.Application.Models;

namespace Standup.Maui.Services;

public class ProjectService : IProjectService
{
    private const string ProjectsKey = "standup_projects";
    private const string CurrentProjectKey = "standup_current_project";
    private List<ProjectInstance>? _cachedProjects;

    public async Task<IEnumerable<ProjectInstance>> GetProjectsAsync()
    {
        Log.Information("ProjectService.GetProjectsAsync called");

        if (_cachedProjects != null)
        {
            Log.Information("Returning {Count} cached projects", _cachedProjects.Count);
            return _cachedProjects;
        }

        string? json = null;
        try
        {
            Log.Information("Attempting to read from Preferences key: {Key}", ProjectsKey);
            json = GetPreference(ProjectsKey);
            Log.Information("Preferences read result: {HasData}", !string.IsNullOrEmpty(json));
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Preferences.Get failed for key {Key}. Type: {ExType}, Message: {Message}",
                ProjectsKey,
                ex.GetType().Name,
                ex.Message);
        }

        if (string.IsNullOrEmpty(json))
        {
            Log.Information("No existing projects found, creating default UPREHS project");
            _cachedProjects =
            [
                new ProjectInstance(
                    Id: Guid.NewGuid().ToString(),
                    Name: "UPREHS AI-Chat-Bot",
                    TenantName: "UPREHS",
                    ApiEndpoint: "https://standup-journeyteam.azurewebsites.net",
                    IsDefault: true,
                    UseLocalGeneration: true,
                    SourceType: Domain.Enums.SourceType.AzureDevOps,
                    SourceOrganization: "UPREHS",
                    SourceProject: "AI-Chat-Bot",
                    SourceRepository: "AI-Chat-Bot",
                    SourcePat: string.Empty, // User must configure PAT in Settings
                    AuthorIdentifier: string.Empty)
            ];

            Log.Information(
                "Default project created: {Name}, Org: {Org}, Project: {Project}",
                _cachedProjects[0].Name,
                _cachedProjects[0].SourceOrganization,
                _cachedProjects[0].SourceProject);

            await SaveProjectsAsync();
        }
        else
        {
            Log.Information("Deserializing projects from Preferences, JSON length: {Length}", json.Length);
            try
            {
                _cachedProjects = JsonSerializer.Deserialize<List<ProjectInstance>>(json) ?? new();
                Log.Information("Deserialized {Count} projects", _cachedProjects.Count);

                foreach (var p in _cachedProjects)
                {
                    Log.Information(
                        "  Project: {Name}, Org: {Org}, UseLocal: {UseLocal}",
                        p.Name,
                        p.SourceOrganization,
                        p.UseLocalGeneration);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to deserialize projects JSON");
                _cachedProjects = new List<ProjectInstance>();
            }
        }

        return _cachedProjects;
    }

    public async Task<ProjectInstance?> GetCurrentProjectAsync()
    {
        Log.Information("ProjectService.GetCurrentProjectAsync called");

        string? currentId = null;
        try
        {
            currentId = GetPreference(CurrentProjectKey);
            Log.Information("Current project ID from Preferences: {Id}", currentId ?? "(null)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Preferences.Get failed for CurrentProjectKey. Type: {ExType}", ex.GetType().Name);
        }

        var projects = await GetProjectsAsync();

        if (!string.IsNullOrEmpty(currentId))
        {
            var current = projects.FirstOrDefault(p => p.Id == currentId);
            if (current != null)
            {
                Log.Information("Found current project by ID: {Name}", current.Name);
                return current;
            }
        }

        var defaultProject = projects.FirstOrDefault(p => p.IsDefault) ?? projects.FirstOrDefault();
        Log.Information("Returning default/first project: {Name}", defaultProject?.Name ?? "(none)");
        return defaultProject;
    }

    public async Task<ProjectInstance> AddProjectAsync(ProjectInstance project)
    {
        Log.Information("ProjectService.AddProjectAsync called for: {Name}", project.Name);

        var projects = (await GetProjectsAsync()).ToList();

        var newProject = project with
        {
            Id = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (newProject.IsDefault)
        {
            projects = projects.Select(p => p with { IsDefault = false }).ToList();
        }

        projects.Add(newProject);
        _cachedProjects = projects;
        await SaveProjectsAsync();

        Log.Information("Project added with ID: {Id}", newProject.Id);
        return newProject;
    }

    public async Task<ProjectInstance> UpdateProjectAsync(ProjectInstance project)
    {
        Log.Information("ProjectService.UpdateProjectAsync called for ID: {Id}, Name: {Name}", project.Id, project.Name);

        var projects = (await GetProjectsAsync()).ToList();
        var index = projects.FindIndex(p => p.Id == project.Id);

        if (index < 0)
        {
            Log.Error("Project not found: {Id}", project.Id);
            throw new InvalidOperationException($"Project {project.Id} not found");
        }

        if (project.IsDefault)
        {
            projects = projects.Select(p => p with { IsDefault = false }).ToList();
        }

        projects[index] = project;
        _cachedProjects = projects;
        await SaveProjectsAsync();

        Log.Information("Project updated successfully");
        return project;
    }

    public async Task DeleteProjectAsync(string projectId)
    {
        Log.Information("ProjectService.DeleteProjectAsync called for ID: {Id}", projectId);

        var projects = (await GetProjectsAsync()).ToList();
        var removed = projects.RemoveAll(p => p.Id == projectId);
        _cachedProjects = projects;
        await SaveProjectsAsync();

        Log.Information("Removed {Count} project(s)", removed);
    }

    public async Task SetCurrentProjectAsync(string projectId)
    {
        Log.Information("ProjectService.SetCurrentProjectAsync called for ID: {Id}", projectId);

        try
        {
            SetPreference(CurrentProjectKey, projectId);
            Log.Information("Current project ID saved to Preferences");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Failed to save current project ID to Preferences. Type: {ExType}, Message: {Message}",
                ex.GetType().Name,
                ex.Message);
            throw;
        }

        await Task.CompletedTask;
    }

    // Use Preferences instead of SecureStorage for development (no provisioning profile needed)
    // SecureStorage requires Keychain entitlements which need a provisioning profile on MacCatalyst
    private static string? GetPreference(string key)
    {
        return Preferences.Default.Get<string?>(key, null);
    }

    private static void SetPreference(string key, string value)
    {
        Preferences.Default.Set(key, value);
    }

    private Task SaveProjectsAsync()
    {
        if (_cachedProjects == null)
        {
            Log.Warning("SaveProjectsAsync called but _cachedProjects is null");
            return Task.CompletedTask;
        }

        var json = JsonSerializer.Serialize(_cachedProjects);
        Log.Information("Saving {Count} projects to Preferences, JSON length: {Length}", _cachedProjects.Count, json.Length);

        try
        {
            SetPreference(ProjectsKey, json);
            Log.Information("Projects saved to Preferences successfully");
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Preferences.Set FAILED for key {Key}. Type: {ExType}, Message: {Message}",
                ProjectsKey,
                ex.GetType().Name,
                ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }
}
