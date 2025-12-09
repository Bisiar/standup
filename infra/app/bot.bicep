@description('Name of the Bot Service')
param botName string

@description('Location for the resources')
param location string = resourceGroup().location

@description('Tags for the resources')
param tags object = {}

@description('App ID for the bot (from Entra ID app registration)')
param msaAppId string

@description('App Type')
@allowed(['MultiTenant', 'SingleTenant', 'UserAssignedMSI'])
param msaAppType string = 'SingleTenant'

@description('Tenant ID for single tenant bots')
param msaAppTenantId string = ''

@description('Managed Identity Resource ID (for UserAssignedMSI)')
param managedIdentityResourceId string = ''

@description('Messaging endpoint URL')
param messagingEndpoint string

resource bot 'Microsoft.BotService/botServices@2022-09-15' = {
  name: botName
  location: 'global'
  tags: tags
  kind: 'azurebot'
  sku: {
    name: 'S1'
  }
  properties: {
    displayName: 'Standup Bot'
    description: 'Standup Automation Bot for Teams'
    endpoint: messagingEndpoint
    msaAppId: msaAppId
    msaAppType: msaAppType
    msaAppTenantId: msaAppType == 'SingleTenant' ? msaAppTenantId : null
    msaAppMSIResourceId: msaAppType == 'UserAssignedMSI' ? managedIdentityResourceId : null
    developerAppInsightsApplicationId: ''
    luisAppIds: []
  }
}

// Teams channel
resource teamsChannel 'Microsoft.BotService/botServices/channels@2022-09-15' = {
  parent: bot
  name: 'MsTeamsChannel'
  location: 'global'
  properties: {
    channelName: 'MsTeamsChannel'
    properties: {
      enableCalling: false
      isEnabled: true
    }
  }
}

output botId string = bot.id
output botName string = bot.name
