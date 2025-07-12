@description('Location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environmentName string = 'dev'

@description('Application name')
param appName string = 'signal9'

@description('Resource token to make resource names unique')
param resourceToken string = uniqueString(subscription().id, resourceGroup().id)

// Variables
var tags = {
  environment: environmentName
  application: appName
  'azd-env-name': environmentName
}

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${appName}-identity-${resourceToken}'
  location: location
  tags: tags
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: '${appName}-kv-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: managedIdentity.properties.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-ai-${resourceToken}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// SignalR Service (configured for serverless mode)
resource signalRService 'Microsoft.SignalRService/signalR@2023-08-01-preview' = {
  name: '${appName}-signalr-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Free_F1'
    capacity: 1
  }
  properties: {
    tls: {
      clientCertEnabled: false
    }
    features: [
      {
        flag: 'ServiceMode'
        value: 'Serverless'  // Changed to Serverless mode for Functions
      }
    ]
    cors: {
      allowedOrigins: ['*']
    }
    networkACLs: {
      defaultAction: 'Allow'
    }
    upstream: {
      templates: [
        {
          urlTemplate: 'https://${appName}-agent-func-${resourceToken}.azurewebsites.net/api/{hub}/{category}/{event}'
          hubPattern: 'AgentHub'
          eventPattern: '*'
          categoryPattern: '*'
        }
        {
          urlTemplate: 'https://${appName}-web-func-${resourceToken}.azurewebsites.net/api/{hub}/{category}/{event}'
          hubPattern: 'DashboardHub'
          eventPattern: '*'
          categoryPattern: '*'
        }
      ]
    }
  }
}

@description('SQL Administrator password')
@secure()
param sqlAdminPassword string

// SQL Database Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: '${appName}-sql-${resourceToken}'
  location: location
  tags: tags
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: 'signal9db'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
  }
}

// SQL Firewall Rule (Allow Azure Services)
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Cosmos DB Account
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-09-15' = {
  name: '${appName}-cosmos-${resourceToken}'
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    disableKeyBasedMetadataWriteAccess: true
  }
}

// Cosmos Database
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-09-15' = {
  parent: cosmosAccount
  name: 'signal9'
  properties: {
    resource: {
      id: 'signal9'
    }
  }
}

// Cosmos Containers
resource telemetryContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
  parent: cosmosDatabase
  name: 'telemetry'
  properties: {
    resource: {
      id: 'telemetry'
      partitionKey: {
        paths: ['/agentId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
    }
  }
}

resource logsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
  parent: cosmosDatabase
  name: 'logs'
  properties: {
    resource: {
      id: 'logs'
      partitionKey: {
        paths: ['/agentId']
        kind: 'Hash'
      }
    }
  }
}

resource eventsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-09-15' = {
  parent: cosmosDatabase
  name: 'events'
  properties: {
    resource: {
      id: 'events'
      partitionKey: {
        paths: ['/agentId']
        kind: 'Hash'
      }
    }
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2023-01-01-preview' = {
  name: '${appName}-sb-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

// Service Bus Queue for commands
resource commandQueue 'Microsoft.ServiceBus/namespaces/queues@2023-01-01-preview' = {
  parent: serviceBusNamespace
  name: 'agent-commands'
  properties: {
    maxSizeInMegabytes: 1024
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
  }
}

// Service Bus Topic for telemetry
resource telemetryTopic 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = {
  parent: serviceBusNamespace
  name: 'telemetry'
  properties: {
    maxSizeInMegabytes: 1024
    defaultMessageTimeToLive: 'P14D'
  }
}

// Service Bus Topic for events
resource eventsTopic 'Microsoft.ServiceBus/namespaces/topics@2023-01-01-preview' = {
  parent: serviceBusNamespace
  name: 'events'
  properties: {
    maxSizeInMegabytes: 1024
    defaultMessageTimeToLive: 'P14D'
  }
}

// Function App Service Plan
resource functionAppServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${appName}-functions-plan-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

// Storage Account for Function Apps
resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${appName}funcst${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// Agent Function App (for agent communication)
resource agentFunctionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${appName}-agent-func-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'agent-functions' })
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: functionAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower('${appName}-agent-func-${resourceToken}')
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'AzureSignalRConnectionString'
          value: signalRService.listKeys().primaryConnectionString
        }
        {
          name: 'AzureConfiguration__KeyVaultUrl'
          value: keyVault.properties.vaultUri
        }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: ['*']
        supportCredentials: true
      }
    }
  }
}

// Web Function App (for dashboard and user management)
resource webFunctionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${appName}-web-func-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'web-functions' })
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: functionAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower('${appName}-web-func-${resourceToken}')
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'AzureSignalRConnectionString'
          value: signalRService.listKeys().primaryConnectionString
        }
        {
          name: 'AzureConfiguration__KeyVaultUrl'
          value: keyVault.properties.vaultUri
        }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: ['*']
        supportCredentials: true
      }
    }
  }
}

// Web App Service
resource webAppService 'Microsoft.Web/sites@2023-01-01' = {
  name: '${appName}-web-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: functionAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'AzureConfiguration__KeyVaultUrl'
          value: keyVault.properties.vaultUri
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${sqlDatabase.name};Authentication=Active Directory Managed Identity;'
        }
      ]
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      cors: {
        allowedOrigins: ['*']
        supportCredentials: true
      }
    }
  }
}

// Store secrets in Key Vault
resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${sqlDatabase.name};Authentication=Active Directory Managed Identity;'
  }
}

resource cosmosConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'CosmosConnectionString'
  properties: {
    value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource serviceBusConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'ServiceBusConnectionString'
  properties: {
    value: serviceBusNamespace.listKeys().primaryConnectionString
  }
}

// Outputs
output AZURE_KEY_VAULT_URL string = keyVault.properties.vaultUri
output AZURE_APPLICATION_INSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
output AGENT_FUNCTION_URL string = 'https://${agentFunctionApp.properties.defaultHostName}'
output WEB_FUNCTION_URL string = 'https://${webFunctionApp.properties.defaultHostName}'
output WEB_URL string = 'https://${webAppService.properties.defaultHostName}'
