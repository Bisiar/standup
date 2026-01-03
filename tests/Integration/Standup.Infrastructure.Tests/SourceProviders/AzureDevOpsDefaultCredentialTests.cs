using Azure.Core;
using Azure.Identity;
using FluentAssertions;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Xunit;
using Xunit.Abstractions;

namespace Standup.Infrastructure.Tests.SourceProviders;

/// <summary>
/// Tests for connecting to Azure DevOps using DefaultAzureCredential.
/// Validates that the user's Azure CLI credentials can access Azure DevOps.
/// </summary>
public class AzureDevOpsDefaultCredentialTests
{
    /// <summary>
    /// Azure DevOps scope for token acquisition.
    /// </summary>
    private const string AzureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

    private readonly ITestOutputHelper _output;

    public AzureDevOpsDefaultCredentialTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Tests that DefaultAzureCredential can acquire a token for Azure DevOps.
    /// Requires: az login to have been run.
    /// </summary>
    [Fact]
    public async Task DefaultAzureCredential_CanAcquireTokenForAzureDevOps()
    {
        // Arrange
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            // Use AzureCliCredential - the most common local dev scenario
            ExcludeAzureCliCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeInteractiveBrowserCredential = true,
        });

        var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });

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

        // Assert
        _output.WriteLine($"Token acquired successfully!");
        _output.WriteLine($"Token expires: {token.ExpiresOn}");
        _output.WriteLine($"Token length: {token.Token.Length} characters");

        token.Token.Should().NotBeNullOrEmpty();
        token.ExpiresOn.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Tests that we can connect to Azure DevOps using the acquired token.
    /// </summary>
    [Fact]
    public async Task DefaultAzureCredential_CanConnectToAzureDevOps()
    {
        // Arrange - Use JT-Ops organization (from existing tests)
        const string organization = "JT-Ops";
        const string project = "JTP";

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeInteractiveBrowserCredential = true,
        });

        var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });

        // Act - Get token
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

        _output.WriteLine($"Token acquired, connecting to Azure DevOps...");

        // Create VssConnection with OAuth token
        var orgUrl = new Uri($"https://dev.azure.com/{organization}");
        var vssCredentials = new VssOAuthAccessTokenCredential(token.Token);

        using var connection = new VssConnection(orgUrl, vssCredentials);

        // Act - Try to access Azure DevOps
        try
        {
            var gitClient = await connection.GetClientAsync<GitHttpClient>();
            var repositories = await gitClient.GetRepositoriesAsync(project);

            // Assert
            _output.WriteLine($"SUCCESS! Connected to {organization}/{project}");
            _output.WriteLine($"Found {repositories.Count} repositories:");
            foreach (var repo in repositories.Take(5))
            {
                _output.WriteLine($"  - {repo.Name}");
            }

            repositories.Should().NotBeEmpty();
        }
        catch (Exception ex)
        {
            _output.WriteLine($"FAILED to connect: {ex.GetType().Name}");
            _output.WriteLine($"Message: {ex.Message}");

            // Still report the failure for debugging
            throw;
        }
    }

    /// <summary>
    /// Tests that we can fetch commits using DefaultAzureCredential.
    /// </summary>
    [Fact]
    public async Task DefaultAzureCredential_CanFetchCommits()
    {
        // Arrange
        const string organization = "JT-Ops";
        const string project = "JTP";
        const string repository = "jt-crm-time-entry-ai";

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzureCliCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeInteractiveBrowserCredential = true,
        });

        var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });

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

        // Connect
        var orgUrl = new Uri($"https://dev.azure.com/{organization}");
        var vssCredentials = new VssOAuthAccessTokenCredential(token.Token);
        using var connection = new VssConnection(orgUrl, vssCredentials);

        // Act
        var gitClient = await connection.GetClientAsync<GitHttpClient>();
        var repos = await gitClient.GetRepositoriesAsync(project);
        var repo = repos.FirstOrDefault(r => r.Name.Equals(repository, StringComparison.OrdinalIgnoreCase));

        repo.Should().NotBeNull($"Repository '{repository}' should exist");

        var commits = await gitClient.GetCommitsAsync(project, repo!.Id, new GitQueryCommitsCriteria(), top: 5);

        // Assert
        _output.WriteLine($"Found {commits.Count} recent commits:");
        foreach (var commit in commits)
        {
            _output.WriteLine($"  {commit.CommitId[..8]}: {commit.Comment?.Split('\n').FirstOrDefault()}");
        }

        commits.Should().NotBeEmpty();
    }
}
