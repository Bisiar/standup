using Standup.Application.Models;

namespace Standup.Application.Interfaces;

public interface IDocumentService
{
    Task<IEnumerable<DocumentItem>> GetDocumentsAsync();
    Task<string> GetDocumentContentAsync(string path);
}
