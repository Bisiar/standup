# Getting Started

This guide will help you get up and running with the Standup Automation Platform.

## Prerequisites

- .NET 10 SDK
- Azure subscription (for AI Foundry and hosting)
- GitHub or Azure DevOps account
- Microsoft 365 account (for Teams integration)

## Installation

### Option 1: MAUI Desktop App

1. Download the latest release from the releases page
2. Install the application
3. Launch and configure your first project

### Option 2: CLI Tool

```bash
# Install as a global tool
dotnet tool install -g Standup.Cli

# Run
standup
```

### Option 3: Deploy API Server

```bash
# Clone the repository
git clone https://github.com/your-org/standup.git
cd standup

# Deploy to Azure
azd up
```

## Initial Configuration

### 1. Create a Project

When you first launch the app, you'll need to create a project:

1. Go to **Projects** in the navigation
2. Click **Add Project**
3. Enter:
   - **Project Name**: e.g., "JourneyTeam"
   - **Tenant Name**: Your organization name
   - **API Endpoint**: Your deployed API URL

### 2. Configure Settings

1. Go to **Settings**
2. Enter your **User ID** (from Entra ID)
3. Enter your **Tenant ID** (from Entra ID)
4. Save

### 3. Add Repositories

1. Go to **Repositories**
2. Click **Add Repository**
3. Choose **GitHub** or **Azure DevOps**
4. Enter repository details:
   - Organization
   - Project (for Azure DevOps)
   - Repository name
   - Your author identifier (username/email)
   - Personal Access Token (optional, for private repos)

## Your First Standup

Once configured:

1. Go to **Standup**
2. Click **Generate**
3. View your AI-generated standup summary
4. Click **Copy** to copy to clipboard

## Next Steps

- [Configure multiple projects](Configuration#multi-project-setup)
- [Set up Teams notifications](Teams-Integration)
- [Schedule automated standups](Configuration#scheduling)
