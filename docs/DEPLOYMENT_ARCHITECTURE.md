# Deployment Architecture Diagram

## Overview

This document provides visual representations of the deployment architecture for the One Page Author API platform.

## GitHub Actions Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub Actions Workflow                       │
│                    (main_onepageauthorapi.yml)                       │
└─────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴────────────────┐
                    │                                │
        ┌───────────▼──────────┐          ┌────────▼─────────┐
        │   Checkout & Setup   │          │  Azure Login     │
        │  - Code checkout     │          │  - Service       │
        │  - .NET 10 SDK       │          │    Principal     │
        └───────────┬──────────┘          └────────┬─────────┘
                    │                              │
        ┌───────────▼──────────────────────────────▼─────────┐
        │              Build All Function Apps                │
        │  ┌────────────┐  ┌──────────────┐  ┌─────────────┐│
        │  │ function-  │  │  ImageAPI    │  │InkStained   ││
        │  │   app      │  │              │  │Wretch*      ││
        │  └────────────┘  └──────────────┘  └─────────────┘│
        │  * InkStainedWretchFunctions & InkStainedWretchStripe│
        └───────────────────────────┬──────────────────────────┘
                                    │
        ┌───────────────────────────▼──────────────────────────┐
        │              Conditional Deployments                  │
        └───────────────────────────┬──────────────────────────┘
                                    │
            ┌───────────────────────┼────────────────────────┐
            │                       │                        │
    ┌───────▼────────┐    ┌────────▼────────┐    ┌────────▼──────────┐
    │  Existing      │    │  ISW            │    │  Function Apps    │
    │  function-app  │    │  Infrastructure │    │  Deployment       │
    │  Deployment    │    │  Deployment     │    │  - ImageAPI       │
    │                │    │  (Bicep)        │    │  - ISWFunctions   │
    │  Conditional:  │    │                 │    │  - ISWStripe      │
    │  - AZURE_      │    │  Conditional:   │    │                   │
    │    CREDENTIALS │    │  - ISW_RESOURCE │    │  Conditional:     │
    │  - AZURE_      │    │    _GROUP       │    │  - DEPLOY_*       │
    │    FUNCTIONAPP │    │  - ISW_LOCATION │    │    flags          │
    │    _NAME       │    │  - ISW_BASE     │    │  - ISW secrets    │
    │                │    │    _NAME        │    │                   │
    └────────────────┘    └─────────────────┘    └───────────────────┘
```

## Azure Infrastructure Architecture

### Ink Stained Wretches Resource Group

```
┌─────────────────────────────────────────────────────────────────────┐
│            Azure Resource Group: InkStainedWretches-RG               │
└─────────────────────────────────────────────────────────────────────┘
                                    │
        ┌───────────────────────────┼────────────────────────────┐
        │                           │                            │
┌───────▼────────┐      ┌───────────▼─────────┐      ┌─────────▼──────┐
│  Storage       │      │  Key Vault          │      │  Application   │
│  Account       │      │                     │      │  Insights      │
│                │      │  - Secrets          │      │                │
│  - Blobs       │      │  - Certificates     │      │  - Monitoring  │
│  - Files       │      │  - RBAC enabled     │      │  - Logging     │
│  - Function    │      │  - Soft delete: 90d │      │  - Metrics     │
│    runtime     │      │                     │      │  - Retention:  │
│                │      │                     │      │    90 days     │
└────────────────┘      └─────────────────────┘      └────────────────┘
        │
        │               ┌─────────────────────┐      ┌────────────────┐
        │               │  DNS Zone           │      │  Static Web    │
        │               │  (Optional)         │      │  App           │
        │               │                     │      │  (Optional)    │
        │               │  - Public zone      │      │                │
        │               │  - Custom domain    │      │  - Free tier   │
        │               │                     │      │  - GitHub sync │
        │               └─────────────────────┘      └────────────────┘
        │
        │
        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   App Service Plan (Consumption)                     │
│                        - Y1/Dynamic SKU                              │
└─────────────────────────────────────────────────────────────────────┘
        │
        ├────────────────────┬────────────────────┬─────────────────────┐
        │                    │                    │                     │
┌───────▼────────┐  ┌────────▼──────────┐  ┌─────▼───────────────┐  │
│  ImageAPI      │  │  InkStainedWretch │  │  InkStainedWretch   │  │
│  Function App  │  │  Functions        │  │  Stripe             │  │
│                │  │  Function App     │  │  Function App       │  │
│  Endpoints:    │  │                   │  │                     │  │
│  - Upload      │  │  Endpoints:       │  │  Endpoints:         │  │
│  - Retrieve    │  │  - Localization   │  │  - Checkout         │  │
│  - Delete      │  │  - Domain mgmt    │  │  - Customers        │  │
│                │  │  - External APIs  │  │  - Subscriptions    │  │
│                │  │                   │  │  - Webhooks         │  │
└────────────────┘  └───────────────────┘  └─────────────────────┘  │
        │                    │                    │                     │
        └────────────────────┴────────────────────┴─────────────────────┘
                                    │
                                    ▼
                    ┌─────────────────────────────┐
                    │  Application Insights       │
                    │  - Telemetry collection     │
                    │  - Performance monitoring   │
                    │  - Exception tracking       │
                    └─────────────────────────────┘
