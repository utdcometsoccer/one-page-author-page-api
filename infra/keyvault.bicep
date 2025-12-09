// =========================================
// Key Vault Infrastructure
// =========================================
// This Bicep template deploys Azure Key Vault for secure storage
// of secrets, keys, and certificates.

@description('The name of the Key Vault')
param keyVaultName string

@description('The location for the Key Vault')
param location string = resourceGroup().location

@description('Enable RBAC authorization (recommended)')
param enableRbacAuthorization bool = true

@description('Enable soft delete (recommended for production)')
param enableSoftDelete bool = true

@description('Soft delete retention period in days')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 90

@description('Enable purge protection (prevents permanent deletion during retention period)')
param enablePurgeProtection bool = false

@description('SKU name for Key Vault')
@allowed([
  'standard'
  'premium'
])
param skuName string = 'standard'

@description('SKU family')
param skuFamily string = 'A'

@description('Enable Key Vault for deployment (ARM templates)')
param enabledForDeployment bool = true

@description('Enable Key Vault for template deployment')
param enabledForTemplateDeployment bool = true

@description('Enable Key Vault for disk encryption')
param enabledForDiskEncryption bool = false

@description('Public network access setting')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

// =========================================
// Key Vault
// =========================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: skuFamily
      name: skuName
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: enableRbacAuthorization
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection ? true : null
    enabledForDeployment: enabledForDeployment
    enabledForTemplateDeployment: enabledForTemplateDeployment
    enabledForDiskEncryption: enabledForDiskEncryption
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// =========================================
// Outputs
// =========================================

output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
