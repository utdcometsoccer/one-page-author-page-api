// =========================================
// Function App Settings Module
// =========================================
// This module provides a standardized way to generate app settings
// for Azure Function Apps with conditional optional parameters.
//
// Usage:
//   module appSettings 'modules/functionAppSettings.bicep' = {
//     name: 'myFunctionAppSettings'
//     params: {
//       functionAppName: 'my-function-app'
//       storageConnectionString: storageAccount.properties.primaryEndpoints.blob
//       // ... other parameters
//     }
//   }

@description('Function App name (used for WEBSITE_CONTENTSHARE)')
param functionAppName string

@description('Storage account connection string for AzureWebJobsStorage')
param storageConnectionString string

@description('Application Insights connection string (optional)')
param appInsightsConnectionString string = ''

// =========================================
// Cosmos DB Parameters (Optional)
// =========================================

@description('Cosmos DB connection string (optional)')
@secure()
param cosmosDbConnectionString string = ''

@description('Cosmos DB endpoint URI (optional)')
param cosmosDbEndpointUri string = ''

@description('Cosmos DB primary key (optional)')
@secure()
param cosmosDbPrimaryKey string = ''

@description('Cosmos DB database ID (optional)')
param cosmosDbDatabaseId string = ''

// =========================================
// Azure AD Authentication Parameters (Optional)
// =========================================

@description('Azure AD tenant ID (optional)')
param aadTenantId string = ''

@description('Azure AD audience/client ID (optional)')
param aadAudience string = ''

@description('Azure AD client ID (optional)')
param aadClientId string = ''

@description('Azure AD authority URL (optional)')
param aadAuthority string = ''

@description('Azure AD valid issuers (optional, comma-separated)')
param aadValidIssuers string = ''

@description('Entra (CIAM) policy name (optional), for example B2C_1_signup_signin')
param entraPolicy string = ''

// =========================================
// Azure Storage Parameters (Optional)
// =========================================

@description('Azure Storage connection string (optional, for ImageAPI)')
@secure()
param azureStorageConnectionString string = ''

// =========================================
// Stripe Parameters (Optional)
// =========================================

@description('Stripe API key (optional)')
@secure()
param stripeApiKey string = ''

@description('Stripe webhook secret (optional)')
@secure()
param stripeWebhookSecret string = ''

// =========================================
// Azure Infrastructure Parameters (Optional)
// =========================================

@description('Azure subscription ID (optional)')
param azureSubscriptionId string = ''

@description('Azure DNS resource group (optional)')
param azureDnsResourceGroup string = ''

// =========================================
// Google Domains Parameters (Optional)
// =========================================

@description('Google Cloud project ID (optional)')
param googleCloudProjectId string = ''

@description('Google Domains location (optional)')
param googleDomainsLocation string = ''

// =========================================
// Amazon Product API Parameters (Optional)
// =========================================

@description('Amazon Product access key (optional)')
@secure()
param amazonProductAccessKey string = ''

@description('Amazon Product secret key (optional)')
@secure()
param amazonProductSecretKey string = ''

@description('Amazon Product partner tag (optional)')
param amazonProductPartnerTag string = ''

@description('Amazon Product region (optional)')
param amazonProductRegion string = ''

@description('Amazon Product marketplace (optional)')
param amazonProductMarketplace string = ''

// =========================================
// Penguin Random House API Parameters (Optional)
// =========================================

@description('Penguin Random House API key (optional)')
@secure()
param penguinRandomHouseApiKey string = ''

@description('Penguin Random House API domain (optional)')
param penguinRandomHouseApiDomain string = ''

// =========================================
// Key Vault Parameters (Optional)
// =========================================

@description('Key Vault URI (optional)')
param keyVaultUri string = ''

@description('Whether to use Key Vault (optional)')
param useKeyVault bool = false

// =========================================
// Function to Build App Settings Array
// =========================================

// Base settings (always required)
var baseSettings = [
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
    value: toLower(functionAppName)
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
]

// Application Insights (optional)
var appInsightsSettings = !empty(appInsightsConnectionString) ? [
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
] : []

// Cosmos DB Configuration (optional)
var cosmosDbSettings = concat(
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
  ] : []
)

// Azure AD Authentication (optional)
var aadSettings = concat(
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
  ] : []
)

// Azure Storage (optional, for ImageAPI)
var azureStorageSettings = !empty(azureStorageConnectionString) ? [
  {
    name: 'AZURE_STORAGE_CONNECTION_STRING'
    value: azureStorageConnectionString
  }
] : []

// Stripe Configuration (optional)
var stripeSettings = concat(
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
  ] : []
)

// Azure Infrastructure (optional, for domain management)
var azureInfraSettings = concat(
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
  ] : []
)

// Google Domains Integration (optional)
var googleDomainsSettings = concat(
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
  ] : (!empty(googleCloudProjectId) && empty(googleDomainsLocation)) ? [
    {
      name: 'GOOGLE_DOMAINS_LOCATION'
      value: 'global'
    }
  ] : []
)

// Amazon Product API (optional)
var amazonProductSettings = concat(
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
  ] : (!empty(amazonProductAccessKey) && empty(amazonProductRegion)) ? [
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
  ] : (!empty(amazonProductAccessKey) && empty(amazonProductMarketplace)) ? [
    {
      name: 'AMAZON_PRODUCT_MARKETPLACE'
      value: 'www.amazon.com'
    }
  ] : []
)

// Penguin Random House API (optional)
var penguinRandomHouseSettings = concat(
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
  ] : (!empty(penguinRandomHouseApiKey) && empty(penguinRandomHouseApiDomain)) ? [
    {
      name: 'PENGUIN_RANDOM_HOUSE_API_DOMAIN'
      value: 'PRH.US'
    }
  ] : []
)

// Key Vault (optional)
var keyVaultSettings = !empty(keyVaultUri) ? [
  {
    name: 'KEY_VAULT_URL'
    value: keyVaultUri
  }
  {
    name: 'USE_KEY_VAULT'
    value: string(useKeyVault)
  }
] : []

// Combine all settings
var allSettings = concat(
  baseSettings,
  appInsightsSettings,
  cosmosDbSettings,
  aadSettings,
  azureStorageSettings,
  stripeSettings,
  azureInfraSettings,
  googleDomainsSettings,
  amazonProductSettings,
  penguinRandomHouseSettings,
  keyVaultSettings
)

// =========================================
// Outputs
// =========================================

@description('Complete app settings array ready for use in Function App configuration')
output appSettings array = allSettings

@description('Count of configured settings')
output settingsCount int = length(allSettings)
