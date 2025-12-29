using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using Standup.Domain.Enums;

namespace Standup.Infrastructure.Git;

/// <summary>
/// Parses .git/config files to extract remote repository information.
/// Supports GitHub and Azure DevOps URL formats.
/// </summary>
public class GitConfigParser
{
    private readonly ILogger<GitConfigParser>? _logger;

    public GitConfigParser(ILogger<GitConfigParser>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Result of parsing a git config file.
    /// </summary>
    public record GitConfigInfo(
        SourceType SourceType,
        string Organization,
        string? Project,
        string Repository,
        string RemoteUrl,
        string? ApiEndpoint = null);

    // Azure DevOps patterns
    private static readonly Regex AzureDevOpsHttpsPattern = new(
        @"https://dev\.azure\.com/(?<org>[^/]+)/(?<project>[^/]+)/_git/(?<repo>[^/\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AzureDevOpsSshPattern = new(
        @"git@ssh\.dev\.azure\.com:v3/(?<org>[^/]+)/(?<project>[^/]+)/(?<repo>[^/\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex VisualStudioHttpsPattern = new(
        @"https://(?<org>[^.]+)\.visualstudio\.com/(?<project>[^/]+)/_git/(?<repo>[^/\s]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // GitHub patterns (supports optional embedded credentials like token@github.com)
    private static readonly Regex GitHubHttpsPattern = new(
        @"https://(?:[^@]+@)?github\.com/(?<org>[^/]+)/(?<repo>[^/\s\.]+)(?:\.git)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GitHubSshPattern = new(
        @"git@github\.com:(?<org>[^/]+)/(?<repo>[^/\s\.]+)(?:\.git)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // GitHub Enterprise patterns - matches any host with github-like URL structure
    private static readonly Regex GitHubEnterpriseHttpsPattern = new(
        @"https://(?:[^@]+@)?(?<host>[^/]+)/(?<org>[^/]+)/(?<repo>[^/\s\.]+)(?:\.git)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GitHubEnterpriseSshPattern = new(
        @"git@(?<host>[^:]+):(?<org>[^/]+)/(?<repo>[^/\s\.]+)(?:\.git)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses a local git repository directory to extract remote information.
    /// </summary>
    /// <param name="localPath">Path to the repository root (containing .git folder).</param>
    /// <returns>Parsed configuration info, or null if parsing fails.</returns>
    public GitConfigInfo? ParseRepository(string localPath)
    {
        var gitConfigPath = Path.Combine(localPath, ".git", "config");

        if (!File.Exists(gitConfigPath))
        {
            _logger?.LogWarning("Git config not found at: {Path}", gitConfigPath);
            return null;
        }

        try
        {
            var configContent = File.ReadAllText(gitConfigPath);
            return ParseConfigContent(configContent);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse git config at: {Path}", gitConfigPath);
            return null;
        }
    }

    /// <summary>
    /// Parses git config content to extract remote URL.
    /// </summary>
    /// <param name="configContent">The content of the git config file.</param>
    /// <returns>Parsed configuration info, or null if parsing fails.</returns>
    public GitConfigInfo? ParseConfigContent(string configContent)
    {
        // Look for [remote "origin"] section and extract url
        var remoteOriginPattern = new Regex(
            @"\[remote\s+""origin""\].*?url\s*=\s*(?<url>[^\r\n]+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var match = remoteOriginPattern.Match(configContent);
        if (!match.Success)
        {
            _logger?.LogWarning("No origin remote found in git config");
            return null;
        }

        var remoteUrl = match.Groups["url"].Value.Trim();
        return ParseRemoteUrl(remoteUrl);
    }

    /// <summary>
    /// Parses a remote URL to extract repository information.
    /// </summary>
    /// <param name="remoteUrl">The remote URL to parse.</param>
    /// <returns>Parsed configuration info, or null if parsing fails.</returns>
    public GitConfigInfo? ParseRemoteUrl(string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return null;
        }

        // Try Azure DevOps patterns
        var adoHttps = AzureDevOpsHttpsPattern.Match(remoteUrl);
        if (adoHttps.Success)
        {
            return new GitConfigInfo(
                SourceType.AzureDevOps,
                adoHttps.Groups["org"].Value,
                adoHttps.Groups["project"].Value,
                adoHttps.Groups["repo"].Value,
                remoteUrl);
        }

        var adoSsh = AzureDevOpsSshPattern.Match(remoteUrl);
        if (adoSsh.Success)
        {
            return new GitConfigInfo(
                SourceType.AzureDevOps,
                adoSsh.Groups["org"].Value,
                adoSsh.Groups["project"].Value,
                adoSsh.Groups["repo"].Value,
                remoteUrl);
        }

        var vsOnline = VisualStudioHttpsPattern.Match(remoteUrl);
        if (vsOnline.Success)
        {
            return new GitConfigInfo(
                SourceType.AzureDevOps,
                vsOnline.Groups["org"].Value,
                vsOnline.Groups["project"].Value,
                vsOnline.Groups["repo"].Value,
                remoteUrl);
        }

        // Try GitHub.com patterns first
        var ghHttps = GitHubHttpsPattern.Match(remoteUrl);
        if (ghHttps.Success)
        {
            return new GitConfigInfo(
                SourceType.GitHub,
                ghHttps.Groups["org"].Value,
                null,
                ghHttps.Groups["repo"].Value,
                remoteUrl,
                ApiEndpoint: null); // null = use github.com
        }

        var ghSsh = GitHubSshPattern.Match(remoteUrl);
        if (ghSsh.Success)
        {
            return new GitConfigInfo(
                SourceType.GitHub,
                ghSsh.Groups["org"].Value,
                null,
                ghSsh.Groups["repo"].Value,
                remoteUrl,
                ApiEndpoint: null);
        }

        // Try GitHub Enterprise patterns (any other git hosting that looks like GitHub)
        var gheHttps = GitHubEnterpriseHttpsPattern.Match(remoteUrl);
        if (gheHttps.Success)
        {
            var host = gheHttps.Groups["host"].Value;

            // Skip known non-GitHub hosts (unsupported platforms)
            if (!host.Contains("azure.com", StringComparison.OrdinalIgnoreCase) &&
                !host.Contains("visualstudio.com", StringComparison.OrdinalIgnoreCase) &&
                !host.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase) &&
                !host.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase))
            {
                return new GitConfigInfo(
                    SourceType.GitHub,
                    gheHttps.Groups["org"].Value,
                    null,
                    gheHttps.Groups["repo"].Value,
                    remoteUrl,
                    ApiEndpoint: $"https://{host}");
            }
        }

        var gheSsh = GitHubEnterpriseSshPattern.Match(remoteUrl);
        if (gheSsh.Success)
        {
            var host = gheSsh.Groups["host"].Value;

            // Skip known non-GitHub hosts (unsupported platforms)
            if (!host.Contains("azure.com", StringComparison.OrdinalIgnoreCase) &&
                !host.Contains("visualstudio.com", StringComparison.OrdinalIgnoreCase) &&
                !host.Contains("bitbucket.org", StringComparison.OrdinalIgnoreCase) &&
                !host.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase))
            {
                return new GitConfigInfo(
                    SourceType.GitHub,
                    gheSsh.Groups["org"].Value,
                    null,
                    gheSsh.Groups["repo"].Value,
                    remoteUrl,
                    ApiEndpoint: $"https://{host}");
            }
        }

        _logger?.LogWarning("Could not parse remote URL: {Url}", remoteUrl);
        return null;
    }

    /// <summary>
    /// Checks if a directory is a valid git repository.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if the directory contains a .git folder, false otherwise.</returns>
    public static bool IsGitRepository(string path)
    {
        var gitDir = Path.Combine(path, ".git");
        return Directory.Exists(gitDir);
    }
}
