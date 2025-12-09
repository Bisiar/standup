using FluentAssertions;
using NSubstitute;
using Standup.Application.Features.GenerateStandup;
using Standup.Application.Services;
using Standup.Domain.Entities;
using Standup.Domain.Enums;
using Standup.Domain.Interfaces;

namespace Standup.Application.Tests.Features;

public sealed class GenerateStandupHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IStandupAggregatorService _aggregatorService;
    private readonly IAISummaryService _summaryService;
    private readonly INotificationService _teamsNotificationService;
    private readonly IRepository<StandupReport> _reportRepository;
    private readonly GenerateStandupHandler _handler;

    public GenerateStandupHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _aggregatorService = Substitute.For<IStandupAggregatorService>();
        _summaryService = Substitute.For<IAISummaryService>();
        _teamsNotificationService = Substitute.For<INotificationService>();
        _teamsNotificationService.Channel.Returns(NotificationChannel.Teams);
        _reportRepository = Substitute.For<IRepository<StandupReport>>();

        _handler = new GenerateStandupHandler(
            _userRepository,
            _aggregatorService,
            _summaryService,
            new[] { _teamsNotificationService },
            _reportRepository);
    }

    [Fact]
    public async Task Handle_WithValidUser_GeneratesStandupReport()
    {
        // Arrange
        var user = CreateTestUser();
        var standupData = new StandupData(
            new List<CommitInfo> { new("sha1", "feat: add feature", "john", DateTimeOffset.UtcNow, "repo") },
            new List<PullRequestInfo>(),
            new List<WorkItemInfo>());

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        _aggregatorService.AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(standupData);

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("AI generated summary");

        var command = new GenerateStandupCommand("user-123", "tenant-456");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().Be("AI generated summary");
        result.CommitCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithCustomDateRange_UsesProvidedDates()
    {
        // Arrange
        var user = CreateTestUser();
        var since = DateTimeOffset.UtcNow.AddDays(-7);
        var until = DateTimeOffset.UtcNow;

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        _aggregatorService.AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new StandupData(new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()));

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        var command = new GenerateStandupCommand("user-123", "tenant-456", since, until);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _aggregatorService.Received(1).AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            since,
            until,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSaveReportTrue_SavesReportToRepository()
    {
        // Arrange
        var user = CreateTestUser();

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        _aggregatorService.AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new StandupData(new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()));

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        var command = new GenerateStandupCommand("user-123", "tenant-456", SaveReport: true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _reportRepository.Received(1).AddAsync(
            Arg.Any<StandupReport>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSaveReportFalse_DoesNotSaveReport()
    {
        // Arrange
        var user = CreateTestUser();

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        _aggregatorService.AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new StandupData(new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()));

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        var command = new GenerateStandupCommand("user-123", "tenant-456", SaveReport: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _reportRepository.DidNotReceive().AddAsync(
            Arg.Any<StandupReport>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSendToChannels_SendsNotifications()
    {
        // Arrange
        var user = CreateTestUser();

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        _aggregatorService.AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new StandupData(new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()));

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        _teamsNotificationService.SendStandupReportAsync(
            Arg.Any<User>(),
            Arg.Any<StandupReport>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new GenerateStandupCommand(
            "user-123",
            "tenant-456",
            SendTo: new List<NotificationChannel> { NotificationChannel.Teams });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _teamsNotificationService.Received(1).SendStandupReportAsync(
            Arg.Any<User>(),
            Arg.Any<StandupReport>(),
            Arg.Any<CancellationToken>());
        result.SentTo.Should().Contain(NotificationChannel.Teams);
    }

    [Fact]
    public async Task Handle_WithUserNotFound_ThrowsException()
    {
        // Arrange
        _userRepository.GetWithRepositoriesAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new GenerateStandupCommand("non-existent", "tenant-456");

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User non-existent not found");
    }

    [Fact]
    public async Task Handle_UpdatesUserLastStandupAt()
    {
        // Arrange
        var user = CreateTestUser();
        user.LastStandupAt = null;

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        _aggregatorService.AggregateDataAsync(
            Arg.Any<IEnumerable<SourceRepository>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new StandupData(new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()));

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        var command = new GenerateStandupCommand("user-123", "tenant-456");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.LastStandupAt.Should().NotBeNull();
        user.LastStandupAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyAggregatesActiveRepositories()
    {
        // Arrange
        var user = CreateTestUser();
        user.SourceRepositories.Add(new SourceRepository { Repository = "active-repo", IsActive = true });
        user.SourceRepositories.Add(new SourceRepository { Repository = "inactive-repo", IsActive = false });

        _userRepository.GetWithRepositoriesAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(user);

        IEnumerable<SourceRepository>? capturedRepos = null;
        _aggregatorService.AggregateDataAsync(
            Arg.Do<IEnumerable<SourceRepository>>(repos => capturedRepos = repos),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(new StandupData(new List<CommitInfo>(), new List<PullRequestInfo>(), new List<WorkItemInfo>()));

        _summaryService.GenerateSummaryAsync(
            Arg.Any<StandupData>(),
            Arg.Any<SummaryOptions>(),
            Arg.Any<CancellationToken>())
            .Returns("Summary");

        var command = new GenerateStandupCommand("user-123", "tenant-456");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRepos.Should().NotBeNull();
        capturedRepos!.Should().HaveCount(1);
        capturedRepos!.First().Repository.Should().Be("active-repo");
    }

    private static User CreateTestUser()
    {
        return new User
        {
            Id = "user-123",
            TenantId = "tenant-456",
            DisplayName = "John Doe",
            Email = "john.doe@company.com",
            Preferences = new UserPreferences
            {
                TimeZone = "America/Denver",
                SkipWeekends = true,
                NotificationChannels = new List<NotificationChannel> { NotificationChannel.Teams }
            }
        };
    }
}
