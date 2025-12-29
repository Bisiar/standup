using System.Reflection;
using Standup.Application.Interfaces;
using Standup.Application.Models;

namespace Standup.Maui.Services;

/// <summary>
/// Service for loading embedded documentation files from the wiki.
/// </summary>
public class DocumentService : IDocumentService
{
    /// <summary>
    /// Preferred document order matching the wiki .order file.
    /// </summary>
    private static readonly string[] DocumentOrder =
    [
        "Home",
        "Getting-Started",
        "Configuration",
        "MAUI-App",
        "Teams-Integration",
        "Local-Repository-Support",
        "Troubleshooting"
    ];

    /// <inheritdoc/>
    public Task<IEnumerable<DocumentItem>> GetDocumentsAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith("Docs.") && r.EndsWith(".md"))
            .Select(r =>
            {
                var filename = r.Replace("Docs.", string.Empty).Replace(".md", string.Empty);
                return new DocumentItem(
                    Path: r,
                    Title: FormatTitle(filename),
                    Category: "Documentation");
            })
            .OrderBy(d => GetSortOrder(d.Path))
            .AsEnumerable();

        return Task.FromResult(resources);
    }

    /// <inheritdoc/>
    public async Task<string> GetDocumentContentAsync(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(path);

        if (stream == null)
        {
            return "Document not found.";
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static int GetSortOrder(string path)
    {
        var filename = path.Replace("Docs.", string.Empty).Replace(".md", string.Empty);
        var index = Array.FindIndex(DocumentOrder, o => o.Equals(filename, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : 999;
    }

    private static string FormatTitle(string filename)
    {
        return filename
            .Replace("-", " ")
            .Replace("_", " ");
    }
}
