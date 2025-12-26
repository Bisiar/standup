using Standup.Domain.Entities;

namespace Standup.Application.Services;

public interface IStandupAggregatorService
{
    Task<StandupData> AggregateDataAsync(
        IEnumerable<SourceRepository> repositories,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default);
}
