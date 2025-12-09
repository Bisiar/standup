using Standup.Maui.Models;

namespace Standup.Maui.Services;

public interface IDocumentService
{
    Task<IEnumerable<DocumentItem>> GetDocumentsAsync();
    Task<string> GetDocumentContentAsync(string path);
}
