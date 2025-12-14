namespace Standup.Maui.Views;

/// <summary>
/// Extension methods for visual tree traversal.
/// </summary>
public static class VisualTreeExtensions
{
    /// <summary>
    /// Gets the first parent of the specified type in the visual tree.
    /// </summary>
    public static T? GetVisualTreeParent<T>(this Element element)
        where T : Element
    {
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent is T typed)
            {
                return typed;
            }

            parent = parent.Parent;
        }

        return null;
    }
}
