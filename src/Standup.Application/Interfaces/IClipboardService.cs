namespace Standup.Application.Interfaces;

/// <summary>
/// Service for clipboard operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Sets text to the clipboard.
    /// </summary>
    /// <param name="text">The text to copy to clipboard.</param>
    /// <returns>A task representing the operation.</returns>
    Task SetTextAsync(string text);
}
