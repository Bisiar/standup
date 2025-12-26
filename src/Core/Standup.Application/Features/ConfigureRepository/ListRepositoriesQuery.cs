using MediatR;
using Standup.Application.DTOs;

namespace Standup.Application.Features.ConfigureRepository;

public record ListRepositoriesQuery(string UserId) : IRequest<IEnumerable<SourceRepositoryDto>>;
