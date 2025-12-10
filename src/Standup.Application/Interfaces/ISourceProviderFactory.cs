using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Application.Interfaces;

public interface ISourceProviderFactory
{
    ISourceProvider GetProvider(SourceType sourceType);
}
