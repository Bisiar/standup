using System.Text.Json;
using Azure.Core;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Serilog;
using Standup.Domain.Entities;
using Standup.Domain.Interfaces;
using Standup.Infrastructure.Configuration;
using DomainAuthResult = Standup.Domain.Entities.AuthenticationResult;
using MsalAuthResult = Microsoft.Identity.Client.AuthenticationResult;

namespace Standup.Infrastructure.Integrations;

/// <summary>
/// Service for integrating with Office 365 email via Microsoft Graph API.
/// </summary>
public class Office365EmailService : IEmailIntegrationService
{
    private static readonly string[] MessageSelectFields =
    [
        "id", "subject", "bodyPreview", "from", "receivedDateTime",
        "importance", "hasAttachments", "conversationId",
    ];

    private static readonly string[] MessageOrderBy = ["receivedDateTime desc"];

    private readonly Office365EmailOptions _options;
    private readonly IAISummaryService _aiService;
    private IPublicClientApplication? _msalApp;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry;

    /// <summary>
    /// Initializes a new instance of the <see cref="Office365EmailService"/> class.
    /// </summary>
    /// <param name="options">Email integration configuration options.</param>
    /// <param name="aiService">AI service for generating task suggestions.</param>
    public Office365EmailService(
        IOptions<Office365EmailOptions> options,
        IAISummaryService aiService)
    {
        _options = options.Value;
        _aiService = aiService;

        Log.Information(
            "Office365EmailService configured: TenantId={TenantId}, ClientId={ClientId}, Enabled={Enabled}",
            _options.TenantId,
            _options.ClientId,
            _options.Enabled);
    }

    /// <inheritdoc/>
    public string Name => "Office 365 Email";

    /// <inheritdoc/>
    public string Description => "Integrates with Microsoft 365 to fetch client emails and generate task suggestions";

