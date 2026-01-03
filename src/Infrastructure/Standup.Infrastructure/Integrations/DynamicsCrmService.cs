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

    /// <inheritdoc/>
    public async Task<List<CrmAttributeMetadata>> GetEntityMetadataAsync(
        string entityLogicalName = "msdyn_project",
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            Log.Warning("CRM service not configured, cannot retrieve metadata");
            return new List<CrmAttributeMetadata>();
        }

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Query entity metadata using the Dataverse Web API
            var apiUrl = $"{_options.InstanceUrl}/api/data/v9.2/EntityDefinitions(LogicalName='{entityLogicalName}')/Attributes?$select=LogicalName,DisplayName,AttributeType,AttributeTypeName,SchemaName,Description,IsCustomAttribute";

            Log.Information("Fetching entity metadata for: {EntityName}", entityLogicalName);

            var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Log.Warning(
                    "CRM metadata API request failed: {StatusCode} - {ReasonPhrase}. Response: {Response}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    errorContent);
                return new List<CrmAttributeMetadata>();
            }

            var result = await response.Content.ReadFromJsonAsync<MetadataQueryResult>(cancellationToken);

            if (result?.Value == null || result.Value.Count == 0)
            {
                Log.Warning("No attributes found for entity: {EntityName}", entityLogicalName);
                return new List<CrmAttributeMetadata>();
            }

            var attributes = result.Value
                .Select(MapToAttributeMetadata)
                .OrderBy(a => a.LogicalName)
                .ToList();

            // Log all discovered fields for analysis
            Log.Information("========================================");
            Log.Information("ENTITY METADATA FOR: {EntityName}", entityLogicalName);
            Log.Information("Total attributes found: {Count}", attributes.Count);
            Log.Information("========================================");

            // Log custom fields (likely what we're looking for)
            var customFields = attributes.Where(a => a.IsCustomAttribute).ToList();
            Log.Information("--- CUSTOM FIELDS ({Count}) ---", customFields.Count);
            foreach (var attr in customFields)
            {
                Log.Information(
                    "  {LogicalName} | {DisplayName} | Type: {Type} ({TypeName})",
                    attr.LogicalName,
                    attr.DisplayName,
                    attr.AttributeType,
                    attr.AttributeTypeName);
            }

            // Log fields that might match what we're looking for
            Log.Information("--- FIELDS MATCHING SEARCH CRITERIA ---");

            // Practice fields
            var practiceFields = attributes.Where(a =>
                a.LogicalName.Contains("practice", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("practice", StringComparison.OrdinalIgnoreCase)).ToList();
            if (practiceFields.Count > 0)
            {
                Log.Information("Practice-related fields:");
                foreach (var attr in practiceFields)
                {
                    Log.Information("  {LogicalName} | {DisplayName} | {Type}", attr.LogicalName, attr.DisplayName, attr.AttributeType);
                }
            }

            // Director/Manager fields (lookups to systemuser)
            var personFields = attributes.Where(a =>
                a.LogicalName.Contains("director", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("manager", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("director", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("manager", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("account", StringComparison.OrdinalIgnoreCase)).ToList();
            if (personFields.Count > 0)
            {
                Log.Information("Director/Manager/Account fields:");
                foreach (var attr in personFields)
                {
                    Log.Information("  {LogicalName} | {DisplayName} | {Type}", attr.LogicalName, attr.DisplayName, attr.AttributeType);
                }
            }

            // SOW/Document URL fields
            var sowFields = attributes.Where(a =>
                a.LogicalName.Contains("sow", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("document", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("url", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("sow", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("statement", StringComparison.OrdinalIgnoreCase)).ToList();
            if (sowFields.Count > 0)
            {
                Log.Information("SOW/Document URL fields:");
                foreach (var attr in sowFields)
                {
                    Log.Information("  {LogicalName} | {DisplayName} | {Type}", attr.LogicalName, attr.DisplayName, attr.AttributeType);
                }
            }

            // Status fields (optionsets for green/yellow/red)
            var statusFields = attributes.Where(a =>
                a.AttributeType == "Picklist" || a.AttributeType == "Status" || a.AttributeType == "State" ||
                a.LogicalName.Contains("status", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("health", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("color", StringComparison.OrdinalIgnoreCase)).ToList();
            if (statusFields.Count > 0)
            {
                Log.Information("Status/Health fields (optionsets):");
                foreach (var attr in statusFields)
                {
                    Log.Information("  {LogicalName} | {DisplayName} | {Type}", attr.LogicalName, attr.DisplayName, attr.AttributeType);
                }
            }

            // Sprint/Commitment fields
            var sprintFields = attributes.Where(a =>
                a.LogicalName.Contains("sprint", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("commitment", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("sprint", StringComparison.OrdinalIgnoreCase) ||
                a.DisplayName.Contains("commitment", StringComparison.OrdinalIgnoreCase)).ToList();
            if (sprintFields.Count > 0)
            {
                Log.Information("Sprint/Commitment fields:");
                foreach (var attr in sprintFields)
                {
                    Log.Information("  {LogicalName} | {DisplayName} | {Type}", attr.LogicalName, attr.DisplayName, attr.AttributeType);
                }
            }

            // Cost/Labor/Budget fields
            var costFields = attributes.Where(a =>
                a.AttributeType == "Money" || a.AttributeType == "Decimal" || a.AttributeType == "Double" ||
                a.LogicalName.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("labor", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("budget", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("estimated", StringComparison.OrdinalIgnoreCase) ||
                a.LogicalName.Contains("percent", StringComparison.OrdinalIgnoreCase)).ToList();
            if (costFields.Count > 0)
            {
                Log.Information("Cost/Labor/Budget fields:");
                foreach (var attr in costFields)
                {
                    Log.Information("  {LogicalName} | {DisplayName} | {Type}", attr.LogicalName, attr.DisplayName, attr.AttributeType);
                }
            }

            Log.Information("========================================");
            Log.Information("END ENTITY METADATA");
            Log.Information("========================================");

            return attributes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching entity metadata for {EntityName}: {ErrorMessage}", entityLogicalName, ex.Message);
            return new List<CrmAttributeMetadata>();
        }
    }

    private static CrmAttributeMetadata MapToAttributeMetadata(JsonElement data)
    {
        var logicalName = GetStringProperty(data, "LogicalName");
        var attributeType = GetStringProperty(data, "AttributeType");
        var attributeTypeName = string.Empty;
        var displayName = string.Empty;
        var description = string.Empty;
        var schemaName = GetStringProperty(data, "SchemaName");
        var isCustom = false;

        // AttributeTypeName is nested object with Value property
        if (data.TryGetProperty("AttributeTypeName", out var typeNameElement) &&
            typeNameElement.TryGetProperty("Value", out var typeNameValue))
        {
            attributeTypeName = typeNameValue.GetString() ?? string.Empty;
        }

        // DisplayName is a LocalizedLabel structure
        if (data.TryGetProperty("DisplayName", out var displayNameElement) &&
            displayNameElement.TryGetProperty("UserLocalizedLabel", out var userLabel) &&
            userLabel.ValueKind != JsonValueKind.Null &&
            userLabel.TryGetProperty("Label", out var labelValue))
        {
            displayName = labelValue.GetString() ?? string.Empty;
        }

        // Description is also a LocalizedLabel structure
        if (data.TryGetProperty("Description", out var descElement) &&
            descElement.TryGetProperty("UserLocalizedLabel", out var descUserLabel) &&
            descUserLabel.ValueKind != JsonValueKind.Null &&
            descUserLabel.TryGetProperty("Label", out var descLabelValue))
        {
            description = descLabelValue.GetString() ?? string.Empty;
        }

        // IsCustomAttribute
        if (data.TryGetProperty("IsCustomAttribute", out var isCustomElement) &&
            isCustomElement.ValueKind == JsonValueKind.True)
        {
            isCustom = true;
        }

        return new CrmAttributeMetadata(
            LogicalName: logicalName,
            DisplayName: displayName,
            AttributeType: attributeType,
            AttributeTypeName: attributeTypeName)
        {
            Description = string.IsNullOrEmpty(description) ? null : description,
            SchemaName = string.IsNullOrEmpty(schemaName) ? null : schemaName,
            IsCustomAttribute = isCustom
        };
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

    private class MetadataQueryResult
    {
        public List<JsonElement> Value { get; set; } = new();
    }
}
