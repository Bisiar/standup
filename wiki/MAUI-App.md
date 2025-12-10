# MAUI App Guide

The Standup MAUI app provides a cross-platform desktop and mobile interface for managing your standup automation.

## Features

- **Multi-Project Support**: Switch between different organizations/projects
- **Repository Management**: Configure GitHub and Azure DevOps repos
- **Standup Generation**: One-click standup reports
- **Documentation**: Built-in help documentation
- **Offline Support**: View cached reports offline

## Installation

### Windows

1. Download `Standup.msix` from releases
2. Double-click to install
3. Launch from Start menu

### macOS

1. Download `Standup.app.zip` from releases
2. Extract and move to Applications
3. Right-click → Open (first time)

### CLI Tool

```bash
dotnet tool install -g Standup.Cli
standup
```

## Navigation

The app uses a flyout menu with these sections:

| Page | Purpose |
|------|---------|
| **Projects** | Manage project instances |
| **Standup** | Generate and view reports |
| **Repositories** | Configure source repos |
| **Documents** | View documentation |
| **Settings** | User configuration |

## Projects Page

### Adding a New Project

1. Click **+ Add Project**
2. Fill in:
   - **Project Name**: Display name (e.g., "JourneyTeam")
   - **Tenant Name**: Organization name
   - **API Endpoint**: Your deployed API URL
3. Click **Add**

### Switching Projects

- Tap on a project to make it active
- The current project is shown in the navigation header

### Default Project

- Mark a project as default to auto-select on app launch
- Toggle via the project settings

## Standup Page

### Generating a Report

1. Ensure you have a project selected
2. Click **Generate**
3. Wait for AI summarization
4. View your report

### Report Contents

The report includes:
- **Summary**: AI-generated natural language summary
- **Stats**: Commit count, PR count, work item count
- **Period**: Date range covered

### Actions

- **Copy**: Copy summary to clipboard
- **Share**: Share via system share sheet (mobile)

## Repositories Page

### Adding a Repository

1. Click **+ Add Repository**
2. Select source type:
   - **GitHub**: Organization + Repository
   - **Azure DevOps**: Organization + Project + Repository
3. Enter your author identifier
4. Add PAT if accessing private repos
5. Click **Add**

### Repository Status

- **Active**: Being tracked for standups
- **Inactive**: Disabled but configuration saved

## Settings Page

Configure your user credentials:

- **API Endpoint**: URL of your Standup API
- **User ID**: Your Entra ID object ID
- **Tenant ID**: Your organization's tenant ID

## Documents Page

Access built-in documentation:

- Getting Started guide
- Configuration help
- API reference
- Troubleshooting

Documentation is synced from the project wiki at build time.

## Keyboard Shortcuts (Desktop)

| Shortcut | Action |
|----------|--------|
| `Ctrl+G` | Generate standup |
| `Ctrl+C` | Copy to clipboard |
| `Ctrl+1-5` | Navigate to page |
| `Ctrl+P` | Open projects |

## Troubleshooting

### "User not authenticated"

- Check your User ID in Settings
- Ensure you've deployed the API with proper auth

### "Failed to generate"

- Verify API endpoint is correct
- Check repository configurations
- Ensure PATs haven't expired

### Reports are empty

- Verify author identifier matches your commits
- Check date range (default: last working day)
- Ensure repositories are marked as Active
