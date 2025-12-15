using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Application.Interfaces;

/// <summary>
/// Factory for creating source control providers.
/// </summary>
public interface ISourceProviderFactory
{
    /// <summary>
    /// Gets a source provider for the specified source type.
    /// </summary>
    /// <param name="sourceType">The type of source control system.</param>
    /// <returns>The source provider instance.</returns>
    ISourceProvider GetProvider(SourceType sourceType);
}
