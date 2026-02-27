@description('The name of the Service Bus namespace')
param namespaceName string = 'onepageauthor-sb'

@description('The location for all resources')
param location string = resourceGroup().location

@description('The name of the queue used for WHMCS domain registration operations')
param whmcsQueueName string = 'whmcs-domain-registrations'

// Service Bus Namespace (Basic tier – lowest cost, supports queues only)
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: namespaceName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

// Queue for WHMCS domain registration messages
resource whmcsQueue 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: serviceBusNamespace
  name: whmcsQueueName
  properties: {
    // Retain unprocessed messages for 14 days
    messageTimeToLive: 'P14D'
    // Lock duration: 5 minutes (enough time for a WHMCS API call)
    lockDuration: 'PT5M'
    // Maximum delivery count before dead-lettering
    maxDeliveryCount: 5
    deadLetteringOnMessageExpiration: true
    enablePartitioning: false
    requiresDuplicateDetection: false
    requiresSession: false
  }
}

// Shared access policy for the function app (send only)
resource senderAuthRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2021-11-01' = {
  parent: serviceBusNamespace
  name: 'WhmcsSender'
  properties: {
    rights: [
      'Send'
    ]
  }
}

// Shared access policy for the VM worker (listen only)
resource listenerAuthRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2021-11-01' = {
  parent: serviceBusNamespace
  name: 'WhmcsListener'
  properties: {
    rights: [
      'Listen'
    ]
  }
}

@description('Service Bus namespace name')
output namespaceName string = serviceBusNamespace.name

@description('Service Bus queue name for WHMCS operations')
output whmcsQueueName string = whmcsQueue.name

@description('Connection string for the function app (Send)')
@secure()
output senderConnectionString string = listKeys(senderAuthRule.id, senderAuthRule.apiVersion).primaryConnectionString

@description('Connection string for the VM worker (Listen)')
@secure()
output listenerConnectionString string = listKeys(listenerAuthRule.id, listenerAuthRule.apiVersion).primaryConnectionString
