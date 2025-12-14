# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🔴 CRITICAL: Research Before Fixing

**Every failed app launch costs points. Research costs nothing.**

Before making ANY fix:
1. **Research first** - WebSearch, read documentation, understand the problem fully
2. **Grep the entire codebase** - find ALL instances of the problem pattern (not just the one in the error)
3. **Understand the root cause** - not just the symptom shown in logs
4. **Make ONE comprehensive fix** - covering all instances
5. **Only then ask user to test**

It is better to spend 8 hours researching and launch the app working once, than to launch it 100 times in an hour and fail each time.

## Build and Development Commands

```bash
# Build the entire solution
dotnet build

# Run API locally (Azure Function)
dotnet run --project src/Standup.Api

# Run MAUI app
dotnet run --project src/Standup.Maui

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Standup.Domain.Tests
dotnet test tests/Standup.Application.Tests
dotnet test tests/Standup.Infrastructure.Tests

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Deploy to Azure (provision + deploy)
azd up

# Deploy only (no provisioning)
azd deploy
```

## Architecture Overview

This is a **Standup Automation Platform** that collects work activity from GitHub/Azure DevOps, summarizes with AI, and delivers via Teams/email.

### Clean Architecture Layers

```
Standup.Api / Standup.Maui / Standup.Teams  (Presentation)
                    │
           Standup.Application              (Use Cases - MediatR handlers)
                    │
           Standup.Infrastructure           (External integrations)
                    │
           Standup.Domain                   (Entities, Interfaces)
```

**Dependency Rule**: Dependencies point inward. Domain has no dependencies. Infrastructure implements Domain interfaces.

### 🔴 CRITICAL: MAUI Project Code Placement

**The ONLY code that belongs in Standup.Maui is MAUI framework-specific code:**
- Views (XAML pages and code-behind)
- ViewModels (MAUI-specific UI state)
- Converters (MAUI value converters)
- Platform-specific code (Platforms/ folder)
- MAUI-specific infrastructure implementations (e.g., using Preferences for storage)

**Everything else goes in the proper layer:**
- Services/business logic → `Standup.Application`
- External integrations (APIs, git, etc.) → `Standup.Infrastructure`
- Entities, interfaces, value objects → `Standup.Domain`

**Examples:**
- GitConfigParser (parses .git/config) → `Standup.Infrastructure`
- LocalGitService (runs git commands) → `Standup.Infrastructure`
- GroupService (business logic) → `Standup.Application`
- PreferencesGroupRepository (MAUI Preferences) → `Standup.Maui` (MAUI-specific storage)

### Key Projects

| Project | Purpose | Host |
|---------|---------|------|
| `Standup.Api` | MCP Server + REST API | Azure Functions (Flex) |
| `Standup.Teams` | Teams bot and tab | Azure App Service |
| `Standup.Maui` | Desktop/mobile app | Local |
| `Standup.Application` | MediatR handlers, business logic | - |
| `Standup.Infrastructure` | GitHub, Azure DevOps, AI, Graph integrations | - |
| `Standup.Domain` | Core entities and interfaces | - |

### Domain Entities

- `Tenant` - Multi-tenant organization support
- `User` - User with repository configurations
- `SourceRepository` - GitHub or Azure DevOps repo config
- `StandupReport` - Generated standup with AI summary
- `TeamSubscription` - Teams channel subscription for standups

### Core Interfaces (in Domain, implemented in Infrastructure)

- `ISourceProvider` - Fetch commits, PRs, work items (GitHub/Azure DevOps)
- `IAISummaryService` - AI summarization (Azure AI Foundry)
- `INotificationService` - Send to Teams/Email

### Application Layer Patterns

Uses **MediatR** for CQRS. Features organized in `Features/` folder:
- `GenerateStandup/` - GenerateStandupCommand/Handler
- `ConfigureRepository/` - AddRepositoryCommand, ListRepositoriesQuery

## Tech Stack

- .NET 10
- MediatR for CQRS
- FluentValidation for validation
- xUnit + FluentAssertions for testing
- Azure Cosmos DB for persistence
- Azure AI Foundry (GPT-4o) for summarization
- Microsoft Graph for Teams/Email
- Bicep for infrastructure (in `/infra`)

## Environment Variables

| Variable | Description |
|----------|-------------|
| `AI_FOUNDRY_ENDPOINT` | Azure AI Foundry endpoint |
| `AI_FOUNDRY_DEPLOYMENT` | Model deployment name (default: gpt-4o) |
| `COSMOS_ENDPOINT` | Cosmos DB endpoint |

## Documentation

Full documentation is in `/wiki` folder (Azure DevOps wiki compatible).

## Amplifier CLI Tools

This project has access to amplifier AI-powered CLI tools for:
- Knowledge synthesis and analysis
- Content processing and transformation
- AI-powered code generation utilities

### Location
Amplifier tools: `/Users/james/Source/github.com.microsoft/amplifier`

### Usage Patterns
- Consult `amplifier-cli-architect` agent for tool organization guidance
- Follow progressive maturity model (scenarios/ -> ai_working/ -> amplifier/)
- Reference exemplar: `scenarios/blog_writer/`

### Philosophy
All amplifier tools follow:
- Ruthless simplicity (no unnecessary abstraction)
- "Bricks and studs" modular design
- Code for structure, AI for intelligence
- YAGNI (You Aren't Gonna Need It)

## Task Master AI Instructions
**Import Task Master's development workflow commands and guidelines, treat as if import is in the main CLAUDE.md file.**
@./.taskmaster/CLAUDE.md
