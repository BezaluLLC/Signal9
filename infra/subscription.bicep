targetScope = 'subscription'

@description('Name of the resource group')
param resourceGroupName string = 'rg-signal9-${environmentName}'

@description('Location for all resources')
param location string = 'East US'

@description('Environment name (dev, staging, prod)')
param environmentName string = 'dev'

@description('Application name')
param appName string = 'signal9'

// Resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: {
    environment: environmentName
    application: appName
    'azd-env-name': environmentName
  }
}

// Deploy main resources
module main 'main.bicep' = {
  name: 'main-deployment'
  scope: resourceGroup
  params: {
    location: location
    environmentName: environmentName
    appName: appName
  }
}

// Outputs
output resourceGroupName string = resourceGroup.name
output location string = location
output environmentName string = environmentName
