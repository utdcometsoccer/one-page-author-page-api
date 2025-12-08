// =========================================
// Azure Communication Services for Email
// =========================================
// This Bicep template deploys Azure Communication Services
// for sending email notifications (author invitations).

@description('The base name for the Communication Services resource')
param baseName string

@description('The location for the Communication Services resource')
param location string = resourceGroup().location

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

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
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

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

// Azure Managed Domain (optional - provides a default sending domain)
// This creates a managed domain like "<uniqueid>.azurecomm.net"
resource emailServiceDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  tags: tags
  properties: {
    domainManagement: 'AzureManaged'
  }
}

// Link Communication Service to Email Service
// Note: This creates the connection between ACS and Email Service
resource senderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01' = {
  parent: emailServiceDomain
  name: 'DoNotReply'
  properties: {
    username: 'DoNotReply'
    displayName: 'One Page Author Invitations'
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
output senderAddress string = 'DoNotReply@${emailServiceDomain.properties.mailFromSenderDomain}'

// Output connection string components (to be used with Key Vault)
// Note: The actual connection string must be retrieved using Azure CLI or Portal
output connectionStringNote string = 'Retrieve connection string from Azure Portal: Communication Services -> Keys'
