# Infrastructure Modules

This directory contains reusable Bicep modules for the OnePageAuthor API infrastructure.

## Modules

### functionAppSettings.bicep

A standardized module for generating Azure Function App settings with conditional optional parameters.

#### Purpose

- **Reduces code duplication** across four function apps
- **Ensures consistency** in configuration patterns
- **Simplifies maintenance** - update logic in one place
- **Enables testing** of configuration generation

#### Usage

```bicep
module functionAppSettings 'modules/functionAppSettings.bicep' = {
  name: 'myFunctionAppSettings'
  params: {
    // Required parameters
    functionAppName: 'my-function-app'
    storageConnectionString: storageAccount.listKeys().keys[0].value
    
    // Optional parameters (only include what you need)
    appInsightsConnectionString: appInsights.properties.ConnectionString
    
    // Cosmos DB
    cosmosDbConnectionString: cosmosDbConnectionString
    cosmosDbEndpointUri: cosmosDbEndpointUri
    cosmosDbPrimaryKey: cosmosDbPrimaryKey
    cosmosDbDatabaseId: 'OnePageAuthorDb'
    
    // Azure AD Authentication
    aadTenantId: aadTenantId
    aadAudience: aadAudience
    aadClientId: aadClientId
    aadAuthority: aadAuthority
    aadValidIssuers: aadValidIssuers
    
    // Additional optional parameters as needed...
  }
}

// Use module output in Function App
resource myFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: 'my-function-app'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: functionAppSettings.outputs.appSettings
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}
```

#### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `functionAppName` | string | Yes | Function App name (used for WEBSITE_CONTENTSHARE) |
| `storageConnectionString` | string | Yes | Storage account connection string |
| `appInsightsConnectionString` | string | No | Application Insights connection string |
| `cosmosDbConnectionString` | string (secure) | No | Cosmos DB connection string |
| `cosmosDbEndpointUri` | string | No | Cosmos DB endpoint URI |
| `cosmosDbPrimaryKey` | string (secure) | No | Cosmos DB primary key |
| `cosmosDbDatabaseId` | string | No | Cosmos DB database ID |
| `aadTenantId` | string | No | Azure AD tenant ID |
| `aadAudience` | string | No | Azure AD audience/client ID |
| `aadClientId` | string | No | Azure AD client ID |
| `aadAuthority` | string | No | Azure AD authority URL |
| `aadValidIssuers` | string | No | Azure AD valid issuers (comma-separated) |
| `azureStorageConnectionString` | string (secure) | No | Azure Storage connection string (for ImageAPI) |
| `stripeApiKey` | string (secure) | No | Stripe API key |
| `stripeWebhookSecret` | string (secure) | No | Stripe webhook secret |
| `azureSubscriptionId` | string | No | Azure subscription ID |
| `azureDnsResourceGroup` | string | No | Azure DNS resource group |
| `googleCloudProjectId` | string | No | Google Cloud project ID |
| `googleDomainsLocation` | string | No | Google Domains location |
| `amazonProductAccessKey` | string (secure) | No | Amazon Product access key |
| `amazonProductSecretKey` | string (secure) | No | Amazon Product secret key |
| `amazonProductPartnerTag` | string | No | Amazon Product partner tag |
| `amazonProductRegion` | string | No | Amazon Product region |
| `amazonProductMarketplace` | string | No | Amazon Product marketplace |
| `penguinRandomHouseApiKey` | string (secure) | No | Penguin Random House API key |
| `penguinRandomHouseApiDomain` | string | No | Penguin Random House API domain |
| `keyVaultUri` | string | No | Key Vault URI |
| `useKeyVault` | bool | No | Whether to use Key Vault |

#### Outputs

| Output | Type | Description |
|--------|------|-------------|
| `appSettings` | array | Complete app settings array |
| `settingsCount` | int | Number of configured settings |

#### Example: ImageAPI Function App

```bicep
module imageApiSettings 'modules/functionAppSettings.bicep' = {
  name: 'imageApiSettings'
  params: {
    functionAppName: '${baseName}-imageapi'
    storageConnectionString: storageConnectionString
    appInsightsConnectionString: deployAppInsights ? appInsights.properties.ConnectionString : ''
    cosmosDbConnectionString: cosmosDbConnectionString
    cosmosDbEndpointUri: cosmosDbEndpointUri
    cosmosDbPrimaryKey: cosmosDbPrimaryKey
    cosmosDbDatabaseId: cosmosDbDatabaseId
    aadTenantId: aadTenantId
    aadAudience: aadAudience
    aadClientId: aadClientId
    aadAuthority: aadAuthority
    aadValidIssuers: aadValidIssuers
    azureStorageConnectionString: azureStorageConnectionString
    keyVaultUri: keyVaultUri
    useKeyVault: false
  }
}

resource imageApiFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: '${baseName}-imageapi'
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: imageApiSettings.outputs.appSettings
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}
```

#### How It Works

1. **Base Settings**: Always includes required Azure Function settings
   - AzureWebJobsStorage
   - WEBSITE_CONTENTAZUREFILECONNECTIONSTRING
   - WEBSITE_CONTENTSHARE
   - FUNCTIONS_EXTENSION_VERSION
   - FUNCTIONS_WORKER_RUNTIME
   - WEBSITE_RUN_FROM_PACKAGE

2. **Conditional Settings**: Only adds optional settings if parameters are provided
   - Uses `!empty(parameter)` checks
   - Returns empty array if parameter not provided
   - Prevents empty string values in Function App

3. **Combines All**: Uses `concat()` to merge all setting arrays

#### Benefits Over Inline Configuration

**Before (Inline):**
```bicep
appSettings: concat([
  { name: 'AzureWebJobsStorage', value: storage }
  // ... 50+ lines of repeated configuration
],
!empty(aadAudience) ? [
  { name: 'AAD_AUDIENCE', value: aadAudience }
] : [])
// Repeated 4 times for each function app
```

**After (Module):**
```bicep
module settings 'modules/functionAppSettings.bicep' = {
  name: 'settings'
  params: {
    functionAppName: name
    storageConnectionString: storage
    aadAudience: aadAudience
    // ... only what's needed
  }
}

appSettings: settings.outputs.appSettings
// No duplication!
```

#### Testing

Validate the module:

```bash
# Validate syntax
az bicep build --file infra/modules/functionAppSettings.bicep

# Run tests
pwsh infra/Test-ConfigurationPropagation.ps1
```

## Future Modules

Potential future modules to add:

- **appServicePlan.bicep** - Standardized App Service Plan configuration
- **functionApp.bicep** - Complete Function App resource with identity and properties
- **keyVaultSecrets.bicep** - Key Vault secret management
- **roleAssignments.bicep** - RBAC role assignments

## Contributing

When creating new modules:

1. Follow Bicep best practices
2. Use clear parameter descriptions
3. Make parameters optional when possible
4. Provide usage examples
5. Add validation tests
6. Document in this README

## Related Documentation

- [CONFIGURATION_PATTERNS_REFERENCE.md](../docs/CONFIGURATION_PATTERNS_REFERENCE.md) - Configuration patterns guide
- [IMPLEMENTATION_SUMMARY_CONFIG_PROPAGATION_FIX.md](../docs/IMPLEMENTATION_SUMMARY_CONFIG_PROPAGATION_FIX.md) - Implementation details
- [GITHUB_SECRETS_CONFIGURATION.md](../docs/GITHUB_SECRETS_CONFIGURATION.md) - Secrets configuration

---

*Last Updated: January 11, 2026*
