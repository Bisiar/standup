using MediatR;
using Standup.Application.DTOs;
using Standup.Application.Interfaces;
using Standup.Domain.Entities;
using Standup.Domain.Interfaces;

namespace Standup.Application.Features.ConfigureRepository;

public class AddRepositoryHandler : IRequestHandler<AddRepositoryCommand, SourceRepositoryDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRepository<SourceRepository> _repositoryRepo;
    private readonly ISourceProviderFactory _providerFactory;
    private readonly IEncryptionService _encryptionService;

    public AddRepositoryHandler(
        IUserRepository userRepository,
        IRepository<SourceRepository> repositoryRepo,
        ISourceProviderFactory providerFactory,
        IEncryptionService encryptionService)
    {
        _userRepository = userRepository;
        _repositoryRepo = repositoryRepo;
        _providerFactory = providerFactory;
        _encryptionService = encryptionService;
    }

    public async Task<SourceRepositoryDto> Handle(AddRepositoryCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.UserId} not found");

        var dto = request.Repository;

        var sourceRepo = new SourceRepository
        {
            UserId = user.Id,
            SourceType = dto.SourceType,
            Organization = dto.Organization,
            Project = dto.Project,
            Repository = dto.Repository,
            DisplayName = dto.DisplayName,
            AuthorIdentifier = dto.AuthorIdentifier,
            DefaultBranch = dto.DefaultBranch,
            EncryptedPat = dto.PersonalAccessToken != null
                ? await _encryptionService.EncryptAsync(dto.PersonalAccessToken)
                : null
        };

        var provider = _providerFactory.GetProvider(sourceRepo.SourceType);
        var isValid = await provider.ValidateConnectionAsync(sourceRepo, cancellationToken);
        if (!isValid)
        {
            throw new InvalidOperationException("Failed to validate repository connection. Check credentials and repository path.");
        }

        await _repositoryRepo.AddAsync(sourceRepo, cancellationToken);

        return new SourceRepositoryDto(
            sourceRepo.Id,
            sourceRepo.SourceType,
            sourceRepo.Organization,
            sourceRepo.Project,
            sourceRepo.Repository,
            sourceRepo.DisplayName,
            sourceRepo.AuthorIdentifier,
            sourceRepo.DefaultBranch,
            sourceRepo.IsActive);
    }
}