```

## Data Flow Architecture

### User Request Flow

```
┌─────────┐
│  User/  │
│  Client │
└────┬────┘
     │
     │ HTTPS Request (JWT Token)
     │
     ▼
┌─────────────────────────────────────┐
│      Azure Function Endpoint        │
│  - ImageAPI                         │
│  - InkStainedWretchFunctions        │
│  - InkStainedWretchStripe           │
└─────────────────┬───────────────────┘
                  │
                  │ 1. Validate JWT Token
                  │    (Microsoft Entra ID)
                  │
                  ▼
┌─────────────────────────────────────┐
│     Function App Processing         │
│  - Validate input                   │
│  - Execute business logic           │
│  - Log to Application Insights      │
└─────────────────┬───────────────────┘
                  │
        ┌─────────┼─────────┬─────────────────┐
        │         │         │                 │
        │         │         │                 │
        ▼         ▼         ▼                 ▼
┌──────────┐ ┌────────┐ ┌──────────┐ ┌──────────────┐
│  Cosmos  │ │ Stripe │ │  Azure   │ │  External    │
│  DB      │ │  API   │ │  Storage │ │  APIs        │
│          │ │        │ │          │ │  (Amazon,    │
│  - Read  │ │ - Pay  │ │  - Blob  │ │   Penguin)   │
│  - Write │ │ - Sub  │ │  - File  │ │              │
└──────────┘ └────────┘ └──────────┘ └──────────────┘
        │         │         │                 │
        └─────────┴─────────┴─────────────────┘
                  │
                  │ Response
                  │
                  ▼
┌─────────────────────────────────────┐
│      Return Response to Client      │
│  - JSON data                        │
│  - Status code                      │
│  - Headers                          │
└─────────────────────────────────────┘
```

## Secrets Management Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                  GitHub Repository Secrets                    │
│                                                               │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │  Azure Creds   │  │  ISW Infra      │  │  App Config  │ │
│  │  - Service     │  │  - Resource     │  │  - Cosmos DB │ │
│  │    Principal   │  │    Group        │  │  - Stripe    │ │
│  │  - Subscription│  │  - Location     │  │  - Entra ID  │ │
│  └────────────────┘  └─────────────────┘  └──────────────┘ │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             │ Secure Parameter Passing
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│                   GitHub Actions Workflow                     │
│                                                               │
│  - Secrets accessed as ${{ secrets.NAME }}                   │
│  - Never exposed in logs                                     │
│  - Passed securely to Azure CLI                              │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             │ Bicep @secure() Parameters
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│                   Azure Deployment                            │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Bicep Template (inkstainedwretches.bicep)           │   │
│  │  - @secure() decorator for sensitive parameters      │   │
│  │  - No secrets in outputs                             │   │
│  │  - Conditional deployment based on parameters        │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             │ Deployment
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│                   Azure Resources                             │
│                                                               │
│  ┌─────────────────┐          ┌──────────────────────┐      │
│  │  Function Apps  │          │  Key Vault           │      │
│  │  - App Settings │◄─────────│  - Runtime Secrets   │      │
│  │    (Config)     │  Managed │  - Certificates      │      │
│  │                 │ Identity │  - Keys              │      │
│  └─────────────────┘          └──────────────────────┘      │
└──────────────────────────────────────────────────────────────┘
```

## Conditional Deployment Logic

```
                    ┌──────────────────────┐
                    │  GitHub Secrets      │
                    │  Configuration       │
                    └──────────┬───────────┘
                               │
                               ▼
        ┌──────────────────────────────────────────────┐
        │  Are required ISW secrets configured?        │
        │  - ISW_RESOURCE_GROUP                        │
        │  - ISW_LOCATION                              │
        │  - ISW_BASE_NAME                             │
        └──────────┬───────────────────────────────────┘
                   │
         ┌─────────┴─────────┐
         │                   │
      YES│                   │NO
         ▼                   ▼
┌─────────────────┐   ┌─────────────────────┐
│  Deploy ISW     │   │  Skip ISW           │
│  Infrastructure │   │  Infrastructure     │
└────────┬────────┘   │  Log warning        │
         │            └─────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────┐
│  Check Optional Components                  │
└─────────────────────────────────────────────┘
         │
         ├─► ISW_DNS_ZONE_NAME configured?
         │   YES → Deploy DNS Zone
         │   NO  → Skip DNS Zone
         │
         └─► Always Deploy:
             - Storage Account
             - Key Vault
             - Application Insights
             - App Service Plan
             - Function Apps (if DEPLOY_* flags set)
```

