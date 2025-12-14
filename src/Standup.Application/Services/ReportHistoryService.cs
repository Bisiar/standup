using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;

namespace Standup.Application.Services;

/// <summary>
/// Application service for managing report history.
/// </summary>
public sealed class ReportHistoryService
{
    private readonly IReportHistoryRepository _repository;

    public ReportHistoryService(IReportHistoryRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<ReportHistory>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<ReportHistory?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<ReportHistory>> GetByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
        => _repository.GetByGroupIdAsync(groupId, cancellationToken);

    public Task<ReportHistory?> GetLatestByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
        => _repository.GetLatestByGroupIdAsync(groupId, cancellationToken);

    /// <summary>
    /// Saves a grouped standup report to history.
    /// </summary>
    public async Task<ReportHistory> SaveReportAsync(
        GroupedStandupReportDto report,
        string reportContent,
        CancellationToken cancellationToken = default)
    {
        var history = new ReportHistory
        {
            GroupId = report.Id,
            GroupName = report.GroupName,
            PeriodStart = report.PeriodStart,
            PeriodEnd = report.PeriodEnd,
            SummaryType = report.CurrentSummaryType,
            TotalCommits = report.TotalCommits,
            TotalPullRequests = report.TotalPullRequests,
            TotalWorkItems = report.TotalWorkItems,
            ReportContent = reportContent,
            ClientCodes = report.Sections.Select(s => s.ClientCode).ToList()
        };

        return await _repository.AddAsync(history, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    public Task DeleteByGroupIdAsync(string groupId, CancellationToken cancellationToken = default)
        => _repository.DeleteByGroupIdAsync(groupId, cancellationToken);
}
