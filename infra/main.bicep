param databaseAccounts_one_page_author_db_account_name string = 'one-page-author-db-account'
param dnszones_edokpayi_com_name string = 'edokpayi.com'
param staticSites_author_page_demo_name string = 'author-page-demo'
param dnszones_monicasalmon_com_name string = 'monicasalmon.com'
param components_author_page_demo_name string = 'author-page-demo'
param actionGroups_Application_Insights_Smart_Detection_name string = 'Application Insights Smart Detection'
param workspaces_DefaultWorkspace_3869f4ae_d40f_4bc2_9333_9744e204183b_WUS2_externalid string = '/subscriptions/3869f4ae-d40f-4bc2-9333-9744e204183b/resourceGroups/DefaultResourceGroup-WUS2/providers/Microsoft.OperationalInsights/workspaces/DefaultWorkspace-3869f4ae-d40f-4bc2-9333-9744e204183b-WUS2'

resource databaseAccounts_one_page_author_db_account_name_resource 'Microsoft.DocumentDB/databaseAccounts@2025-05-01-preview' = {
  name: databaseAccounts_one_page_author_db_account_name
  location: 'Central US'
  tags: {
    defaultExperience: 'Core (SQL)'
    'hidden-workload-type': 'Development/Testing'
    'hidden-cosmos-mmspecial': ''
  }
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'None'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: true
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: false
    enableAnalyticalStorage: false
    analyticalStorageConfiguration: {
      schemaType: 'WellDefined'
    }
    databaseAccountOfferType: 'Standard'
    enableMaterializedViews: false
    capacityMode: 'Serverless'
    defaultIdentity: 'FirstPartyIdentity'
    networkAclBypass: 'None'
    disableLocalAuth: false
    enablePartitionMerge: false
    enablePerRegionPerPartitionAutoscale: false
    enableBurstCapacity: false
    enablePriorityBasedExecution: false
    defaultPriorityLevel: 'High'
    minimalTlsVersion: 'Tls12'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: 'Central US'
        failoverPriority: 0
        isZoneRedundant: true
      }
    ]
    cors: []
    capabilities: []
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
    diagnosticLogSettings: {
      enableFullTextQuery: 'None'
    }
    capacity: {
      totalThroughputLimit: 4000
    }
  }
}

