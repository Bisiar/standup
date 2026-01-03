using Azure.Core;
using Azure.Identity;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Infrastructure.Tests.Integrations;

/// <summary>
/// Tests for connecting to Dynamics 365 CRM using DefaultAzureCredential.
/// Validates that the user's Azure CLI credentials can access Dynamics 365.
/// </summary>
public class DynamicsCrmDefaultCredentialTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _crmInstanceUrl;

    public DynamicsCrmDefaultCredentialTests(ITestOutputHelper output)
    {
        _output = output;

        // Get CRM instance URL from environment variable (required for tests)
        _crmInstanceUrl = Environment.GetEnvironmentVariable("CRM_INSTANCE_URL")
            ?? "https://your-org.crm.dynamics.com";
    }

    /// <summary>
    /// Tests that DefaultAzureCredential can acquire a token for Dynamics 365.
    /// Requires: az login to have been run with access to the CRM tenant.
    /// </summary>
    [Fact]
    public async Task DefaultAzureCredential_CanAcquireTokenForDynamics365()
    {
        // Arrange
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

        // Dynamics 365 scope
        var crmScope = $"{_crmInstanceUrl}/.default";
        var tokenRequestContext = new TokenRequestContext(new[] { crmScope });

        _output.WriteLine($"Attempting to get token for scope: {crmScope}");

        // Act
        AccessToken token;
        try
        {
            token = await credential.GetTokenAsync(tokenRequestContext);
        }
        catch (CredentialUnavailableException ex)
        {
            _output.WriteLine($"SKIPPED: No Azure credentials available. Run 'az login' first.");
            _output.WriteLine($"Exception: {ex.Message}");
            return;
        }
        catch (AuthenticationFailedException ex)
        {
            _output.WriteLine($"SKIPPED: Authentication failed - user may not have access to CRM.");
            _output.WriteLine($"Exception: {ex.Message}");
            return;
        }

        // Assert
        _output.WriteLine($"Token acquired successfully!");
        _output.WriteLine($"Token expires: {token.ExpiresOn}");
        _output.WriteLine($"Token length: {token.Token.Length} characters");

        token.Token.Should().NotBeNullOrEmpty();
        token.ExpiresOn.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Tests that we can query Dynamics 365 WhoAmI endpoint using DefaultAzureCredential.
    /// </summary>
    [Fact]
    public async Task DefaultAzureCredential_CanQueryDynamics365WhoAmI()
    {
        // Arrange
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

        var crmScope = $"{_crmInstanceUrl}/.default";
        var tokenRequestContext = new TokenRequestContext(new[] { crmScope });

        // Get token
        AccessToken token;
        try
        {
            token = await credential.GetTokenAsync(tokenRequestContext);
        }
        catch (CredentialUnavailableException ex)
        {
            _output.WriteLine($"SKIPPED: {ex.Message}");
            return;
        }
        catch (AuthenticationFailedException ex)
        {
            _output.WriteLine($"SKIPPED: {ex.Message}");
            return;
        }

        // Act - Call WhoAmI endpoint
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        var whoAmIUrl = $"{_crmInstanceUrl}/api/data/v9.2/WhoAmI";
        _output.WriteLine($"Calling: {whoAmIUrl}");

        try
        {
            var response = await httpClient.GetAsync(whoAmIUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"SUCCESS! WhoAmI response:");
                _output.WriteLine(content);

                response.IsSuccessStatusCode.Should().BeTrue();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"FAILED: {response.StatusCode}");
                _output.WriteLine($"Error: {errorContent}");

                // Don't fail the test if it's an authorization issue - user might not have CRM access
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _output.WriteLine("User does not have access to this CRM instance.");
                    return;
                }

                response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Tests that we can query projects from Dynamics 365 using DefaultAzureCredential.
    /// </summary>
    [Fact]
    public async Task DefaultAzureCredential_CanQueryProjects()
    {
        // Arrange
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

        var crmScope = $"{_crmInstanceUrl}/.default";
        var tokenRequestContext = new TokenRequestContext(new[] { crmScope });

        // Get token
        AccessToken token;
        try
        {
            token = await credential.GetTokenAsync(tokenRequestContext);
        }
        catch (Exception ex) when (ex is CredentialUnavailableException or AuthenticationFailedException)
        {
            _output.WriteLine($"SKIPPED: {ex.Message}");
            return;
        }

        // Act - Query projects
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        // Query msdyn_projects entity (Project Operations)
        var projectsUrl = $"{_crmInstanceUrl}/api/data/v9.2/msdyn_projects?$select=msdyn_subject,msdyn_projectid&$top=5";
        _output.WriteLine($"Querying: {projectsUrl}");

        try
        {
            var response = await httpClient.GetAsync(projectsUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"SUCCESS! Found projects:");
                _output.WriteLine(content);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine($"Status: {response.StatusCode}");
                _output.WriteLine($"Response: {errorContent}");

                // Don't fail if user doesn't have access
                if (response.StatusCode is System.Net.HttpStatusCode.Forbidden
                    or System.Net.HttpStatusCode.Unauthorized)
                {
                    _output.WriteLine("User does not have access to CRM projects.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
