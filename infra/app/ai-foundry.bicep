@description('Name of the Azure OpenAI resource')
param accountName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Tags for the resources')
param tags object = {}

@description('Managed Identity Principal ID for RBAC')
param managedIdentityPrincipalId string

@description('User Identity Principal ID for RBAC (for local development/testing)')
param userIdentityPrincipalId string = ''

@description('Flag to enable user identity role assignments')
param allowUserIdentityPrincipal bool = false

@description('Model deployment name')
param deploymentName string = 'gpt-4o'

@description('Model name to deploy')
param modelName string = 'gpt-4o'

@description('Model version')
param modelVersion string = '2024-08-06'

@description('Capacity for the deployment (TPM in thousands)')
param capacity int = 10

resource openAIAccount 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: accountName
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAIAccount
  name: deploymentName
  sku: {
    name: 'Standard'
    capacity: capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// RBAC: Cognitive Services OpenAI User for Managed Identity
resource openAIUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAIAccount.id, managedIdentityPrincipalId, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Cognitive Services OpenAI User for User Identity (local development/testing)
resource openAIUser_User 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (allowUserIdentityPrincipal && !empty(userIdentityPrincipalId)) {
  name: guid(openAIAccount.id, userIdentityPrincipalId, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: userIdentityPrincipalId
    principalType: 'User'
  }
}

output endpoint string = openAIAccount.properties.endpoint
output accountName string = openAIAccount.name
output deploymentName string = deployment.name
