# Architecture

The Standup Automation Platform follows Clean Architecture principles with distinct layers for separation of concerns.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Presentation                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                  │
│  │  MAUI App   │  │  Teams App  │  │  MCP Server │                  │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘                  │
└─────────┼────────────────┼────────────────┼─────────────────────────┘
          │                │                │
          └────────────────┼────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────────┐
│                          Application                                 │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    MediatR Handlers                          │   │
│  │  GenerateStandup | ConfigureRepository | ManageSubscriptions │   │
│  └─────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    Services                                  │   │
│  │              StandupAggregatorService                        │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────────┐
│                         Infrastructure                               │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐    │
│  │   GitHub   │  │   Azure    │  │    AI      │  │   Graph    │    │
│  │  Provider  │  │  DevOps    │  │  Foundry   │  │  (Teams/   │    │
│  │            │  │  Provider  │  │            │  │   Email)   │    │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                           │
┌─────────────────────────────────────────────────────────────────────┐
│                           Domain                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Entities: Tenant, User, SourceRepository, StandupReport    │   │
│  │  Interfaces: ISourceProvider, INotificationService          │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### Domain Layer (`Standup.Domain`)

The core of the application containing:
- **Entities**: Business objects like `User`, `Tenant`, `StandupReport`
- **Interfaces**: Contracts like `ISourceProvider`, `INotificationService`
- **Value Objects**: Immutable types like `DateRange`
- **Enums**: `SourceType`, `NotificationChannel`

### Application Layer (`Standup.Application`)

Orchestrates the use cases:
- **Features**: CQRS handlers using MediatR
  - `GenerateStandupCommand/Handler`
  - `AddRepositoryCommand/Handler`
- **Services**: Business logic coordination
  - `StandupAggregatorService`
- **DTOs**: Data transfer objects for API contracts

### Infrastructure Layer (`Standup.Infrastructure`)

External integrations:
- **Source Providers**: `GitHubSourceProvider`, `AzureDevOpsSourceProvider`
- **AI**: `AIFoundrySummaryService`
- **Notifications**: `TeamsNotificationService`, `EmailNotificationService`
- **Persistence**: Cosmos DB repositories

### Presentation Layer

User interfaces:
- **Standup.Api**: MCP Server + REST API
- **Standup.Maui**: Desktop/mobile app
- **Standup.Teams**: Teams bot and tab

## Data Flow

```
1. User triggers "Generate Standup"
           │
           ▼
2. GenerateStandupHandler receives command
           │
           ▼
3. StandupAggregatorService fetches data
   ├── GitHubSourceProvider.GetCommits()
   ├── AzureDevOpsSourceProvider.GetCommits()
   ├── GetOpenPullRequests()
   └── GetWorkItems()
           │
           ▼
4. AIFoundrySummaryService.GenerateSummary()
           │
           ▼
5. NotificationServices send to configured channels
   ├── TeamsNotificationService
   └── EmailNotificationService
           │
           ▼
6. StandupReport saved and returned
```

## Multi-Tenant Design

Each tenant (organization) has:
- Isolated configuration
- Separate AI Foundry endpoint (optional)
- Own Teams team/channel settings
- User-specific repository configurations

```
Tenant: JourneyTeam
├── Users
│   ├── User1 → [Repo1, Repo2]
│   └── User2 → [Repo3]
└── Schedules
    └── Morning Standup (8:00 AM MST)

Tenant: Digital Solutions
├── Users
│   └── User1 → [Repo4, Repo5]
└── Schedules
    └── Async Standup (Channel post)
```
