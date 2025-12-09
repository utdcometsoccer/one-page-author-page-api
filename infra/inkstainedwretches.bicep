// =========================================
// Ink Stained Wretches Infrastructure
// =========================================
// This Bicep template deploys all Azure resources needed for the
// Ink Stained Wretches platform including:
// - Storage Account
// - Key Vault
// - DNS Zone
// - Application Insights
// - Four Function Apps (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe, InkStainedWretchesConfig)
// - Azure Communication Services (optional, for email notifications)

@description('The base name for all resources (used to generate unique names)')
param baseName string

@description('The location for all resources')
param location string = resourceGroup().location

@description('Whether to deploy the Storage Account')
param deployStorageAccount bool = true

@description('Whether to deploy the Key Vault')
param deployKeyVault bool = true

@description('Whether to deploy the DNS Zone')
param deployDnsZone bool = true

@description('DNS Zone name (required if deployDnsZone is true)')
param dnsZoneName string = ''

@description('Whether to deploy Application Insights')
param deployAppInsights bool = true

@description('Whether to deploy Azure Communication Services for email')
param deployCommunicationServices bool = false

@description('Whether to deploy the ImageAPI Function App')
param deployImageApi bool = true

@description('Whether to deploy the InkStainedWretchFunctions Function App')
param deployInkStainedWretchFunctions bool = true

@description('Whether to deploy the InkStainedWretchStripe Function App')
param deployInkStainedWretchStripe bool = true

@description('Whether to deploy the InkStainedWretchesConfig Function App')
param deployInkStainedWretchesConfig bool = true

@description('Cosmos DB connection string (required for Function Apps)')
@secure()
param cosmosDbConnectionString string = ''

@description('Stripe API Key (required for InkStainedWretchStripe)')
@secure()
param stripeApiKey string = ''

@description('Azure AD Tenant ID')
param aadTenantId string = ''

@description('Azure AD Audience (Client ID)')
param aadAudience string = ''

// =========================================
// Variables
// =========================================

// Sanitize storage account name - remove all non-alphanumeric characters and convert to lowercase
var storageAccountNameRaw = toLower(replace(replace(baseName, '-', ''), '_', ''))
var storageAccountName = length(storageAccountNameRaw) > 24 ? substring(storageAccountNameRaw, 0, 24) : storageAccountNameRaw
var keyVaultName = toLower('${baseName}-kv')
var appInsightsName = '${baseName}-insights'
var imageApiFunctionName = '${baseName}-imageapi'
var inkStainedWretchFunctionsName = '${baseName}-functions'
var inkStainedWretchStripeName = '${baseName}-stripe'
var inkStainedWretchesConfigName = '${baseName}-config'
var appServicePlanName = '${baseName}-plan'

// Storage account connection string (used by all Function Apps)
var storageConnectionString = deployStorageAccount ? 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}' : ''

// Key Vault URI for configuration
var keyVaultUri = deployKeyVault ? keyVault.properties.vaultUri : ''

// =========================================
// Storage Account
// =========================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = if (deployStorageAccount) {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// =========================================
// Key Vault
// =========================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = if (deployKeyVault) {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: false
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// =========================================
// DNS Zone
// =========================================

resource dnsZone 'Microsoft.Network/dnszones@2023-07-01-preview' = if (deployDnsZone && !empty(dnsZoneName)) {
  name: dnsZoneName
  location: 'global'
  properties: {
    zoneType: 'Public'
  }
}

// =========================================
// Application Insights
// =========================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = if (deployAppInsights) {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'rest'
    RetentionInDays: 90
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// =========================================
// =========================================
// App Service Plan (Consumption)
// =========================================

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = if (deployImageApi || deployInkStainedWretchFunctions || deployInkStainedWretchStripe || deployInkStainedWretchesConfig) {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false
  }
}

// =========================================
// ImageAPI Function App
// =========================================

resource imageApiFunctionApp 'Microsoft.Web/sites@2024-04-01' = if (deployImageApi && deployStorageAccount) {
  name: imageApiFunctionName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(imageApiFunctionName)
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
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: deployAppInsights ? appInsights.properties.ConnectionString : ''
        }
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// =========================================
// InkStainedWretchFunctions Function App
// =========================================

resource inkStainedWretchFunctionsApp 'Microsoft.Web/sites@2024-04-01' = if (deployInkStainedWretchFunctions && deployStorageAccount) {
  name: inkStainedWretchFunctionsName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(inkStainedWretchFunctionsName)
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
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: deployAppInsights ? appInsights.properties.ConnectionString : ''
        }
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// =========================================
// InkStainedWretchStripe Function App
// =========================================

