using Standup.Application.Interfaces;

namespace Standup.Application.Tests.TestHelpers;

/// <summary>
/// Fake folder picker service for testing.
/// NO MOCKS - real test implementation.
/// </summary>
public sealed class FakeFolderPickerService : IFolderPickerService
{
    private readonly Queue<string?> _results = new();

    public void EnqueueResult(string? folderPath)
    {
        _results.Enqueue(folderPath);
    }

    public Task<string?> BrowseForRepositoryFolderAsync()
    {
        if (_results.Count > 0)
        {
            return Task.FromResult(_results.Dequeue());
        }

        return Task.FromResult<string?>(null);
    }
}
