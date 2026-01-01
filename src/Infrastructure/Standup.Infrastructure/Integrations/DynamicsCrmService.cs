using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Serilog;
using Standup.Domain.Entities;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Configuration;

namespace Standup.Infrastructure.Integrations;

/// <summary>
/// Service for interacting with Dynamics 365 CRM using the Dataverse Web API.
/// </summary>
public class DynamicsCrmService : ICrmProjectService
{
    private readonly DynamicsCrmOptions _options;
    private readonly HttpClient _httpClient;
    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiry;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicsCrmService"/> class.
    /// </summary>
    /// <param name="options">CRM configuration options.</param>
    /// <param name="httpClient">HTTP client for API requests.</param>
    public DynamicsCrmService(IOptions<DynamicsCrmOptions> options, HttpClient httpClient)
    {
        _options = options.Value;
        _httpClient = httpClient;

        Log.Information(
            "DynamicsCrmService configured: InstanceUrl={InstanceUrl}, TenantId={TenantId}",
            _options.InstanceUrl,
            _options.TenantId);
    }

    /// <inheritdoc/>
    public async Task<List<CrmProject>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            Log.Warning("CRM service not configured, returning empty project list");
            return new List<CrmProject>();
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Query all active projects from Dynamics 365 Project Operations
            var query = "msdyn_projects?$filter=statecode eq 0&$select=msdyn_projectid,msdyn_subject,msdyn_description,statecode,msdyn_scheduledstart,msdyn_scheduledend,msdyn_progress&$orderby=msdyn_subject";
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/{query}";

