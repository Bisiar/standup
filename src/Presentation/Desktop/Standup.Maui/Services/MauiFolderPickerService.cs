using CommunityToolkit.Maui.Storage;
using Serilog;
using Standup.Application.Interfaces;

namespace Standup.Maui.Services;

/// <summary>
/// MAUI implementation of folder picker service using CommunityToolkit.Maui.
/// </summary>
public class MauiFolderPickerService : IFolderPickerService
{
    /// <summary>
    /// Shows a folder picker dialog and returns the selected folder path.
    /// </summary>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    public async Task<string?> BrowseForRepositoryFolderAsync()
    {
        Log.Information("BrowseForRepositoryFolderAsync - showing folder picker");
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);

            if (result.IsSuccessful)
            {
                Log.Information("Folder selected: {Path}", result.Folder.Path);
                return result.Folder.Path;
            }

            Log.Information("Folder picker cancelled or failed: {Exception}", result.Exception?.Message);
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in folder picker");
            return null;
        }
    }
}
