// =========================================
// Cosmos DB Account Infrastructure
// =========================================
// This Bicep template deploys a Cosmos DB account for the OnePageAuthor platform.
// It uses serverless capacity mode suitable for development and moderate workloads.

@description('The name of the Cosmos DB account')
param cosmosDbAccountName string

@description('The location for the Cosmos DB account')
param location string = resourceGroup().location

@description('Whether to enable automatic failover')
param enableAutomaticFailover bool = true

@description('Whether to enable free tier (only one per subscription)')
param enableFreeTier bool = false

@description('Capacity mode: Serverless or Provisioned')
@allowed([
  'Serverless'
  'Provisioned'
])
param capacityMode string = 'Serverless'

@description('Minimum TLS version')
@allowed([
  'Tls'
  'Tls11'
  'Tls12'
])
param minimalTlsVersion string = 'Tls12'

// =========================================
// Cosmos DB Account
// =========================================

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDbAccountName
  location: location
  tags: {
    defaultExperience: 'Core (SQL)'
    'hidden-workload-type': 'Development/Testing'
  }
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'None'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: enableAutomaticFailover
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: enableFreeTier
    enableAnalyticalStorage: false
    databaseAccountOfferType: 'Standard'
    defaultIdentity: 'FirstPartyIdentity'
    networkAclBypass: 'None'
    disableLocalAuth: false
    enablePartitionMerge: false
    minimalTlsVersion: minimalTlsVersion
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: true
      }
    ]
    cors: []
    capabilities: capacityMode == 'Serverless' ? [
      {
        name: 'EnableServerless'
      }
    ] : []
    ipRules: []
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 8
        backupStorageRedundancy: 'Geo'
      }
    }
    networkAclBypassResourceIds: []
  }
}

// =========================================
// Outputs
// =========================================

output cosmosDbAccountName string = cosmosDbAccount.name
output cosmosDbAccountId string = cosmosDbAccount.id
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output cosmosDbPrimaryKey string = cosmosDbAccount.listKeys().primaryMasterKey
output cosmosDbConnectionString string = 'AccountEndpoint=${cosmosDbAccount.properties.documentEndpoint};AccountKey=${cosmosDbAccount.listKeys().primaryMasterKey}'
