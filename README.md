# Standup Automation Platform

Automate your daily standup reports by collecting work activity from GitHub and Azure DevOps, summarizing with AI, and delivering via Microsoft Teams and email.

## Features

- **Multi-Source Support**: GitHub and Azure DevOps repositories
- **Multi-Tenant**: Configure different projects for JourneyTeam, Digital Solutions, etc.
- **AI Summarization**: Natural language summaries using Azure AI Foundry (GPT-4o)
- **Teams Integration**: DMs, channel posts, and a Teams app for subscriptions
- **MCP Server**: AI agents can generate standups via MCP tools
- **MAUI App**: Cross-platform desktop/mobile app with multi-instance support
- **Azure DevOps Wiki**: Full documentation compatible with Azure DevOps wiki publishing

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│  MAUI App     │  Teams App     │  MCP Server (API)                  │
└───────────────┴────────────────┴────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────────────┐
│  Application Layer (MediatR Handlers)                                 │
│  GenerateStandup | ConfigureRepository | ManageSubscriptions          │
└───────────────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────────────┐
│  Infrastructure Layer                                                  │
│  GitHub | Azure DevOps | AI Foundry | Teams (Graph) | Email           │
└───────────────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────────────────────────────────────────────────┐
│  Domain Layer                                                          │
│  Tenant | User | SourceRepository | StandupReport | TeamSubscription  │
└───────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
Standup/
├── src/
│   ├── Standup.Domain/           # Core entities and interfaces
│   ├── Standup.Application/      # Use cases, MediatR handlers
│   ├── Standup.Infrastructure/   # External integrations
│   ├── Standup.Api/              # MCP Server + REST API
│   ├── Standup.Maui/             # Desktop/mobile app
│   └── Standup.Teams/            # Teams bot and app
├── wiki/                         # Azure DevOps compatible documentation
├── infra/                        # Azure Bicep templates
└── tests/                        # Unit and integration tests
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Azure subscription
- GitHub or Azure DevOps account
- Microsoft 365 account (for Teams)

### Quick Deploy

```bash
# Clone the repository
git clone https://github.com/your-org/standup.git
cd standup

# Login to Azure
azd auth login

# Deploy to Azure
azd up
```

### Local Development

```bash
# Restore packages
dotnet restore

# Run the API
dotnet run --project src/Standup.Api

# Run the MAUI app
dotnet run --project src/Standup.Maui
```

### Install as CLI Tool

```bash
dotnet tool install -g Standup.Cli
standup
```

## Configuration

### Environment Variables

| Variable | Description |
|----------|-------------|
| `AI_FOUNDRY_ENDPOINT` | Azure AI Foundry endpoint |
| `AI_FOUNDRY_DEPLOYMENT` | Model deployment name (default: gpt-4o) |
| `COSMOS_ENDPOINT` | Cosmos DB endpoint |

### Multi-Project Setup

The MAUI app supports multiple project instances:

1. **JourneyTeam** - Main company projects
2. **Digital Solutions** - Side project company

Each project can have different:
- API endpoints
- Repository configurations
- Notification preferences

## Documentation

Full documentation is available in the `/wiki` folder, compatible with Azure DevOps wiki publishing:

- [Getting Started](wiki/Getting-Started.md)
- [Architecture](wiki/Architecture.md)
- [Configuration](wiki/Configuration.md)
- [API Reference](wiki/API-Reference.md)
- [MAUI App Guide](wiki/MAUI-App.md)
- [Teams Integration](wiki/Teams-Integration.md)
- [Deployment](wiki/Deployment.md)
- [Troubleshooting](wiki/Troubleshooting.md)

## MCP Tools

The API exposes MCP tools for AI agents:

- `GenerateStandup` - Generate a standup report
- `PreviewStandup` - Preview without sending
- `ListRepositories` - List configured repos
- `AddGitHubRepository` - Add a GitHub repo
- `AddAzureDevOpsRepository` - Add an Azure DevOps repo

## Tech Stack

- **.NET 10** - Runtime
- **Clean Architecture** - Project structure
- **MediatR** - CQRS pattern
- **MAUI** - Cross-platform UI
- **Azure Functions (Flex)** - API hosting
- **Azure Cosmos DB** - Data persistence
- **Azure AI Foundry** - AI summarization
- **Microsoft Graph** - Teams and Email
- **Octokit** - GitHub integration
- **Azure DevOps SDK** - Azure DevOps integration

## AI Tools

See [AMPLIFIER_TOOLS.md](./AMPLIFIER_TOOLS.md) for available AI-powered CLI tools.

## License

See [LICENSE.md](LICENSE.md)
