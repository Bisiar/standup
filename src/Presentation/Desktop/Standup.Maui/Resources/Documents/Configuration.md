# Configuration

This guide covers all configuration options for the Standup Automation Platform.

## Application Settings

### API Configuration (`appsettings.json`)

> **Note:** The values below are examples. Replace with your actual Azure resource endpoints.

```json
{
  "AIFoundry": {
    "Endpoint": "https://your-ai-foundry.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "UseAzureIdentity": true
  },
  "Encryption": {
    "EncryptionKey": "your-encryption-key-here"
  },
  "CosmosDb": {
    "Endpoint": "https://your-cosmos.documents.azure.com:443/",
    "DatabaseName": "standup"
  }
}
```

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `AZURE_CLIENT_ID` | Managed Identity client ID | Yes (Azure) |
| `AI_FOUNDRY_ENDPOINT` | AI Foundry endpoint URL | Yes |
| `AI_FOUNDRY_DEPLOYMENT` | Model deployment name | Yes |
| `COSMOS_ENDPOINT` | Cosmos DB endpoint | Yes |

## Multi-Project Setup

The MAUI app supports multiple project instances for different organizations.

### Adding a Project

> **Example:** Project configuration structure (values are placeholders).

```json
{
  "Id": "guid",
  "Name": "My Organization",
  "TenantName": "My Organization",
  "ApiEndpoint": "https://your-standup-api.azurewebsites.net",
  "UserId": "your-entra-object-id",
  "TenantId": "your-entra-tenant-id",
  "IsDefault": true
}
```

### Switching Projects

1. Open the **Projects** page
2. Tap on the project you want to switch to
3. The app will use that project's configuration

## Repository Configuration

### GitHub Repository

> **Example:** Replace with your actual GitHub organization and repository.

```json
{
  "SourceType": "GitHub",
  "Organization": "your-org",
  "Repository": "your-repo",
  "AuthorIdentifier": "your-github-username",
  "DefaultBranch": "main",
  "PersonalAccessToken": "ghp_xxxxxxxxxxxx"
}
```

*Note: PersonalAccessToken is optional for public repositories.*

**Required GitHub PAT Permissions:**
- `repo` (for private repositories)
- `read:user`

### Azure DevOps Repository

> **Example:** Replace with your actual Azure DevOps organization and project.

```json
{
  "SourceType": "AzureDevOps",
  "Organization": "your-org",
  "Project": "your-project",
  "Repository": "your-repo",
  "AuthorIdentifier": "your.email@company.com",
  "DefaultBranch": "main",
  "PersonalAccessToken": "your-pat-token"
}
```

**Required Azure DevOps PAT Permissions:**
- Code: Read
- Work Items: Read
- Build: Read (optional)

## Scheduling

### Cron Expression Format

Standups can be scheduled using cron expressions:

```
┌───────────── minute (0 - 59)
│ ┌───────────── hour (0 - 23)
│ │ ┌───────────── day of month (1 - 31)
│ │ │ ┌───────────── month (1 - 12)
│ │ │ │ ┌───────────── day of week (0 - 6)
│ │ │ │ │
* * * * *
```

**Examples:**

| Expression | Description |
|------------|-------------|
| `0 8 * * 1-5` | 8:00 AM Monday-Friday |
| `30 9 * * 1-5` | 9:30 AM Monday-Friday |
| `0 8 * * *` | 8:00 AM every day |

### Schedule Configuration

```json
{
  "Name": "Morning Standup",
  "CronExpression": "0 8 * * 1-5",
  "TimeZone": "America/Denver",
  "NotifyMinutesBefore": 15,
  "IsAsync": false
}
```

## AI Customization

### Custom System Prompts

You can customize the AI summarization:

```json
{
  "CustomPrompt": "Focus on customer-facing features and bug fixes",
  "Tone": "Professional",
  "IncludeBlockers": true,
  "IncludeNextSteps": true,
  "MaxLength": 500
}
```

### Available Tones

- `Professional` - Formal, suitable for business meetings
- `Casual` - Friendly, conversational
- `Brief` - Bullet points only

## Security

### Token Encryption

All personal access tokens are encrypted using AES-256 before storage.

### Managed Identity

The API uses Azure Managed Identity for:
- Azure AI Foundry authentication
- Microsoft Graph access
- Cosmos DB access

No secrets are stored in configuration files when deployed to Azure.
