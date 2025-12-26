namespace Standup.Infrastructure.Tests.Configuration;

/// <summary>
/// Helper class to load environment variables from .env files for test execution.
/// </summary>
public static class TestEnvironmentLoader
{
    private static readonly object LockObject = new();
    private static bool _isLoaded;

    /// <summary>
    /// Load environment variables from the Azure Developer CLI .env file.
    /// This method is safe to call multiple times - it will only load once.
    /// </summary>
    /// <param name="customEnvPath">Optional custom path to a .env file.</param>
    public static void LoadEnvironmentVariables(string? customEnvPath = null)
    {
        lock (LockObject)
        {
            if (_isLoaded)
            {
                return;
            }

            // Try multiple locations for .env file
            var envPaths = new[]
            {
                customEnvPath,
                Path.Combine(FindProjectRoot(), ".azure", "mcpsdkserver-dotnet", ".env"),
                Path.Combine(FindProjectRoot(), ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "..", ".azure", "mcpsdkserver-dotnet", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".azure", "mcpsdkserver-dotnet", ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            };

            foreach (var envPath in envPaths)
            {
                if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                {
                    LoadEnvFile(envPath);
                    _isLoaded = true;
                    return;
                }
            }

            // If no .env file found, that's okay - environment variables might be set elsewhere
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Reset the loader state (mainly for testing purposes).
    /// </summary>
    public static void Reset()
    {
        lock (LockObject)
        {
            _isLoaded = false;
        }
    }

    /// <summary>
    /// Load environment variables from a specific .env file.
    /// </summary>
    private static void LoadEnvFile(string envPath)
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim().Trim('"');

                // Only set if not already set (allows override from system environment)
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }

    /// <summary>
    /// Find the project root directory by looking for .sln file.
    /// </summary>
    private static string FindProjectRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();

        while (currentDir != null)
        {
            if (Directory.GetFiles(currentDir, "*.sln").Length > 0)
            {
                return currentDir;
            }

            var parent = Directory.GetParent(currentDir);
            currentDir = parent?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}
