using System.Diagnostics;
using System.Text;
using Foundation;

namespace Standup.Maui.Platforms.MacCatalyst.Helpers;

public static class DiagnosticsHelper
{
    private static string LogFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "standup_diagnostics.log");

    public static void Initialize()
    {
        try
        {
            LogInfo("========== DIAGNOSTICS INITIALIZATION STARTED ==========");

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LogCritical($"UNHANDLED EXCEPTION: {args.ExceptionObject}");
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogCritical($"UNOBSERVED TASK EXCEPTION: {args.Exception}");
                args.SetObserved();
            };

            LogEnvironmentInfo();
            LogInfo("========== DIAGNOSTICS INITIALIZATION COMPLETED ==========");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing diagnostics: {ex}");
            LogError($"Failed to initialize diagnostics: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public static void LogEnvironmentInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ENVIRONMENT INFORMATION ===");

        // App info
        sb.AppendLine($"App Version: {AppInfo.VersionString} ({AppInfo.BuildString})");
        sb.AppendLine($"Platform: {DeviceInfo.Platform}");
        sb.AppendLine($"Device Type: {DeviceInfo.DeviceType}");
        sb.AppendLine($"OS Version: {DeviceInfo.VersionString}");
        sb.AppendLine($"Device Name: {DeviceInfo.Name}");
        sb.AppendLine($"Manufacturer: {DeviceInfo.Manufacturer}");
        sb.AppendLine($"Model: {DeviceInfo.Model}");

        // Bundle info
        var bundleInfo = NSBundle.MainBundle.InfoDictionary;
        sb.AppendLine($"Bundle ID: {NSBundle.MainBundle.BundleIdentifier}");
        sb.AppendLine($"Bundle Path: {NSBundle.MainBundle.BundlePath}");

        LogInfo(sb.ToString());
    }

    public static void LogInfo(string message) => Log("INFO", message);

    public static void LogError(string message) => Log("ERROR", message);

    public static void LogCritical(string message) => Log("CRITICAL", message);

    public static void LogWarning(string message) => Log("WARNING", message);

    public static void LogDebug(string message) => Log("DEBUG", message);

    private static void Log(string level, string message)
    {
        try
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";

            Debug.WriteLine(logEntry);
            Console.WriteLine($"(DiagnosticsHelper) {logEntry}");

            File.AppendAllText(LogFilePath, logEntry);

            if (level == "CRITICAL")
            {
                try
                {
                    var logContent = File.ReadAllText(LogFilePath);
                    Console.WriteLine("========== BEGIN FULL LOG DUMP ==========");
                    Console.WriteLine(logContent);
                    Console.WriteLine("========== END FULL LOG DUMP ==========");
                }
                catch (Exception readEx)
                {
                    Console.WriteLine($"Failed to dump log file: {readEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error writing to log: {ex}");
            Console.WriteLine($"Error writing to log: {ex}");
        }
    }
}
