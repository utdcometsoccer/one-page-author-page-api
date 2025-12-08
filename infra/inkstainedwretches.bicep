// =========================================
// Ink Stained Wretches Infrastructure
// =========================================
// This Bicep template deploys all Azure resources needed for the
// Ink Stained Wretches platform including:
// - Storage Account
// - Key Vault
// - DNS Zone
// - Application Insights
// - Static Web App
// - Three Function Apps (ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe)

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

@description('Whether to deploy the Static Web App')
param deployStaticWebApp bool = true

@description('GitHub repository URL for Static Web App')
param staticWebAppRepoUrl string = ''

@description('GitHub branch for Static Web App')
param staticWebAppBranch string = 'main'

@description('Whether to deploy the ImageAPI Function App')
param deployImageApi bool = true

@description('Whether to deploy the InkStainedWretchFunctions Function App')
param deployInkStainedWretchFunctions bool = true

@description('Whether to deploy the InkStainedWretchStripe Function App')
param deployInkStainedWretchStripe bool = true

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

var storageAccountName = toLower(replace('${baseName}storage', '-', ''))
var keyVaultName = toLower('${baseName}-kv')
var appInsightsName = '${baseName}-insights'
var staticWebAppName = '${baseName}-webapp'
var imageApiFunctionName = '${baseName}-imageapi'
var inkStainedWretchFunctionsName = '${baseName}-functions'
var inkStainedWretchStripeName = '${baseName}-stripe'
var appServicePlanName = '${baseName}-plan'

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
// Static Web App
// =========================================

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = if (deployStaticWebApp && !empty(staticWebAppRepoUrl)) {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: staticWebAppRepoUrl
    branch: staticWebAppBranch
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// =========================================
// App Service Plan (Consumption)
// =========================================

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = if (deployImageApi || deployInkStainedWretchFunctions || deployInkStainedWretchStripe) {
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
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
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
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
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
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
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
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// =========================================
// Outputs
// =========================================

output storageAccountName string = deployStorageAccount ? storageAccount.name : ''
output keyVaultName string = deployKeyVault ? keyVault.name : ''
output dnsZoneName string = deployDnsZone && !empty(dnsZoneName) ? dnsZone.name : ''
output appInsightsName string = deployAppInsights ? appInsights.name : ''
output appInsightsInstrumentationKey string = deployAppInsights ? appInsights.properties.InstrumentationKey : ''
output appInsightsConnectionString string = deployAppInsights ? appInsights.properties.ConnectionString : ''
output staticWebAppName string = deployStaticWebApp && !empty(staticWebAppRepoUrl) ? staticWebApp.name : ''
output staticWebAppUrl string = deployStaticWebApp && !empty(staticWebAppRepoUrl) ? 'https://${staticWebApp.properties.defaultHostname}' : ''
output imageApiFunctionName string = deployImageApi && deployStorageAccount ? imageApiFunctionApp.name : ''
output imageApiFunctionUrl string = deployImageApi && deployStorageAccount ? 'https://${imageApiFunctionApp.properties.defaultHostName}' : ''
output inkStainedWretchFunctionsName string = deployInkStainedWretchFunctions && deployStorageAccount ? inkStainedWretchFunctionsApp.name : ''
output inkStainedWretchFunctionsUrl string = deployInkStainedWretchFunctions && deployStorageAccount ? 'https://${inkStainedWretchFunctionsApp.properties.defaultHostName}' : ''
output inkStainedWretchStripeName string = deployInkStainedWretchStripe && deployStorageAccount ? inkStainedWretchStripeApp.name : ''
output inkStainedWretchStripeUrl string = deployInkStainedWretchStripe && deployStorageAccount ? 'https://${inkStainedWretchStripeApp.properties.defaultHostName}' : ''
