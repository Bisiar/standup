using Standup.Application.Models;

namespace Standup.Application.Interfaces;

/// <summary>
/// Service for managing documentation files.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Gets all available documents.
    /// </summary>
    /// <returns>List of document items.</returns>
    Task<IEnumerable<DocumentItem>> GetDocumentsAsync();

    /// <summary>
    /// Gets the content of a document.
    /// </summary>
    /// <param name="path">The path to the document.</param>
    /// <returns>The document content as a string.</returns>
    Task<string> GetDocumentContentAsync(string path);
}
