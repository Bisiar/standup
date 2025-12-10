# Standup Automation Platform

Welcome to the Standup Automation Platform documentation. This platform automates the collection and summarization of your daily work activities for standup meetings.

## Overview

The Standup Automation Platform helps developers:
- **Automatically collect** commits, pull requests, and work items from GitHub and Azure DevOps
- **Summarize activities** using AI (Azure AI Foundry)
- **Deliver reports** via Microsoft Teams and Email
- **Support multiple projects** with different configurations

## Key Features

| Feature | Description |
|---------|-------------|
| Multi-Source | Support for both GitHub and Azure DevOps repositories |
| Multi-Tenant | Configure different projects for different organizations |
| AI Summarization | Natural language summaries using Azure OpenAI |
| Teams Integration | Direct messages, channel posts, and a Teams app |
| MCP Server | AI agents can generate standups via MCP tools |
| MAUI App | Desktop/mobile app with multi-instance support |

## Quick Links

- [Getting Started](Getting-Started)
- [Architecture Overview](Architecture)
- [Configuration Guide](Configuration)
- [API Reference](API-Reference)
- [MAUI App Guide](MAUI-App)
- [Teams Integration](Teams-Integration)
- [Deployment Guide](Deployment)
- [Troubleshooting](Troubleshooting)

## Project Structure

```
Standup/
├── src/
│   ├── Standup.Domain/        # Core entities and interfaces
│   ├── Standup.Application/   # Use cases and services
│   ├── Standup.Infrastructure/# External integrations
│   ├── Standup.Api/          # MCP Server and REST API
│   ├── Standup.Maui/         # Desktop/mobile app
│   └── Standup.Teams/        # Teams bot and app
├── wiki/                     # This documentation
└── infra/                    # Azure Bicep templates
```

## Support

For issues and feature requests, please contact the development team or create an issue in the repository.
