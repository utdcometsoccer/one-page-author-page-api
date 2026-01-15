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

// =========================================
// Required Secure Parameters
// =========================================

@description('Cosmos DB connection string (required for Function Apps)')
@secure()
param cosmosDbConnectionString string = ''

@description('Cosmos DB Endpoint URI (optional, extracted from connection string if not provided)')
param cosmosDbEndpointUri string = ''

@description('Cosmos DB Primary Key (optional, extracted from connection string if not provided)')
@secure()
param cosmosDbPrimaryKey string = ''

@description('Cosmos DB Database ID')
param cosmosDbDatabaseId string = 'OnePageAuthorDb'

@description('Azure Storage Connection String (required for ImageAPI)')
@secure()
param azureStorageConnectionString string = ''

@description('Stripe API Key (required for InkStainedWretchStripe)')
@secure()
param stripeApiKey string = ''

@description('Stripe Webhook Secret (required for InkStainedWretchStripe webhooks)')
@secure()
param stripeWebhookSecret string = ''

// =========================================
// Azure AD Authentication (Optional)
// =========================================

@description('Azure AD Tenant ID (optional)')
param aadTenantId string = ''

@description('Azure AD Audience/Client ID (optional)')
param aadAudience string = ''

@description('Azure AD Client ID (optional, for apps that need both audience and client ID)')
param aadClientId string = ''

@description('Azure AD Authority URL (optional)')
param aadAuthority string = ''

@description('Azure AD Valid Issuers (optional, comma-separated list of issuer URLs)')
param aadValidIssuers string = ''

@description('Entra (CIAM) policy name (optional), for example B2C_1_signup_signin')
param entraPolicy string = ''

// =========================================
// Azure Infrastructure (Optional - for Domain Management)
// =========================================

@description('Azure Subscription ID (optional, for domain management features)')
param azureSubscriptionId string = ''

@description('Azure DNS Resource Group (optional, for DNS zone creation)')
param azureDnsResourceGroup string = ''

// =========================================
// Google Domains Integration (Optional)
// =========================================

@description('Google Cloud Project ID (optional, for Google Domains registration)')
param googleCloudProjectId string = ''

@description('Google Domains Location (optional, default: global)')
param googleDomainsLocation string = ''

// =========================================
// Amazon Product Advertising API (Optional)
// =========================================

@description('Amazon Product Access Key (optional, for Amazon API integration)')
@secure()
param amazonProductAccessKey string = ''

@description('Amazon Product Secret Key (optional, for Amazon API integration)')
@secure()
param amazonProductSecretKey string = ''

@description('Amazon Product Partner Tag (optional, for Amazon API integration)')
param amazonProductPartnerTag string = ''

@description('Amazon Product Region (optional, default: us-east-1)')
param amazonProductRegion string = ''

@description('Amazon Product Marketplace (optional, default: www.amazon.com)')
param amazonProductMarketplace string = ''

// =========================================
// Penguin Random House API (Optional)
// =========================================

@description('Penguin Random House API Key (optional)')
@secure()
param penguinRandomHouseApiKey string = ''

@description('Penguin Random House API Domain (optional, default: PRH.US)')
param penguinRandomHouseApiDomain string = ''

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
var storageConnectionString = deployStorageAccount ? 'DefaultEndpointsProtocol=https;AccountName=${storageAccount!.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount!.listKeys().keys[0].value}' : ''

