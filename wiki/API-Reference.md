# API Reference

The Standup API provides both MCP tools for AI agents and REST endpoints for applications.

## MCP Tools

### GenerateStandup

Generates a standup report for the current user.

```
Tool: GenerateStandup

Parameters:
- since (optional): Custom date range start (ISO 8601)
- until (optional): Custom date range end (ISO 8601)
- sendTo (optional): Notification channels (comma-separated)

Returns: JSON with summary and statistics
```

**Example Response:**

```json
{
  "id": "report-id",
  "summary": "Yesterday, I worked on...",
  "periodStart": "2024-12-07T00:00:00Z",
  "periodEnd": "2024-12-08T00:00:00Z",
  "generatedAt": "2024-12-08T08:00:00Z",
  "commitCount": 5,
  "pullRequestCount": 2,
  "workItemCount": 3,
  "sentTo": ["TeamsDirectMessage"]
}
```

### PreviewStandup

Generates a preview without sending notifications or saving.

```
Tool: PreviewStandup

Parameters:
- since (optional): Custom date range start
- until (optional): Custom date range end

Returns: Summary text only
```

### ListRepositories

Lists configured repositories for the current user.

```
Tool: ListRepositories

Returns: Array of repository configurations
```

### AddGitHubRepository

Adds a GitHub repository to track.

```
Tool: AddGitHubRepository

Parameters:
- organization: GitHub org or username
- repository: Repository name
- authorUsername: Your GitHub username
- displayName (optional): Friendly name
- defaultBranch (optional): Default "main"
- pat (optional): Personal Access Token
```

### AddAzureDevOpsRepository

Adds an Azure DevOps repository to track.

```
Tool: AddAzureDevOpsRepository

Parameters:
- organization: Azure DevOps org
- project: Project name
- repository: Repository name
- authorIdentifier: Your email/display name
- displayName (optional): Friendly name
- defaultBranch (optional): Default "main"
- pat (optional): Personal Access Token
```

## REST API Endpoints

### POST /api/standup/generate

Generate a standup report.

**Request:**

```json
{
  "userId": "user-id",
  "tenantId": "tenant-id",
  "since": "2024-12-07T00:00:00Z",
  "until": "2024-12-08T00:00:00Z",
  "sendTo": ["TeamsDirectMessage", "Email"],
  "saveReport": true
}
```

**Response:**

```json
{
  "id": "report-id",
  "summary": "...",
  "periodStart": "...",
  "periodEnd": "...",
  "generatedAt": "...",
  "commitCount": 5,
  "pullRequestCount": 2,
  "workItemCount": 3,
  "sentTo": ["TeamsDirectMessage", "Email"]
}
```

### GET /api/standup/repositories

List repositories for a user.

**Query Parameters:**
- `userId`: User ID

**Response:**

```json
[
  {
    "id": "repo-id",
    "sourceType": "GitHub",
    "organization": "org",
    "project": null,
    "repository": "repo",
    "displayName": "My Repo",
    "authorIdentifier": "username",
    "defaultBranch": "main",
    "isActive": true
  }
]
```

### POST /api/standup/repositories

Add a new repository.

**Request:**

```json
{
  "userId": "user-id",
  "repository": {
    "sourceType": "GitHub",
    "organization": "org",
    "repository": "repo",
    "authorIdentifier": "username",
    "personalAccessToken": "ghp_..."
  }
}
```

### GET /api/healthz

Health check endpoint.

**Response:** `Healthy`

## Authentication

All API endpoints require authentication via Azure Entra ID.

**Headers:**

```
Authorization: Bearer <access-token>
```

The access token should contain:
- `oid` claim (user object ID)
- `tid` claim (tenant ID)
