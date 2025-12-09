// =========================================
// Azure Communication Services for Email
// =========================================
// This Bicep template deploys Azure Communication Services
// for sending email notifications (author invitations).

@description('The base name for the Communication Services resource')
param baseName string

@description('The data location for Communication Services (e.g., United States)')
param dataLocation string = 'United States'

@description('Tags to apply to all resources')
param tags object = {
  environment: 'production'
  project: 'OnePageAuthor'
  component: 'EmailService'
}

// =========================================
// Variables
// =========================================

var communicationServiceName = '${baseName}-acs'
var emailServiceName = '${baseName}-email'

// =========================================
// Communication Services
// =========================================

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01-preview' = {
  name: communicationServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// =========================================
// Email Service (Domain)
// =========================================
// Note: Azure Communication Services Email requires a verified domain.
// This creates the email service resource, but you must verify your domain
// through the Azure Portal after deployment.

resource emailService 'Microsoft.Communication/emailServices@2023-04-01-preview' = {
  name: emailServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// Azure Managed Domain (optional - provides a default sending domain)
// This creates a managed domain like "<uniqueid>.azurecomm.net"
resource emailServiceDomain 'Microsoft.Communication/emailServices/domains@2023-04-01-preview' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  tags: tags
  properties: {
    domainManagement: 'AzureManaged'
  }
}

// =========================================
// Outputs
// =========================================

output communicationServiceName string = communicationService.name
output communicationServiceId string = communicationService.id
output communicationServiceEndpoint string = communicationService.properties.hostName
output emailServiceName string = emailService.name
output emailServiceId string = emailService.id
output emailServiceDomainName string = emailServiceDomain.name
output senderDomain string = emailServiceDomain.properties.fromSenderDomain

// Output connection string components (to be used with Key Vault)
// Note: The actual connection string must be retrieved using Azure CLI or Portal
output connectionStringNote string = 'Retrieve connection string from Azure Portal: Communication Services -> Keys'
