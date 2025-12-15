using Serilog;
using Standup.Application.Interfaces;
using IOPath = System.IO.Path;

namespace Standup.Maui.Services;

/// <summary>
/// MAUI implementation of folder picker service.
/// Since CommunityToolkit.Maui's FolderPicker is disabled (causes crash on macOS 26),
/// we use a workaround: ask user to select the .git/config file, then derive the folder.
/// </summary>
public class MauiFolderPickerService : IFolderPickerService
{
    public async Task<string?> BrowseForRepositoryFolderAsync()
    {
        Log.Information("BrowseForRepositoryFolderAsync - showing file picker for .git/config");
        try
        {
            // Show an alert explaining what to do
            var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlertAsync(
                    "Select Repository",
                    "Please navigate to your git repository folder and select the '.git/config' file.\n\nTip: Press Cmd+Shift+. to show hidden files in the file picker.",
                    "OK");
            }

            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.macOS, new[] { "public.data", "*" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.data", "*" } }
                });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select .git/config file in your repository",
                FileTypes = customFileType
            });

            if (result != null)
            {
                var filePath = result.FullPath;
                Log.Information("File selected: {Path}", filePath);

                // If user selected .git/config, go up two levels to get repo root
                if (filePath.EndsWith(".git/config") || filePath.EndsWith(".git\\config"))
                {
                    var repoPath = IOPath.GetDirectoryName(IOPath.GetDirectoryName(filePath));
                    Log.Information("Derived repo path: {Path}", repoPath);
                    return repoPath;
                }

                // If user selected any file, try to find .git folder
                var dirPath = IOPath.GetDirectoryName(filePath);
                while (!string.IsNullOrEmpty(dirPath))
                {
                    if (Directory.Exists(IOPath.Combine(dirPath, ".git")))
                    {
                        Log.Information("Found .git folder, repo path: {Path}", dirPath);
                        return dirPath;
                    }

                    dirPath = IOPath.GetDirectoryName(dirPath);
                }

                // Couldn't find .git, just return the folder of the selected file
                return IOPath.GetDirectoryName(filePath);
            }

            Log.Information("File picker cancelled");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in file picker");
            return null;
        }
    }
}