// Key Vault URI for configuration
var keyVaultUri = deployKeyVault ? keyVault!.properties.vaultUri : ''

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
      appSettings: concat([
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
      ],
      // Application Insights (optional)
      !empty(deployAppInsights ? appInsights!.properties.ConnectionString : '') ? [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights!.properties.ConnectionString
        }
      ] : [],
      // Cosmos DB Configuration
      !empty(cosmosDbConnectionString) ? [
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
      ] : [],
      !empty(cosmosDbEndpointUri) ? [
        {
          name: 'COSMOSDB_ENDPOINT_URI'
          value: cosmosDbEndpointUri
        }
      ] : [],
      !empty(cosmosDbPrimaryKey) ? [
        {
          name: 'COSMOSDB_PRIMARY_KEY'
          value: cosmosDbPrimaryKey
        }
      ] : [],
      !empty(cosmosDbDatabaseId) ? [
        {
          name: 'COSMOSDB_DATABASE_ID'
          value: cosmosDbDatabaseId
        }
      ] : [],
      // Azure Storage for Images
      !empty(azureStorageConnectionString) ? [
        {
          name: 'AZURE_STORAGE_CONNECTION_STRING'
          value: azureStorageConnectionString
        }
      ] : [],
      // Azure AD Authentication (optional)
      !empty(aadTenantId) ? [
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
      ] : [],
      !empty(aadAudience) ? [
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
      ] : [],
      !empty(aadClientId) ? [
        {
          name: 'AAD_CLIENT_ID'
          value: aadClientId
        }
      ] : [],
      !empty(aadAuthority) ? [
        {
          name: 'AAD_AUTHORITY'
          value: aadAuthority
        }
      ] : [],
      !empty(aadValidIssuers) ? [
        {
          name: 'AAD_VALID_ISSUERS'
          value: aadValidIssuers
        }
      ] : [],
      !empty(entraPolicy) ? [
        {
          name: 'ENTRA_POLICY'
          value: entraPolicy
        }
      ] : [],
      // Key Vault (optional)
      !empty(keyVaultUri) ? [
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ] : []
      )
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
      appSettings: concat([
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
      ],
      // Application Insights (optional)
      !empty(deployAppInsights ? appInsights!.properties.ConnectionString : '') ? [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights!.properties.ConnectionString
        }
      ] : [],
      // Cosmos DB Configuration
      !empty(cosmosDbConnectionString) ? [
        {
          name: 'CosmosDBConnection'
          value: cosmosDbConnectionString
        }
      ] : [],
      !empty(cosmosDbEndpointUri) ? [
        {
          name: 'COSMOSDB_ENDPOINT_URI'
          value: cosmosDbEndpointUri
        }
      ] : [],
      !empty(cosmosDbPrimaryKey) ? [
        {
          name: 'COSMOSDB_PRIMARY_KEY'
          value: cosmosDbPrimaryKey
        }
      ] : [],
      !empty(cosmosDbDatabaseId) ? [
        {
          name: 'COSMOSDB_DATABASE_ID'
          value: cosmosDbDatabaseId
        }
      ] : [],
      // Azure AD Authentication (optional)
      !empty(aadTenantId) ? [
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
      ] : [],
      !empty(aadAudience) ? [
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
      ] : [],
      !empty(aadClientId) ? [
        {
          name: 'AAD_CLIENT_ID'
          value: aadClientId
        }
      ] : [],
      !empty(aadAuthority) ? [
        {
          name: 'AAD_AUTHORITY'
          value: aadAuthority
        }
      ] : [],
      !empty(aadValidIssuers) ? [
        {
          name: 'AAD_VALID_ISSUERS'
          value: aadValidIssuers
        }
      ] : [],
      // Azure Infrastructure (optional - for domain management)
      !empty(azureSubscriptionId) ? [
        {
          name: 'AZURE_SUBSCRIPTION_ID'
          value: azureSubscriptionId
        }
      ] : [],
      !empty(azureDnsResourceGroup) ? [
        {
          name: 'AZURE_DNS_RESOURCE_GROUP'
          value: azureDnsResourceGroup
        }
      ] : [],
      // Google Domains Integration (optional)
      !empty(googleCloudProjectId) ? [
        {
          name: 'GOOGLE_CLOUD_PROJECT_ID'
          value: googleCloudProjectId
        }
      ] : [],
      !empty(googleDomainsLocation) ? [
        {
          name: 'GOOGLE_DOMAINS_LOCATION'
          value: googleDomainsLocation
        }
      ] : !empty(googleCloudProjectId) ? [
        {
          name: 'GOOGLE_DOMAINS_LOCATION'
          value: 'global'
        }
      ] : [],
      // Amazon Product Advertising API (optional)
      !empty(amazonProductAccessKey) ? [
        {
          name: 'AMAZON_PRODUCT_ACCESS_KEY'
          value: amazonProductAccessKey
        }
      ] : [],
      !empty(amazonProductSecretKey) ? [
        {
          name: 'AMAZON_PRODUCT_SECRET_KEY'
          value: amazonProductSecretKey
        }
      ] : [],
      !empty(amazonProductPartnerTag) ? [
        {
          name: 'AMAZON_PRODUCT_PARTNER_TAG'
          value: amazonProductPartnerTag
        }
      ] : [],
      !empty(amazonProductRegion) ? [
        {
          name: 'AMAZON_PRODUCT_REGION'
          value: amazonProductRegion
        }
      ] : !empty(amazonProductAccessKey) ? [
        {
          name: 'AMAZON_PRODUCT_REGION'
          value: 'us-east-1'
        }
      ] : [],
      !empty(amazonProductMarketplace) ? [
        {
          name: 'AMAZON_PRODUCT_MARKETPLACE'
          value: amazonProductMarketplace
        }
      ] : !empty(amazonProductAccessKey) ? [
        {
          name: 'AMAZON_PRODUCT_MARKETPLACE'
          value: 'www.amazon.com'
        }
      ] : [],
      // Penguin Random House API (optional)
      !empty(penguinRandomHouseApiKey) ? [
        {
          name: 'PENGUIN_RANDOM_HOUSE_API_KEY'
          value: penguinRandomHouseApiKey
        }
      ] : [],
      !empty(penguinRandomHouseApiDomain) ? [
        {
          name: 'PENGUIN_RANDOM_HOUSE_API_DOMAIN'
          value: penguinRandomHouseApiDomain
        }
      ] : !empty(penguinRandomHouseApiKey) ? [
        {
          name: 'PENGUIN_RANDOM_HOUSE_API_DOMAIN'
          value: 'PRH.US'
        }
      ] : [],
      // Key Vault (optional)
      !empty(keyVaultUri) ? [
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ] : []
      )
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
      appSettings: concat([
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
      ],
      // Application Insights (optional)
      !empty(deployAppInsights ? appInsights!.properties.ConnectionString : '') ? [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights!.properties.ConnectionString
        }
      ] : [],
      // Cosmos DB Configuration
      !empty(cosmosDbConnectionString) ? [
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
      ] : [],
      !empty(cosmosDbEndpointUri) ? [
        {
          name: 'COSMOSDB_ENDPOINT_URI'
          value: cosmosDbEndpointUri
        }
      ] : [],
      !empty(cosmosDbPrimaryKey) ? [
        {
          name: 'COSMOSDB_PRIMARY_KEY'
          value: cosmosDbPrimaryKey
        }
      ] : [],
      !empty(cosmosDbDatabaseId) ? [
        {
          name: 'COSMOSDB_DATABASE_ID'
          value: cosmosDbDatabaseId
        }
      ] : [],
      // Stripe Configuration
      !empty(stripeApiKey) ? [
        {
          name: 'STRIPE_API_KEY'
          value: stripeApiKey
        }
      ] : [],
      !empty(stripeWebhookSecret) ? [
        {
          name: 'STRIPE_WEBHOOK_SECRET'
          value: stripeWebhookSecret
        }
      ] : [],
      // Azure AD Authentication (optional)
      !empty(aadTenantId) ? [
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
      ] : [],
      !empty(aadAudience) ? [
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
      ] : [],
      !empty(aadClientId) ? [
        {
          name: 'AAD_CLIENT_ID'
          value: aadClientId
        }
      ] : [],
      !empty(aadAuthority) ? [
        {
          name: 'AAD_AUTHORITY'
          value: aadAuthority
        }
      ] : [],
      !empty(aadValidIssuers) ? [
        {
          name: 'AAD_VALID_ISSUERS'
          value: aadValidIssuers
        }
      ] : [],
      // Key Vault (optional)
      !empty(keyVaultUri) ? [
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ] : []
      )
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
      appSettings: concat([
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
      ],
      // Application Insights (optional)
      !empty(deployAppInsights ? appInsights!.properties.ConnectionString : '') ? [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights!.properties.ConnectionString
        }
      ] : [],
      // Cosmos DB Configuration
      !empty(cosmosDbConnectionString) ? [
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: cosmosDbConnectionString
        }
      ] : [],
      !empty(cosmosDbEndpointUri) ? [
        {
          name: 'COSMOSDB_ENDPOINT_URI'
          value: cosmosDbEndpointUri
        }
      ] : [],
      !empty(cosmosDbPrimaryKey) ? [
        {
          name: 'COSMOSDB_PRIMARY_KEY'
          value: cosmosDbPrimaryKey
        }
      ] : [],
      !empty(cosmosDbDatabaseId) ? [
        {
          name: 'COSMOSDB_DATABASE_ID'
          value: cosmosDbDatabaseId
        }
      ] : [],
      // Azure AD Authentication (optional)
      !empty(aadTenantId) ? [
        {
          name: 'AAD_TENANT_ID'
          value: aadTenantId
        }
      ] : [],
      !empty(aadAudience) ? [
        {
          name: 'AAD_AUDIENCE'
          value: aadAudience
        }
      ] : [],
      !empty(aadClientId) ? [
        {
          name: 'AAD_CLIENT_ID'
          value: aadClientId
        }
      ] : [],
      !empty(aadAuthority) ? [
        {
          name: 'AAD_AUTHORITY'
          value: aadAuthority
        }
      ] : [],
      !empty(aadValidIssuers) ? [
        {
          name: 'AAD_VALID_ISSUERS'
          value: aadValidIssuers
        }
      ] : [],
      // Key Vault (optional)
      !empty(keyVaultUri) ? [
        {
          name: 'KEY_VAULT_URL'
          value: keyVaultUri
        }
        {
          name: 'USE_KEY_VAULT'
          value: 'false'
        }
      ] : []
      )
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

