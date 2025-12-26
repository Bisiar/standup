using MediatR;
using Standup.Application.DTOs;
using Standup.Domain.Interfaces;

namespace Standup.Application.Features.ConfigureRepository;

public class ListRepositoriesHandler : IRequestHandler<ListRepositoriesQuery, IEnumerable<SourceRepositoryDto>>
{
    private readonly IUserRepository _userRepository;

    public ListRepositoriesHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<SourceRepositoryDto>> Handle(ListRepositoriesQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetWithRepositoriesAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.UserId} not found");

        return user.SourceRepositories.Select(r => new SourceRepositoryDto(
            r.Id,
            r.SourceType,
            r.Organization,
            r.Project,
            r.Repository,
            r.DisplayName,
            r.AuthorIdentifier,
            r.DefaultBranch,
            r.IsActive));
    }
}
