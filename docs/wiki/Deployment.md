# Deployment Guide

Deploy the Standup Automation Platform to Azure using the Azure Developer CLI (azd).

## Prerequisites

- Azure subscription
- Azure Developer CLI (`azd`) installed
- .NET 10 SDK
- Azure CLI (`az`) for additional configuration

## Quick Deploy

```bash
# Clone the repository
git clone https://github.com/your-org/standup.git
cd standup

# Login to Azure
azd auth login

# Deploy everything
azd up
```

## Infrastructure Components

The deployment creates:

| Resource | Purpose |
|----------|---------|
| Azure Functions (Flex) | API hosting |
| Azure Cosmos DB | Data storage |
| Azure AI Foundry | AI summarization |
| Application Insights | Monitoring |
| Key Vault | Secret storage |
| Virtual Network | Network isolation |
| Managed Identity | Secure auth |

## Configuration

### Environment Variables

Set these before deployment:

```bash
# Required
azd env set AI_FOUNDRY_ENDPOINT "https://your-ai.openai.azure.com/"
azd env set AI_FOUNDRY_DEPLOYMENT "gpt-4o"

# Optional
azd env set VNET_ENABLED "true"
azd env set PRE_AUTHORIZED_CLIENT_IDS "client-id-1,client-id-2"
```

### Parameters

Edit `infra/main.parameters.json`:

```json
{
  "parameters": {
    "environmentName": { "value": "standup-prod" },
    "location": { "value": "eastus" },
    "vnetEnabled": { "value": true },
    "delegatedPermissions": {
      "value": ["User.Read", "Mail.Send", "ChannelMessage.Send"]
    }
  }
}
```

## Step-by-Step Deployment

### 1. Provision Infrastructure

```bash
azd provision
```

This creates all Azure resources.

### 2. Deploy Applications

```bash
azd deploy
```

Deploys the API to Azure Functions.

### 3. Configure Authentication

After deployment, configure Entra ID:

1. Go to Azure Portal → Entra ID → App registrations
2. Find the created app (MCP Authorization App)
3. Add redirect URIs for your clients
4. Grant admin consent for permissions

### 4. Configure AI Foundry

1. Go to Azure AI Foundry
2. Create a deployment for `gpt-4o`
3. Note the endpoint URL
4. Update app settings

## Multi-Tenant Deployment

For multiple organizations:

### Option 1: Separate Deployments

Deploy to different subscriptions/resource groups:

```bash
# JourneyTeam
azd env new journeyteam
azd env set AZURE_SUBSCRIPTION_ID "sub-1"
azd up

# Digital Solutions
azd env new digitalsolutions
azd env set AZURE_SUBSCRIPTION_ID "sub-2"
azd up
```

### Option 2: Single Deployment, Multi-Tenant

Use Cosmos DB partitioning by tenant:

```json
{
  "partitionKey": "/tenantId"
}
```

## Monitoring

### Application Insights

View logs and metrics:

```bash
# Get connection string
az monitor app-insights component show \
  --app standup-insights \
  --resource-group standup-rg \
  --query connectionString
```

### Health Check

```bash
curl https://your-function.azurewebsites.net/api/healthz
# Returns: Healthy
```

## Scaling

### Azure Functions Flex Consumption

Automatically scales based on load:

- **Minimum instances**: 0 (scales to zero)
- **Maximum instances**: 100 (configurable)
- **Cold start**: ~2 seconds

### Cosmos DB

Configure throughput:

```bash
az cosmosdb sql container throughput update \
  --account-name standup-cosmos \
  --database-name standup \
  --name users \
  --throughput 400
```

## CI/CD Pipeline

### GitHub Actions

```yaml
name: Deploy Standup

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install azd
        uses: Azure/setup-azd@v1

      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy
        run: azd up --no-prompt
        env:
          AZURE_ENV_NAME: production
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: AzureCLI@2
    inputs:
      azureSubscription: 'Azure Connection'
      scriptType: 'bash'
      scriptLocation: 'inlineScript'
      inlineScript: |
        curl -fsSL https://aka.ms/install-azd.sh | bash
        azd up --no-prompt
```

## Updating

```bash
# Pull latest changes
git pull

# Update infrastructure and code
azd up
```

## Cleanup

```bash
# Remove all resources
azd down --force --purge
```
