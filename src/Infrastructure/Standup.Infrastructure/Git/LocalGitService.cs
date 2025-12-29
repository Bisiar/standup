using System.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Standup.Infrastructure.Git;

/// <summary>
/// Service for reading git information directly from local repositories.
/// No PAT required - uses local git commands.
/// </summary>
public class LocalGitService
{
    private readonly ILogger<LocalGitService>? _logger;

    public LocalGitService(ILogger<LocalGitService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Represents a commit from local git log.
    /// </summary>
    public record LocalCommit(
        string Sha,
        string ShortSha,
        string AuthorName,
        string AuthorEmail,
        DateTimeOffset CommitDate,
        string Subject,
        string Body,
        int Additions = 0,
        int Deletions = 0);

    /// <summary>
    /// Gets commits from local git repository for a specific author within a date range.
    /// </summary>
    /// <param name="localPath">Path to the git repository.</param>
    /// <param name="authorIdentifier">Author name or email to filter by (optional).</param>
    /// <param name="since">Start date for commits.</param>
    /// <param name="until">End date for commits.</param>
    /// <param name="maxCount">Maximum number of commits to return.</param>
    /// <returns>List of commits matching the criteria.</returns>
    public async Task<List<LocalCommit>> GetCommitsAsync(
        string localPath,
        string? authorIdentifier = null,
        DateTimeOffset? since = null,
        DateTimeOffset? until = null,
        int maxCount = 100)
    {
        var commits = new List<LocalCommit>();

        if (!Directory.Exists(localPath))
        {
            _logger?.LogWarning("Local path does not exist: {Path}", localPath);
            return commits;
        }

        try
        {
            // Build git log command with custom format for easy parsing
            // Use COMMIT_START marker at beginning so shortstat stays with its commit
            // Format: COMMIT_START%x01SHA%x00ShortSHA%x00AuthorName%x00AuthorEmail%x00Date%x00Subject%x00Body
            var format = "COMMIT_START%x01%H%x00%h%x00%an%x00%ae%x00%aI%x00%s%x00%b";
            var args = $"log --format=\"{format}\" --shortstat -n {maxCount}";

            if (!string.IsNullOrEmpty(authorIdentifier))
            {
                args += $" --author=\"{authorIdentifier}\"";
            }

            if (since.HasValue)
            {
                args += $" --since=\"{since.Value:yyyy-MM-dd}\"";
            }

            if (until.HasValue)
            {
                args += $" --until=\"{until.Value:yyyy-MM-dd}\"";
            }

            var output = await RunGitCommandAsync(localPath, args);

            if (string.IsNullOrEmpty(output))
            {
                _logger?.LogInformation("No commits found for criteria in {Path}", localPath);
                return commits;
            }

            // Parse the output - commits are separated by "COMMIT_START\x01"
            // Each block contains: metadata line, optional empty lines, shortstat line
            var commitStrings = output.Split("COMMIT_START\x01", StringSplitOptions.RemoveEmptyEntries);

            foreach (var commitStr in commitStrings)
            {
                var lines = commitStr.Trim().Split('\n');
                if (lines.Length == 0)
                {
                    continue;
                }

                // First line is the metadata (SHA|short|author|email|date|subject|body)
                var metadataLine = lines[0];
                var parts = metadataLine.Split('\x00');

                if (parts.Length >= 6)
                {
                    // Parse shortstat from remaining lines
                    int additions = 0;
                    int deletions = 0;

                    foreach (var line in lines.Skip(1))
                    {
                        // shortstat format: " 3 files changed, 45 insertions(+), 12 deletions(-)"
                        var statLine = line.Trim();
                        if (statLine.Contains("insertion") || statLine.Contains("deletion"))
                        {
                            var insertMatch = System.Text.RegularExpressions.Regex.Match(statLine, @"(\d+) insertion");
                            var deleteMatch = System.Text.RegularExpressions.Regex.Match(statLine, @"(\d+) deletion");

                            if (insertMatch.Success)
                            {
                                additions = int.Parse(insertMatch.Groups[1].Value);
                            }

                            if (deleteMatch.Success)
                            {
                                deletions = int.Parse(deleteMatch.Groups[1].Value);
                            }

                            break; // Found the shortstat line
                        }
                    }

                    var commit = new LocalCommit(
                        Sha: parts[0],
                        ShortSha: parts[1],
                        AuthorName: parts[2],
                        AuthorEmail: parts[3],
                        CommitDate: DateTimeOffset.Parse(parts[4]),
                        Subject: parts[5],
                        Body: parts.Length > 6 ? parts[6].Trim() : string.Empty,
                        Additions: additions,
                        Deletions: deletions);

                    commits.Add(commit);
                }
            }

            _logger?.LogInformation("Found {Count} commits in {Path}", commits.Count, localPath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get commits from {Path}", localPath);
        }

        return commits;
    }

    /// <summary>
    /// Gets the current HEAD commit SHA.
    /// </summary>
    /// <param name="localPath">Path to the local git repository.</param>
    /// <returns>The HEAD commit SHA, or null if it cannot be determined.</returns>
    public async Task<string?> GetHeadShaAsync(string localPath)
    {
        try
        {
            var output = await RunGitCommandAsync(localPath, "rev-parse HEAD");
            return output?.Trim();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get HEAD SHA from {Path}", localPath);
            return null;
        }
    }

    /// <summary>
    /// Gets the current branch name.
    /// </summary>
    /// <param name="localPath">Path to the local git repository.</param>
    /// <returns>The current branch name, or null if it cannot be determined.</returns>
    public async Task<string?> GetCurrentBranchAsync(string localPath)
    {
        try
        {
            var output = await RunGitCommandAsync(localPath, "rev-parse --abbrev-ref HEAD");
            return output?.Trim();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get current branch from {Path}", localPath);
            return null;
        }
    }

    /// <summary>
    /// Checks if the local repository is in sync with the remote.
    /// </summary>
    /// <param name="localPath">Path to the local git repository.</param>
    /// <param name="remoteName">Name of the remote to check against.</param>
    /// <returns>True if local is at same commit as remote, false otherwise.</returns>
    public async Task<bool> IsInSyncWithRemoteAsync(string localPath, string remoteName = "origin")
    {
        try
        {
            // Fetch remote refs without changing local state
            await RunGitCommandAsync(localPath, $"fetch {remoteName} --dry-run");

            var localHead = await GetHeadShaAsync(localPath);
            var branch = await GetCurrentBranchAsync(localPath);

            if (string.IsNullOrEmpty(localHead) || string.IsNullOrEmpty(branch))
            {
                return false;
            }

            // Get the remote tracking branch SHA
            var remoteRef = $"{remoteName}/{branch}";
            var remoteHead = await RunGitCommandAsync(localPath, $"rev-parse {remoteRef}");

            return localHead == remoteHead?.Trim();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not check sync status for {Path}", localPath);
            return false;
        }
    }

    /// <summary>
    /// Gets a list of unique authors who committed to this repository.
    /// </summary>
    /// <param name="localPath">Path to the local git repository.</param>
    /// <returns>A list of tuples containing author name and email.</returns>
    public async Task<List<(string Name, string Email)>> GetAuthorsAsync(string localPath)
    {
        var authors = new List<(string Name, string Email)>();

        try
        {
            var output = await RunGitCommandAsync(localPath, "log --format=\"%an|%ae\" | sort -u");

            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Distinct())
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2)
                    {
                        authors.Add((parts[0].Trim(), parts[1].Trim()));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get authors from {Path}", localPath);
        }

        return authors;
    }

    /// <summary>
    /// Gets the repository name from the local path (folder name).
    /// </summary>
    /// <param name="localPath">Path to the local git repository.</param>
    /// <returns>The repository name (directory name).</returns>
    public static string GetRepositoryName(string localPath)
    {
        return new DirectoryInfo(localPath).Name;
    }

    private async Task<string?> RunGitCommandAsync(string workingDirectory, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                _logger?.LogWarning("Git command failed: {Error}", error);
                return null;
            }

            return output;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to run git command: {Args}", arguments);
            return null;
        }
    }
}