## Deployment Scenarios Matrix

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Deployment Scenarios                            │
├──────────────┬──────────────┬────────────────┬─────────────────────┤
│  Scenario    │  Secrets Set │  Infrastructure│  Function Apps      │
├──────────────┼──────────────┼────────────────┼─────────────────────┤
│  Scenario 1  │  All secrets │  ✓ All         │  ✓ All 3 deployed   │
│  Full Deploy │  configured  │  resources     │  (ImageAPI, ISW*2)  │
├──────────────┼──────────────┼────────────────┼─────────────────────┤
│  Scenario 2  │  Only infra  │  ✓ Core only   │  ✗ None deployed    │
│  Infra Only  │  secrets     │  (Storage, KV, │                     │
│              │              │   AppInsights) │                     │
├──────────────┼──────────────┼────────────────┼─────────────────────┤
│  Scenario 3  │  Infra +     │  ✓ All         │  ✓ Only selected    │
│  Partial     │  Selected    │  resources     │  (based on flags)   │
│              │  DEPLOY_*    │                │                     │
├──────────────┼──────────────┼────────────────┼─────────────────────┤
│  Scenario 4  │  Only        │  ✗ ISW skipped │  ✗ ISW skipped      │
│  Legacy Only │  function-app│  ✓ Legacy      │  ✓ function-app     │
│              │  secrets     │  deployed      │  only               │
└──────────────┴──────────────┴────────────────┴─────────────────────┘
```

## Security Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                      Security Layer 1                            │
│                   GitHub Secrets Management                      │
│  - Encrypted at rest                                            │
│  - Never exposed in logs                                        │
│  - Access controlled by repository permissions                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Security Layer 2                            │
│                   Azure Service Principal                        │
│  - Limited permissions (Contributor role)                       │
│  - Scoped to specific subscriptions                             │
│  - Rotatable credentials                                        │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Security Layer 3                            │
│                   Bicep Template Security                        │
│  - @secure() decorator for sensitive parameters                 │
│  - No secrets in outputs                                        │
│  - Conditional deployment reduces attack surface                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Security Layer 4                            │
│                   Azure Resource Security                        │
│  - HTTPS only                                                   │
│  - TLS 1.2 minimum                                              │
│  - FTPS only (no FTP)                                           │
│  - Key Vault with RBAC                                          │
│  - Storage: Public access disabled                              │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Security Layer 5                            │
│                   Application Security                           │
│  - Microsoft Entra ID authentication                            │
│  - JWT token validation                                         │
│  - Input validation                                             │
│  - Secure configuration in Function Apps                        │
└─────────────────────────────────────────────────────────────────┘
```

## Monitoring and Observability

```
┌──────────────────────────────────────────────────────────────────┐
│                     Application Insights                          │
│                   (Central Monitoring Hub)                        │
└────────────┬─────────────────────────────────────┬───────────────┘
             │                                     │
             │                                     │
    ┌────────▼────────┐                  ┌────────▼────────┐
    │  Real-time      │                  │  Historical     │
    │  Monitoring     │                  │  Analysis       │
    │                 │                  │                 │
    │  - Live Metrics │                  │  - Queries      │
    │  - Request rate │                  │  - Dashboards   │
    │  - Failures     │                  │  - Alerts       │
    │  - Dependencies │                  │  - Trends       │
    └─────────────────┘                  └─────────────────┘
             │                                     │
             │                                     │
             └──────────────┬──────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────────────┐
        │         Connected Function Apps           │
        ├─────────────┬──────────────┬──────────────┤
        │  ImageAPI   │  ISWFunctions│  ISWStripe   │
        │             │              │              │
        │  - Requests │  - Requests  │  - Requests  │
        │  - Logs     │  - Logs      │  - Logs      │
        │  - Traces   │  - Traces    │  - Traces    │
        │  - Metrics  │  - Metrics   │  - Metrics   │
        └─────────────┴──────────────┴──────────────┘
```

## Legend

```
┌─────────┐
│  Box    │  = Component or Service
└─────────┘

    │
    ▼         = Data/Control Flow (downward)

    ◄─────    = Data/Control Flow (leftward)

┌───────────┐
│  ✓ Text   │ = Feature included/enabled
└───────────┘

┌───────────┐
│  ✗ Text   │ = Feature excluded/disabled
└───────────┘
```

---

**Document Version**: 1.0
**Last Updated**: December 2024
**Related Documentation**:
- [Deployment Guide](DEPLOYMENT_GUIDE.md)
- [GitHub Secrets Reference](GITHUB_SECRETS_REFERENCE.md)
- [Implementation Summary](IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT.md)