            Log.Debug("Fetching all CRM projects");

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "CRM API request failed: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                return new List<CrmProject>();
            }

            var result = await response.Content.ReadFromJsonAsync<CrmQueryResult>(cancellationToken);

            if (result?.Value == null || result.Value.Count == 0)
            {
                Log.Debug("No CRM projects found");
                return new List<CrmProject>();
            }

            var projects = result.Value
                .Select(data => MapToCrmProject(data, string.Empty))
                .ToList();

            Log.Information("Retrieved {Count} CRM projects", projects.Count);

            return projects;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching all CRM projects: {ErrorMessage}", ex.Message);
            return new List<CrmProject>();
        }
    }

    /// <inheritdoc/>
    public async Task<CrmProject?> GetProjectByIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            Log.Warning("CRM service not configured, skipping project lookup for ID {ProjectId}", projectId);
            return null;
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Fetch single project by ID
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/msdyn_projects({projectId})?$select=msdyn_projectid,msdyn_subject,msdyn_description,statecode,msdyn_scheduledstart,msdyn_scheduledend,msdyn_progress";

            Log.Debug("Fetching CRM project by ID: {ProjectId}", projectId);

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "CRM API request failed: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                return null;
            }

            var projectData = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var project = MapToCrmProject(projectData, string.Empty);

            Log.Information("Retrieved CRM project: {ProjectName}", project.ProjectName);

            return project;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching CRM project by ID {ProjectId}: {ErrorMessage}", projectId, ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<CrmProject?> GetProjectByClientCodeAsync(string clientCode, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            Log.Warning("CRM service not configured, skipping project lookup for {ClientCode}", clientCode);
            return null;
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Query the CRM for projects using Dynamics 365 Project Operations msdyn_project entity
            // Using standard D365 Project Operations field names
            // Escape the client code to prevent OData injection attacks
            var escapedClientCode = Uri.EscapeDataString(clientCode).Replace("'", "''");
            var query = $"msdyn_projects?$filter=msdyn_subject eq '{escapedClientCode}'&$top=1&$select=msdyn_projectid,msdyn_subject,msdyn_description,statecode,msdyn_scheduledstart,msdyn_scheduledend,msdyn_progress,msdyn_projectmanager";
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/{query}";

            Log.Debug("Fetching CRM project for client code: {ClientCode}", clientCode);

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "CRM API request failed: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<CrmQueryResult>(cancellationToken);

            if (result?.Value == null || result.Value.Count == 0)
            {
                Log.Debug("No CRM project found for client code: {ClientCode}", clientCode);
                return null;
            }

            var projectData = result.Value[0];
            var project = MapToCrmProject(projectData, clientCode);

            Log.Information(
                "Retrieved CRM project: {ProjectName} ({ClientCode})",
                project.ProjectName,
                clientCode);

            return project;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error fetching CRM project for client code {ClientCode}: {ErrorMessage}",
                clientCode,
                ex.Message);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<CrmMilestone>> GetUpcomingMilestonesAsync(string projectId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return new List<CrmMilestone>();
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Query for project tasks (milestones) using Dynamics 365 Project Operations msdyn_projecttask entity
            // Using standard D365 Project Operations field names including effort tracking
            // Escape the project ID to prevent OData injection attacks
            var escapedProjectId = Uri.EscapeDataString(projectId).Replace("'", "''");
            var query = $"msdyn_projecttasks?$filter=_msdyn_project_value eq '{escapedProjectId}'&$orderby=msdyn_scheduledend asc&$select=msdyn_projecttaskid,msdyn_subject,msdyn_description,msdyn_scheduledend,msdyn_progress,msdyn_effort,msdyn_effortcompleted,msdyn_effortremaining,statecode";
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/{query}";

            Log.Debug("Fetching CRM milestones for project: {ProjectId}", projectId);

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "CRM API request for milestones failed: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                return new List<CrmMilestone>();
            }

            var result = await response.Content.ReadFromJsonAsync<CrmQueryResult>(cancellationToken);

            if (result?.Value == null || result.Value.Count == 0)
            {
                return new List<CrmMilestone>();
            }

            var milestones = result.Value
                .Select(m => MapToCrmMilestone(m, projectId))
                .Where(m => m != null)
                .Cast<CrmMilestone>()
                .ToList();

            Log.Information("Retrieved {Count} milestones for project {ProjectId}", milestones.Count, projectId);

            return milestones;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error fetching CRM milestones for project {ProjectId}: {ErrorMessage}",
                projectId,
                ex.Message);
            return new List<CrmMilestone>();
        }
    }

    /// <inheritdoc/>
    public async Task<List<CrmTask>> GetInProgressTasksAsync(string projectId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return new List<CrmTask>();
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Query for active project tasks (statecode=0 means Active)
            // Note: Not filtering by progress - show all active tasks regardless of completion %
            var escapedProjectId = Uri.EscapeDataString(projectId).Replace("'", "''");
            var query = $"msdyn_projecttasks?$filter=_msdyn_project_value eq '{escapedProjectId}' and statecode eq 0&$orderby=msdyn_scheduledend asc&$select=msdyn_projecttaskid,msdyn_subject,msdyn_description,msdyn_scheduledend,msdyn_progress,statecode&$top=10";
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/{query}";

            Log.Debug("Fetching CRM in-progress tasks for project: {ProjectId}", projectId);

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning(
                    "CRM API request for tasks failed: {StatusCode} - {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                return new List<CrmTask>();
            }

            var result = await response.Content.ReadFromJsonAsync<CrmQueryResult>(cancellationToken);

            if (result?.Value == null || result.Value.Count == 0)
            {
                return new List<CrmTask>();
            }

            var tasks = result.Value
                .Select(MapToCrmTask)
                .Where(t => t != null)
                .Cast<CrmTask>()
                .ToList();

            Log.Information("Retrieved {Count} in-progress tasks for project {ProjectId}", tasks.Count, projectId);

            return tasks;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error fetching CRM tasks for project {ProjectId}: {ErrorMessage}",
                projectId,
                ex.Message);
            return new List<CrmTask>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            Log.Warning("CRM service not configured");
            return false;
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Simple query to validate connection
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/WhoAmI";

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Log.Information("CRM connection validated successfully");
                return true;
            }

            Log.Warning(
                "CRM connection validation failed: {StatusCode} - {ReasonPhrase}",
                response.StatusCode,
                response.ReasonPhrase);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CRM connection validation error: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    private static CrmProject MapToCrmProject(JsonElement data, string clientCode)
    {
        // Map D365 Project Operations statecode to readable status
        var stateCode = GetIntProperty(data, "statecode");
        var status = stateCode switch
        {
            0 => "Active",
            1 => "Inactive",
            _ => "Unknown"
        };

        return new CrmProject(
            CrmProjectId: GetStringProperty(data, "msdyn_projectid"),
            ClientCode: clientCode,
            ProjectName: GetStringProperty(data, "msdyn_subject"),
            ClientName: GetStringProperty(data, "msdyn_description"),
            Status: status)
        {
            StartDate = GetDateTimeProperty(data, "msdyn_scheduledstart"),
            EndDate = GetDateTimeProperty(data, "msdyn_scheduledend"),
            CurrentPhase = string.Empty, // D365 Project Operations doesn't have phase field by default
            ProjectManager = GetStringProperty(data, "msdyn_projectmanager"),
            PercentComplete = GetDecimalProperty(data, "msdyn_progress"),
            BudgetHours = null, // Would need to query related resource assignments
            HoursUsed = null // Would need to query related time entries
        };
    }

    private static CrmMilestone? MapToCrmMilestone(JsonElement data, string projectId)
    {
        var dueDateValue = GetDateTimeProperty(data, "msdyn_scheduledend");
        if (!dueDateValue.HasValue)
        {
            return null;
        }

        // Map D365 Project Operations statecode to readable status
        var stateCode = GetIntProperty(data, "statecode");
        var status = stateCode switch
        {
            0 => "Active",
            1 => "Inactive",
            _ => "Unknown"
        };

        return new CrmMilestone(
            MilestoneId: GetStringProperty(data, "msdyn_projecttaskid"),
            ProjectId: projectId,
            Name: GetStringProperty(data, "msdyn_subject"),
            DueDate: dueDateValue.Value)
        {
            Status = status,
            Description = GetStringProperty(data, "msdyn_description"),
            PercentComplete = GetDecimalProperty(data, "msdyn_progress"),
            EffortEstimated = GetDecimalProperty(data, "msdyn_effort"),
            EffortCompleted = GetDecimalProperty(data, "msdyn_effortcompleted"),
            EffortRemaining = GetDecimalProperty(data, "msdyn_effortremaining")
        };
    }

    private static CrmTask? MapToCrmTask(JsonElement data)
    {
        var taskId = GetStringProperty(data, "msdyn_projecttaskid");
        if (string.IsNullOrEmpty(taskId))
        {
            return null;
        }

        var stateCode = GetIntProperty(data, "statecode");
        var isActive = stateCode == 0;

        return new CrmTask(
            TaskId: taskId,
            Name: GetStringProperty(data, "msdyn_subject"),
            Description: GetStringProperty(data, "msdyn_description"),
            ScheduledEnd: GetDateTimeProperty(data, "msdyn_scheduledend"),
            Progress: GetDecimalProperty(data, "msdyn_progress") ?? 0,
            AssignedTo: null,
            IsActive: isActive);
    }

    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static DateTime? GetDateTimeProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(property.GetString(), out var dateValue))
            {
                return dateValue;
            }
        }

        return null;
    }

    private static decimal? GetDecimalProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }
        }

        return null;
    }

    private static int GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
            {
                return intValue;
            }
        }

        return -1;
    }

    private bool IsConfigured()
    {
        // Only need InstanceUrl to be configured - we can use DefaultAzureCredential
        return !string.IsNullOrEmpty(_options.InstanceUrl);
    }

    private bool HasClientCredentials()
    {
        return !string.IsNullOrEmpty(_options.TenantId)
            && !string.IsNullOrEmpty(_options.ClientId)
            && !string.IsNullOrEmpty(_options.ClientSecret);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        // Check if we have a valid token
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return;
        }

        var tokenRequestContext = new TokenRequestContext(new[] { $"{_options.InstanceUrl}/.default" });

        // Priority 1: Use client credentials if configured
        if (HasClientCredentials())
        {
            Log.Debug("CRM: Using client credentials for {InstanceUrl}", _options.InstanceUrl);
            var credential = new ClientSecretCredential(
                _options.TenantId,
                _options.ClientId,
                _options.ClientSecret);

            var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            _cachedAccessToken = token.Token;
            _tokenExpiry = token.ExpiresOn;
        }
        else
        {
            // Priority 2: Use DefaultAzureCredential (Azure CLI, Managed Identity, etc.)
            Log.Debug("CRM: Using DefaultAzureCredential for {InstanceUrl}", _options.InstanceUrl);
            try
            {
                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeEnvironmentCredential = true,
                    ExcludeManagedIdentityCredential = false,
                    ExcludeWorkloadIdentityCredential = true,
                    ExcludeVisualStudioCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeAzureCliCredential = false,
                    ExcludeAzureDeveloperCliCredential = false,
                    ExcludeInteractiveBrowserCredential = true,
                });

                var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
                _cachedAccessToken = token.Token;
                _tokenExpiry = token.ExpiresOn;

                Log.Information("CRM: Connected using DefaultAzureCredential to {InstanceUrl}", _options.InstanceUrl);
            }
            catch (CredentialUnavailableException ex)
            {
                Log.Warning("CRM: DefaultAzureCredential unavailable: {Message}", ex.Message);
                throw new InvalidOperationException(
                    $"No credentials available for CRM. Either configure ClientSecret or run 'az login'. Error: {ex.Message}",
                    ex);
            }
        }

        // Set the authorization header
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedAccessToken);

        Log.Debug("Obtained new CRM access token, expires at {Expiry}", _tokenExpiry);
    }

    private class CrmQueryResult
    {
        public List<JsonElement> Value { get; set; } = new();
    }
}
