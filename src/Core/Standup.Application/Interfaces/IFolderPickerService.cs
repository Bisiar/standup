namespace Standup.Application.Interfaces;

/// <summary>
/// Service for picking folders from the file system.
/// Platform-specific implementations handle the actual file/folder picker UI.
/// </summary>
public interface IFolderPickerService
{
    /// <summary>
    /// Prompts user to browse for a local git repository folder.
    /// On platforms where folder pickers aren't available, this may prompt
    /// the user to select a file (like .git/config) and derive the folder from it.
    /// </summary>
    /// <returns>The repository root path, or null if cancelled.</returns>
    Task<string?> BrowseForRepositoryFolderAsync();
}
