param location string = 'eastus'
param appServicePlanName string = 'b2capprole-plan'
param webAppName string = 'b2capprole'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'D1' // Shared SKU
    tier: 'Shared'
  }
  kind: 'app'
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
  }
  dependsOn: [
    appServicePlan
  ]
}

// Flex Consumption plan for Azure Function
resource functionPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'B2CappRole-flex-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
}

// Azure Function App
resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: 'func-b2c-dev-eu-01'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: functionPlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
      ]
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
  dependsOn: [
    functionPlan
  ]
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'kv-entra-dev-eastus-01'
  location: 'eastus'
  tags: {
    Environment: 'Dev'
  }
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: []
    enableSoftDelete: true
    publicNetworkAccess: 'Enabled'
  }
}

// Assign Key Vault Secrets User role to the web app's managed identity on the Key Vault
resource keyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(subscription().id, keyVault.id, webApp.name)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
  dependsOn: [
    webApp // Keep only necessary dependencies
  ]
}