resource databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000003 'Microsoft.DocumentDB/databaseAccounts/cassandraRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000003'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/read'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/containers/entities/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000004 'Microsoft.DocumentDB/databaseAccounts/cassandraRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000004'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/read'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/write'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/*'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/write'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/delete'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/cassandra/containers/entities/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource Microsoft_DocumentDB_databaseAccounts_gremlinRoleDefinitions_databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000003 'Microsoft.DocumentDB/databaseAccounts/gremlinRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000003'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/read'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/containers/entities/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource Microsoft_DocumentDB_databaseAccounts_gremlinRoleDefinitions_databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000004 'Microsoft.DocumentDB/databaseAccounts/gremlinRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000004'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/read'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/write'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/*'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/write'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/delete'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/gremlin/containers/entities/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource Microsoft_DocumentDB_databaseAccounts_mongoMIRoleDefinitions_databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000003 'Microsoft.DocumentDB/databaseAccounts/mongoMIRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000003'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/read'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/containers/entities/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource Microsoft_DocumentDB_databaseAccounts_mongoMIRoleDefinitions_databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000004 'Microsoft.DocumentDB/databaseAccounts/mongoMIRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000004'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/read'
          'Microsoft.DocumentDB/databaseAccounts/throughputSettings/write'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/*'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/write'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/delete'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/mongoMI/containers/entities/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000001 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000001'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000002 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000002'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource Microsoft_DocumentDB_databaseAccounts_tableRoleDefinitions_databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000001 'Microsoft.DocumentDB/databaseAccounts/tableRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000001'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/tables/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/tables/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/tables/containers/entities/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource Microsoft_DocumentDB_databaseAccounts_tableRoleDefinitions_databaseAccounts_one_page_author_db_account_name_00000000_0000_0000_0000_000000000002 'Microsoft.DocumentDB/databaseAccounts/tableRoleDefinitions@2025-05-01-preview' = {
  parent: databaseAccounts_one_page_author_db_account_name_resource
  name: '00000000-0000-0000-0000-000000000002'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_one_page_author_db_account_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/tables/*'
          'Microsoft.DocumentDB/databaseAccounts/tables/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/tables/containers/entities/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource actionGroups_Application_Insights_Smart_Detection_name_resource 'microsoft.insights/actionGroups@2024-10-01-preview' = {
  name: actionGroups_Application_Insights_Smart_Detection_name
  location: 'Global'
  properties: {
    groupShortName: 'SmartDetect'
    enabled: true
    emailReceivers: []
    smsReceivers: []
    webhookReceivers: []
    eventHubReceivers: []
    itsmReceivers: []
    azureAppPushReceivers: []
    automationRunbookReceivers: []
    voiceReceivers: []
    logicAppReceivers: []
    azureFunctionReceivers: []
    armRoleReceivers: [
      {
        name: 'Monitoring Contributor'
        roleId: '749f88d5-cbae-40b8-bcfc-e573ddc772fa'
        useCommonAlertSchema: true
      }
      {
        name: 'Monitoring Reader'
        roleId: '43d0d8ad-25c7-4714-9337-8ba259a9fe05'
        useCommonAlertSchema: true
      }
    ]
  }
}

resource components_author_page_demo_name_resource 'microsoft.insights/components@2020-02-02' = {
  name: components_author_page_demo_name
  location: 'westus2'
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'IbizaAIExtension'
    RetentionInDays: 90
    WorkspaceResourceId: workspaces_DefaultWorkspace_3869f4ae_d40f_4bc2_9333_9744e204183b_WUS2_externalid
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource dnszones_edokpayi_com_name_resource 'Microsoft.Network/dnszones@2023-07-01-preview' = {
  name: dnszones_edokpayi_com_name
  location: 'global'
  properties: {
    zoneType: 'Public'
  }
}

resource dnszones_monicasalmon_com_name_resource 'Microsoft.Network/dnszones@2023-07-01-preview' = {
  name: dnszones_monicasalmon_com_name
  location: 'global'
  properties: {
    zoneType: 'Public'
  }
}

resource staticSites_author_page_demo_name_resource 'Microsoft.Web/staticSites@2024-11-01' = {
  name: staticSites_author_page_demo_name
  location: 'West US 2'
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: 'https://github.com/utdcometsoccer/one-page-author-page'
    branch: 'main'
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource components_author_page_demo_name_degradationindependencyduration 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'degradationindependencyduration'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'degradationindependencyduration'
      DisplayName: 'Degradation in dependency duration'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_degradationinserverresponsetime 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'degradationinserverresponsetime'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'degradationinserverresponsetime'
      DisplayName: 'Degradation in server response time'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_digestMailConfiguration 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'digestMailConfiguration'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'digestMailConfiguration'
      DisplayName: 'Digest Mail Configuration'
      Description: 'This rule describes the digest mail preferences'
      HelpUrl: 'www.homail.com'
      IsHidden: true
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_extension_billingdatavolumedailyspikeextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'extension_billingdatavolumedailyspikeextension'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'extension_billingdatavolumedailyspikeextension'
      DisplayName: 'Abnormal rise in daily data volume (preview)'
      Description: 'This detection rule automatically analyzes the billing data generated by your application, and can warn you about an unusual increase in your application\'s billing costs'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/tree/master/SmartDetection/billing-data-volume-daily-spike.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_extension_canaryextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'extension_canaryextension'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'extension_canaryextension'
      DisplayName: 'Canary extension'
      Description: 'Canary extension'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/'
      IsHidden: true
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_extension_exceptionchangeextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'extension_exceptionchangeextension'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'extension_exceptionchangeextension'
      DisplayName: 'Abnormal rise in exception volume (preview)'
      Description: 'This detection rule automatically analyzes the exceptions thrown in your application, and can warn you about unusual patterns in your exception telemetry.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/abnormal-rise-in-exception-volume.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_extension_memoryleakextension 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'extension_memoryleakextension'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'extension_memoryleakextension'
      DisplayName: 'Potential memory leak detected (preview)'
      Description: 'This detection rule automatically analyzes the memory consumption of each process in your application, and can warn you about potential memory leaks or increased memory consumption.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/tree/master/SmartDetection/memory-leak.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_extension_securityextensionspackage 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'extension_securityextensionspackage'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'extension_securityextensionspackage'
      DisplayName: 'Potential security issue detected (preview)'
      Description: 'This detection rule automatically analyzes the telemetry generated by your application and detects potential security issues.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/application-security-detection-pack.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_extension_traceseveritydetector 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'extension_traceseveritydetector'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'extension_traceseveritydetector'
      DisplayName: 'Degradation in trace severity ratio (preview)'
      Description: 'This detection rule automatically analyzes the trace logs emitted from your application, and can warn you about unusual patterns in the severity of your trace telemetry.'
      HelpUrl: 'https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SmartDetection/degradation-in-trace-severity-ratio.md'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_longdependencyduration 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'longdependencyduration'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'longdependencyduration'
      DisplayName: 'Long dependency duration'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_migrationToAlertRulesCompleted 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'migrationToAlertRulesCompleted'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'migrationToAlertRulesCompleted'
      DisplayName: 'Migration To Alert Rules Completed'
      Description: 'A configuration that controls the migration state of Smart Detection to Smart Alerts'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: true
      IsEnabledByDefault: false
      IsInPreview: true
      SupportsEmailNotifications: false
    }
    enabled: false
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_slowpageloadtime 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'slowpageloadtime'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'slowpageloadtime'
      DisplayName: 'Slow page load time'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource components_author_page_demo_name_slowserverresponsetime 'microsoft.insights/components/ProactiveDetectionConfigs@2018-05-01-preview' = {
  parent: components_author_page_demo_name_resource
  name: 'slowserverresponsetime'
  location: 'westus2'
  properties: {
    ruleDefinitions: {
      Name: 'slowserverresponsetime'
      DisplayName: 'Slow server response time'
      Description: 'Smart Detection rules notify you of performance anomaly issues.'
      HelpUrl: 'https://docs.microsoft.com/en-us/azure/application-insights/app-insights-proactive-performance-diagnostics'
      IsHidden: false
      IsEnabledByDefault: true
      IsInPreview: false
      SupportsEmailNotifications: true
    }
    enabled: true
    sendEmailsToSubscriptionOwners: true
    customEmails: []
  }
}

resource dnszones_edokpayi_com_name_autodiscover 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: 'autodiscover'
  properties: {
    TTL: 3600
    CNAMERecord: {
      cname: 'autodiscover.outlook.com'
    }
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource dnszones_monicasalmon_com_name_www 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_monicasalmon_com_name_resource
  name: 'www'
  properties: {
    TTL: 3600
    CNAMERecord: {
      cname: 'monicasalmon.com'
    }
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_MX_dnszones_edokpayi_com_name 'Microsoft.Network/dnszones/MX@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    MXRecords: [
      {
        exchange: 'edokpayi-com.mail.protection.outlook.com'
        preference: 0
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_NS_dnszones_edokpayi_com_name 'Microsoft.Network/dnszones/NS@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: '@'
  properties: {
    TTL: 172800
    NSRecords: [
      {
        nsdname: 'ns1-04.azure-dns.com.'
      }
      {
        nsdname: 'ns2-04.azure-dns.net.'
      }
      {
        nsdname: 'ns3-04.azure-dns.org.'
      }
      {
        nsdname: 'ns4-04.azure-dns.info.'
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_NS_dnszones_monicasalmon_com_name 'Microsoft.Network/dnszones/NS@2023-07-01-preview' = {
  parent: dnszones_monicasalmon_com_name_resource
  name: '@'
  properties: {
    TTL: 172800
    NSRecords: [
      {
        nsdname: 'ns1-06.azure-dns.com.'
      }
      {
        nsdname: 'ns2-06.azure-dns.net.'
      }
      {
        nsdname: 'ns3-06.azure-dns.org.'
      }
      {
        nsdname: 'ns4-06.azure-dns.info.'
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_SOA_dnszones_edokpayi_com_name 'Microsoft.Network/dnszones/SOA@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    SOARecord: {
      email: 'azuredns-hostmaster.microsoft.com'
      expireTime: 2419200
      host: 'ns1-04.azure-dns.com.'
      minimumTTL: 300
      refreshTime: 3600
      retryTime: 300
      serialNumber: 1
    }
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_SOA_dnszones_monicasalmon_com_name 'Microsoft.Network/dnszones/SOA@2023-07-01-preview' = {
  parent: dnszones_monicasalmon_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    SOARecord: {
      email: 'azuredns-hostmaster.microsoft.com'
      expireTime: 2419200
      host: 'ns1-06.azure-dns.com.'
      minimumTTL: 300
      refreshTime: 3600
      retryTime: 300
      serialNumber: 1
    }
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_TXT_dnszones_edokpayi_com_name 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    TXTRecords: [
      {
        value: [
          '_4gcrke1qi6qhdj0b5nfjsg5hntiipue'
        ]
      }
      {
        value: [
          '_u6c36nnaes9gai6yj5qbqtgtq0xj3xw'
        ]
      }
      {
        value: [
          'v=spf1 include:spf.protection.outlook.com -all'
        ]
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_TXT_dnszones_monicasalmon_com_name 'Microsoft.Network/dnszones/TXT@2023-07-01-preview' = {
  parent: dnszones_monicasalmon_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    TXTRecords: [
      {
        value: [
          '_ktahxrqchwfmv32a8xcsdnpr5vx66fy'
        ]
      }
      {
        value: [
          '_377wzj9tzbrhse6e087msiaqne1wk7r'
        ]
      }
    ]
    targetResource: {}
    trafficManagementProfile: {}
  }
}

resource staticSites_author_page_demo_name_default 'Microsoft.Web/staticSites/basicAuth@2024-11-01' = {
  parent: staticSites_author_page_demo_name_resource
  name: 'default'
  properties: {
    applicableEnvironmentsMode: 'SpecifiedEnvironments'
  }
}

resource staticSites_author_page_demo_name_edokpayi_com 'Microsoft.Web/staticSites/customDomains@2024-11-01' = {
  parent: staticSites_author_page_demo_name_resource
  name: 'edokpayi.com'
  properties: {}
}

resource staticSites_author_page_demo_name_monicasalmon_com 'Microsoft.Web/staticSites/customDomains@2024-11-01' = {
  parent: staticSites_author_page_demo_name_resource
  name: 'monicasalmon.com'
  properties: {}
}

resource Microsoft_Network_dnszones_A_dnszones_edokpayi_com_name 'Microsoft.Network/dnszones/A@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSites_author_page_demo_name_resource.id
    }
    trafficManagementProfile: {}
  }
}

resource Microsoft_Network_dnszones_A_dnszones_monicasalmon_com_name 'Microsoft.Network/dnszones/A@2023-07-01-preview' = {
  parent: dnszones_monicasalmon_com_name_resource
  name: '@'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSites_author_page_demo_name_resource.id
    }
    trafficManagementProfile: {}
  }
}

resource dnszones_edokpayi_com_name_www 'Microsoft.Network/dnszones/CNAME@2023-07-01-preview' = {
  parent: dnszones_edokpayi_com_name_resource
  name: 'www'
  properties: {
    TTL: 3600
    targetResource: {
      id: staticSites_author_page_demo_name_resource.id
    }
    trafficManagementProfile: {}
  }
}