// =========================================
// Key Vault Role Assignments
// =========================================
// NOTE: Role assignments have been removed from this template to avoid permission errors
// during automated deployments. The GitHub Actions service principal typically lacks the
// "User Access Administrator" role required to create role assignments.
//
// To grant Function Apps access to Key Vault after deployment, run the following commands
// or use the provided scripts in the infra/ directory:
//
// az role assignment create \
//   --assignee <function-app-principal-id> \
//   --role "Key Vault Secrets User" \
//   --scope <key-vault-id>
//
// Or use the helper script:
// ./infra/Assign-KeyVaultRole.sh -k <keyvault-name>
//
// For automated deployments, ensure the service principal has "User Access Administrator"
// role by running:
// ./infra/Grant-ServicePrincipalPermissions.sh

// =========================================
// Outputs
// =========================================

output storageAccountName string = deployStorageAccount ? storageAccount!.name : ''
output keyVaultName string = deployKeyVault ? keyVault!.name : ''
output keyVaultId string = deployKeyVault ? keyVault!.id : ''
output keyVaultUri string = deployKeyVault ? keyVaultUri : ''
output dnsZoneName string = deployDnsZone && !empty(dnsZoneName) ? dnsZone!.name : ''
output appInsightsName string = deployAppInsights ? appInsights!.name : ''
output appInsightsInstrumentationKey string = deployAppInsights ? appInsights!.properties.InstrumentationKey : ''
output appInsightsConnectionString string = deployAppInsights ? appInsights!.properties.ConnectionString : ''
output imageApiFunctionName string = deployImageApi && deployStorageAccount ? imageApiFunctionApp!.name : ''
output imageApiFunctionUrl string = deployImageApi && deployStorageAccount ? 'https://${imageApiFunctionApp!.properties.defaultHostName}' : ''
output imageApiFunctionPrincipalId string = deployImageApi && deployStorageAccount ? imageApiFunctionApp!.identity.principalId : ''
output inkStainedWretchFunctionsName string = deployInkStainedWretchFunctions && deployStorageAccount ? inkStainedWretchFunctionsApp!.name : ''
output inkStainedWretchFunctionsUrl string = deployInkStainedWretchFunctions && deployStorageAccount ? 'https://${inkStainedWretchFunctionsApp!.properties.defaultHostName}' : ''
output inkStainedWretchFunctionsPrincipalId string = deployInkStainedWretchFunctions && deployStorageAccount ? inkStainedWretchFunctionsApp!.identity.principalId : ''
output inkStainedWretchStripeName string = deployInkStainedWretchStripe && deployStorageAccount ? inkStainedWretchStripeApp!.name : ''
output inkStainedWretchStripeUrl string = deployInkStainedWretchStripe && deployStorageAccount ? 'https://${inkStainedWretchStripeApp!.properties.defaultHostName}' : ''
output inkStainedWretchStripePrincipalId string = deployInkStainedWretchStripe && deployStorageAccount ? inkStainedWretchStripeApp!.identity.principalId : ''
output inkStainedWretchesConfigName string = deployInkStainedWretchesConfig && deployStorageAccount ? inkStainedWretchesConfigApp!.name : ''
output inkStainedWretchesConfigUrl string = deployInkStainedWretchesConfig && deployStorageAccount ? 'https://${inkStainedWretchesConfigApp!.properties.defaultHostName}' : ''
output inkStainedWretchesConfigPrincipalId string = deployInkStainedWretchesConfig && deployStorageAccount ? inkStainedWretchesConfigApp!.identity.principalId : ''
output communicationServicesDeployed string = deployCommunicationServices ? 'true' : 'false'
output communicationServicesNote string = deployCommunicationServices ? 'Communication Services deployed. Retrieve connection string from Azure Portal -> Communication Services -> Keys' : 'Communication Services not deployed'
output postDeploymentNote string = deployKeyVault ? 'IMPORTANT: Role assignments for Key Vault access have been removed from this template. To grant Function Apps access to Key Vault, run: ./infra/Assign-KeyVaultRole.sh -k ${keyVault!.name} for each Function App, or grant the service principal User Access Administrator role using ./infra/Grant-ServicePrincipalPermissions.sh and redeploy.' : ''

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
