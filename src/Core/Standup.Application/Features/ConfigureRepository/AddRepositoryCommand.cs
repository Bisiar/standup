using MediatR;
using Standup.Application.DTOs;

namespace Standup.Application.Features.ConfigureRepository;

public record AddRepositoryCommand(
    string UserId,
    CreateSourceRepositoryDto Repository) : IRequest<SourceRepositoryDto>;
