using Standup.Maui.Models;
using System.Reflection;

namespace Standup.Maui.Services;

public class DocumentService : IDocumentService
{
    public Task<IEnumerable<DocumentItem>> GetDocumentsAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith("Docs.") && r.EndsWith(".md"))
            .Select(r =>
            {
                var path = r.Replace("Docs.", "").Replace(".md", "");
                var parts = path.Split('.');
                return new DocumentItem
                {
                    Path = r,
                    Title = FormatTitle(parts.Last()),
                    Category = parts.Length > 1 ? parts.First() : "General"
                };
            })
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Title)
            .AsEnumerable();

        return Task.FromResult(resources);
    }

    public async Task<string> GetDocumentContentAsync(string path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(path);

        if (stream == null)
            return "Document not found.";

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static string FormatTitle(string filename)
    {
        return filename
            .Replace("-", " ")
            .Replace("_", " ");
    }
}