resource inkStainedWretchStripeApp 'Microsoft.Web/sites@2024-04-01' = if (deployInkStainedWretchStripe && deployStorageAccount) {
  name: inkStainedWretchStripeName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(inkStainedWretchStripeName)
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
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: deployAppInsights ? appInsights.properties.ConnectionString : ''
        }
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
        {
          name: 'STRIPE_API_KEY'
          value: stripeApiKey
        }
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// =========================================
// InkStainedWretchesConfig Function App
// =========================================

resource inkStainedWretchesConfigApp 'Microsoft.Web/sites@2024-04-01' = if (deployInkStainedWretchesConfig && deployStorageAccount) {
  name: inkStainedWretchesConfigName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(inkStainedWretchesConfigName)
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
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: deployAppInsights ? appInsights.properties.ConnectionString : ''
        }
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'true'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// =========================================
// Key Vault Role Assignments
// =========================================

// Grant ImageAPI access to Key Vault
resource imageApiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployKeyVault && deployImageApi && deployStorageAccount) {
  name: guid(keyVault.id, imageApiFunctionApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: imageApiFunctionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant InkStainedWretchFunctions access to Key Vault
resource functionsKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployKeyVault && deployInkStainedWretchFunctions && deployStorageAccount) {
  name: guid(keyVault.id, inkStainedWretchFunctionsApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: inkStainedWretchFunctionsApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant InkStainedWretchStripe access to Key Vault
resource stripeKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployKeyVault && deployInkStainedWretchStripe && deployStorageAccount) {
  name: guid(keyVault.id, inkStainedWretchStripeApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: inkStainedWretchStripeApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant InkStainedWretchesConfig access to Key Vault
resource configKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployKeyVault && deployInkStainedWretchesConfig && deployStorageAccount) {
  name: guid(keyVault.id, inkStainedWretchesConfigApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: inkStainedWretchesConfigApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// =========================================
// Outputs
// =========================================

output storageAccountName string = deployStorageAccount ? storageAccount.name : ''
output keyVaultName string = deployKeyVault ? keyVault.name : ''
output keyVaultUri string = deployKeyVault ? keyVaultUri : ''
output dnsZoneName string = deployDnsZone && !empty(dnsZoneName) ? dnsZone.name : ''
output appInsightsName string = deployAppInsights ? appInsights.name : ''
output appInsightsInstrumentationKey string = deployAppInsights ? appInsights.properties.InstrumentationKey : ''
output appInsightsConnectionString string = deployAppInsights ? appInsights.properties.ConnectionString : ''
output imageApiFunctionName string = deployImageApi && deployStorageAccount ? imageApiFunctionApp.name : ''
output imageApiFunctionUrl string = deployImageApi && deployStorageAccount ? 'https://${imageApiFunctionApp.properties.defaultHostName}' : ''
output inkStainedWretchFunctionsName string = deployInkStainedWretchFunctions && deployStorageAccount ? inkStainedWretchFunctionsApp.name : ''
output inkStainedWretchFunctionsUrl string = deployInkStainedWretchFunctions && deployStorageAccount ? 'https://${inkStainedWretchFunctionsApp.properties.defaultHostName}' : ''
output inkStainedWretchStripeName string = deployInkStainedWretchStripe && deployStorageAccount ? inkStainedWretchStripeApp.name : ''
output inkStainedWretchStripeUrl string = deployInkStainedWretchStripe && deployStorageAccount ? 'https://${inkStainedWretchStripeApp.properties.defaultHostName}' : ''
output inkStainedWretchesConfigName string = deployInkStainedWretchesConfig && deployStorageAccount ? inkStainedWretchesConfigApp.name : ''
output inkStainedWretchesConfigUrl string = deployInkStainedWretchesConfig && deployStorageAccount ? 'https://${inkStainedWretchesConfigApp.properties.defaultHostName}' : ''
output communicationServicesDeployed string = deployCommunicationServices ? 'true' : 'false'
output communicationServicesNote string = deployCommunicationServices ? 'Communication Services deployed. Retrieve connection string from Azure Portal -> Communication Services -> Keys' : 'Communication Services not deployed'

// =========================================
// Azure Communication Services (Optional)
// =========================================

module communicationServices 'communication-services.bicep' = if (deployCommunicationServices) {
  name: 'communication-services-deployment'
  params: {
    baseName: baseName
    dataLocation: 'United States'
    tags: {
      environment: 'production'
      project: 'OnePageAuthor'
      component: 'EmailService'
    }
  }
}
