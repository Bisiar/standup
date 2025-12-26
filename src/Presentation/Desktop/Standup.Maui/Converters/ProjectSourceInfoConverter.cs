using System.Globalization;
using Standup.Application.Models;

namespace Standup.Maui.Converters;

/// <summary>
/// Converts a ProjectInstance to display source info (org/project/repo or API endpoint).
/// </summary>
public class ProjectSourceInfoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProjectInstance project)
        {
            if (project.UseLocalGeneration)
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(project.SourceOrganization))
                {
                    parts.Add(project.SourceOrganization);
                }

                if (!string.IsNullOrEmpty(project.SourceProject))
                {
                    parts.Add(project.SourceProject);
                }

                if (!string.IsNullOrEmpty(project.SourceRepository))
                {
                    parts.Add(project.SourceRepository);
                }

                if (parts.Count > 0)
                {
                    return $"Source: {string.Join(" / ", parts)}";
                }

                return "Local generation (no source configured)";
            }
            else
            {
                if (!string.IsNullOrEmpty(project.ApiEndpoint))
                {
                    return $"API: {project.ApiEndpoint}";
                }

                return "API mode (no endpoint configured)";
            }
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
