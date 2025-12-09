using Standup.Domain.Entities;

namespace Standup.Domain.Interfaces;

public interface IAISummaryService
{
    Task<string> GenerateSummaryAsync(
        StandupData data,
        SummaryOptions? options = null,
        CancellationToken cancellationToken = default);
}