    /// <inheritdoc/>
    public bool IsConfigured => !string.IsNullOrEmpty(_options.TenantId) && !string.IsNullOrEmpty(_options.ClientId);

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get => _options.Enabled;
        set => _options.Enabled = value;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            Log.Warning("Office 365 Email service not configured");
            return false;
        }

        try
        {
            using var graphClient = await CreateGraphClientAsync(cancellationToken);
            var user = await graphClient.Me.GetAsync(cancellationToken: cancellationToken);
            Log.Information("Office 365 Email connection validated for user: {UserEmail}", user?.Mail ?? user?.UserPrincipalName);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Office 365 Email connection validation failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IntegrationData> FetchDataAsync(
        string clientCode,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !IsEnabled)
        {
            Log.Warning("Office 365 Email service not configured or not enabled");
            return new IntegrationData();
        }

        try
        {
            var domains = _options.DomainMappings
                .Where(m => m.ClientCode == clientCode)
                .Select(m => m.Domain)
                .ToList();

            if (domains.Count == 0)
            {
                Log.Debug("No email domains configured for client code: {ClientCode}", clientCode);
                return new IntegrationData();
            }

            var emails = await GetClientEmailsAsync(since, until, domains, cancellationToken);
            return new IntegrationData(Emails: emails);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error fetching email data for client {ClientCode}: {ErrorMessage}",
                clientCode,
                ex.Message);
            return new IntegrationData();
        }
    }

    /// <inheritdoc/>
    public async Task<List<ClientEmail>> GetClientEmailsAsync(
        DateTimeOffset since,
        DateTimeOffset until,
        IEnumerable<string> clientDomains,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !IsEnabled)
        {
            Log.Warning("Office 365 Email service not configured or not enabled");
            return new List<ClientEmail>();
        }

        try
        {
            using var graphClient = await CreateGraphClientAsync(cancellationToken);
            var emails = new List<ClientEmail>();

            foreach (var domain in clientDomains)
            {
                var domainEmails = await FetchEmailsForDomainAsync(
                    graphClient,
                    domain,
                    since,
                    until,
                    cancellationToken);

                emails.AddRange(domainEmails);
            }

            Log.Information(
                "Fetched {EmailCount} emails from Office 365 for domains: {Domains}",
                emails.Count,
                string.Join(", ", clientDomains));

            return emails;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching client emails: {ErrorMessage}", ex.Message);
            return new List<ClientEmail>();
        }
    }

    /// <inheritdoc/>
    public async Task<List<SuggestedTask>> GenerateSuggestedTasksFromEmailsAsync(
        IEnumerable<ClientEmail> emails,
        CancellationToken cancellationToken = default)
    {
        var allTasks = new List<SuggestedTask>();

        foreach (var email in emails)
        {
            try
            {
                var tasks = await GenerateTasksFromEmailAsync(email, cancellationToken);
                allTasks.AddRange(tasks);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Error generating tasks from email {EmailId}: {ErrorMessage}",
                    email.EmailId,
                    ex.Message);
            }
        }

        return allTasks;
    }

    /// <inheritdoc/>
    public Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var isAuthenticated = !string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTimeOffset.UtcNow;
        return Task.FromResult(isAuthenticated);
    }

    /// <inheritdoc/>
    public async Task<DomainAuthResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return DomainAuthResult.Failure("Office 365 Email service not configured");
        }

        try
        {
            var msalApp = GetMsalApp();
            var accounts = await msalApp.GetAccountsAsync();
            MsalAuthResult result;

            try
            {
                // Try silent authentication first
                result = await msalApp.AcquireTokenSilent(_options.Scopes, accounts.FirstOrDefault())
                    .ExecuteAsync(cancellationToken);
            }
            catch (MsalUiRequiredException)
            {
                // Fall back to interactive authentication
                // Redirect URI already set on app builder
                result = await msalApp.AcquireTokenInteractive(_options.Scopes)
                    .ExecuteAsync(cancellationToken);
            }

            _accessToken = result.AccessToken;
            _tokenExpiry = result.ExpiresOn;

            Log.Information(
                "Office 365 authentication successful for user: {UserEmail}",
                result.Account?.Username);

            return DomainAuthResult.Success(
                result.AccessToken,
                result.Account?.HomeAccountId?.Identifier,
                result.ExpiresOn,
                result.Account?.Username,
                result.Account?.Username);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Office 365 authentication failed: {ErrorMessage}", ex.Message);
            return DomainAuthResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task RevokeAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var msalApp = GetMsalApp();
            var accounts = await msalApp.GetAccountsAsync();

            foreach (var account in accounts)
            {
                await msalApp.RemoveAsync(account);
            }

            _accessToken = null;
            _tokenExpiry = DateTimeOffset.MinValue;

            Log.Information("Office 365 authentication revoked");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error revoking Office 365 authentication: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private IPublicClientApplication GetMsalApp()
    {
        if (_msalApp != null)
        {
            return _msalApp;
        }

        _msalApp = PublicClientApplicationBuilder
            .Create(_options.ClientId)
            .WithAuthority($"https://login.microsoftonline.com/{_options.TenantId}")
            .WithRedirectUri(_options.RedirectUri)
            .Build();

        return _msalApp;
    }

    private async Task<GraphServiceClient> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_accessToken) || _tokenExpiry <= DateTimeOffset.UtcNow)
        {
            var authResult = await AuthenticateAsync(cancellationToken);
            if (!authResult.IsSuccess)
            {
                throw new InvalidOperationException($"Authentication failed: {authResult.ErrorMessage}");
            }
        }

        var tokenCredential = new TokenCredentialAdapter(_accessToken!);
        return new GraphServiceClient(tokenCredential);
    }

    private async Task<List<ClientEmail>> FetchEmailsForDomainAsync(
        GraphServiceClient graphClient,
        string domain,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken)
    {
        var emails = new List<ClientEmail>();

        try
        {
            var clientCode = _options.DomainMappings
                .FirstOrDefault(m => m.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))
                ?.ClientCode ?? "UNKNOWN";

            var filter = $"receivedDateTime ge {since:yyyy-MM-ddTHH:mm:ssZ} and receivedDateTime le {until:yyyy-MM-ddTHH:mm:ssZ}";

            var messages = await graphClient.Me.Messages.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = filter;
                    requestConfiguration.QueryParameters.Select = MessageSelectFields;
                    requestConfiguration.QueryParameters.Orderby = MessageOrderBy;
                    requestConfiguration.QueryParameters.Top = 50;
                },
                cancellationToken);

            if (messages?.Value == null)
            {
                return emails;
            }

            foreach (var message in messages.Value)
            {
                if (message.From?.EmailAddress?.Address == null)
                {
                    continue;
                }

                var senderDomain = message.From.EmailAddress.Address.Split('@').LastOrDefault() ?? string.Empty;
                if (!senderDomain.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var email = new ClientEmail(
                    EmailId: message.Id ?? Guid.NewGuid().ToString(),
                    ClientCode: clientCode,
                    Subject: message.Subject ?? "(No Subject)",
                    Preview: message.BodyPreview ?? string.Empty,
                    SenderName: message.From.EmailAddress.Name ?? "Unknown",
                    SenderEmail: message.From.EmailAddress.Address,
                    ReceivedAt: message.ReceivedDateTime ?? DateTimeOffset.UtcNow,
                    IsImportant: message.Importance == Importance.High,
                    HasAttachments: message.HasAttachments ?? false,
                    ConversationId: message.ConversationId);

                emails.Add(email);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching emails for domain {Domain}: {ErrorMessage}", domain, ex.Message);
        }

        return emails;
    }

    private async Task<List<SuggestedTask>> GenerateTasksFromEmailAsync(
        ClientEmail email,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $$$"""
                Analyze this client email and suggest 1-3 specific, actionable tasks:

                From: {{{email.SenderName}}} ({{{email.SenderEmail}}})
                Subject: {{{email.Subject}}}
                Content: {{{email.Preview}}}

                Generate tasks as a JSON array with the following format:
                [
                  {
                    "title": "Brief task title",
                    "description": "Detailed task description",
                    "priority": "High|Medium|Low"
                  }
                ]

                Only suggest tasks if the email contains clear action items or requests.
                Return an empty array [] if no actionable tasks are identified.
                """;

            var standupData = new StandupData(); // Empty data, just for AI call structure
            var summaryOptions = new SummaryOptions
            {
                CustomPrompt = prompt,
                MaxLength = 500
            };

            var aiResponse = await _aiService.GenerateSummaryAsync(standupData, summaryOptions, cancellationToken);

            // Parse JSON response
            var tasks = new List<SuggestedTask>();
            try
            {
                var jsonStart = aiResponse.IndexOf('[');
                var jsonEnd = aiResponse.LastIndexOf(']');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var taskDtos = JsonSerializer.Deserialize<List<TaskDto>>(json);

                    if (taskDtos != null)
                    {
                        tasks.AddRange(taskDtos.Select(dto => new SuggestedTask(
                            dto.Title,
                            dto.Description,
                            dto.Priority,
                            email.EmailId)));
                    }
                }
            }
            catch (JsonException ex)
            {
                Log.Warning(ex, "Failed to parse AI task suggestions for email {EmailId}", email.EmailId);
            }

            return tasks;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating tasks from email {EmailId}: {ErrorMessage}", email.EmailId, ex.Message);
            return new List<SuggestedTask>();
        }
    }

    private record TaskDto(string Title, string Description, string Priority);

    /// <summary>
    /// Adapter to use a pre-acquired access token with Azure.Core TokenCredential.
    /// </summary>
    private sealed class TokenCredentialAdapter : TokenCredential
    {
        private readonly string _accessToken;

        public TokenCredentialAdapter(string accessToken)
        {
            _accessToken = accessToken;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1)));
        }
    }
}
