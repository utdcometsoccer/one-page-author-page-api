// =========================================
// Application Insights Infrastructure
// =========================================
// This Bicep template deploys Application Insights for monitoring
// Azure Functions and other application components.

@description('The name of the Application Insights resource')
param appInsightsName string

@description('The location for the Application Insights resource')
param location string = resourceGroup().location

@description('Application type')
@allowed([
  'web'
  'other'
])
param applicationType string = 'web'

@description('Retention period in days')
@allowed([
  30
  60
  90
  120
  180
  270
  365
  550
  730
])
param retentionInDays int = 90

@description('Log Analytics Workspace ID (optional)')
param workspaceResourceId string = ''

// =========================================
// Application Insights
// =========================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: applicationType
  properties: {
    Application_Type: applicationType
    Flow_Type: 'Redfield'
    Request_Source: 'rest'
    RetentionInDays: retentionInDays
    WorkspaceResourceId: !empty(workspaceResourceId) ? workspaceResourceId : null
    IngestionMode: !empty(workspaceResourceId) ? 'LogAnalytics' : 'ApplicationInsights'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// =========================================
// Outputs
// =========================================

output appInsightsName string = appInsights.name
output appInsightsId string = appInsights.id
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
