# Documentation Index

This index provides a comprehensive guide to all documentation in the OnePageAuthor API repository.

## 📚 Documentation Organization

All documentation is organized in the [`docs/`](.) directory with the following structure:

### 🎯 Getting Started

Start here if you're new to the project:

1. [**Main README**](../README.md) - Project overview, quick start, and architecture
2. [**PRODUCT_ROADMAP**](../docs/PRODUCT_ROADMAP.md) - **Strategic roadmap** - Features, technical debt, and release planning
3. [**CONTRIBUTING**](../CONTRIBUTING.md) - How to contribute to the project
4. [**CODE_OF_CONDUCT**](../CODE_OF_CONDUCT.md) - Community guidelines
5. [**SECURITY**](SECURITY.md) - Security policies and vulnerability reporting

### 🌐 WHMCS API Proxy (Domain Registration)

**Domain registration uses a queue-based VM proxy** — start here to understand and deploy it:

- [**WhmcsWorkerService/README.md**](../WhmcsWorkerService/README.md) ⭐ — **Full step-by-step deployment guide**: provision Azure VM with static IP, install .NET runtime, build and deploy the worker, configure systemd, set WHMCS IP allowlist
- [**WHMCS_INTEGRATION_SUMMARY**](WHMCS_INTEGRATION_SUMMARY.md) — Architecture deep-dive: queue-based proxy pattern, implementation details, configuration reference, monitoring, and troubleshooting
- [**GET_TLD_PRICING_API**](GET_TLD_PRICING_API.md) — TLD pricing API reference (uses WHMCS credentials directly from the Function App)

> **Why a VM proxy?** Azure Functions run on shared infrastructure with dynamic outbound IPs. WHMCS restricts API access by IP. The `WhmcsWorkerService` runs on a Linux VM with a **static public IP**, so WHMCS can allowlist exactly one address. The Function enqueues domain registration requests to Azure Service Bus; the worker dequeues and calls WHMCS.

### 🔐 Authentication & Authorization

**NEW: Comprehensive Microsoft Entra ID documentation** - Start here for authentication setup:

- [**Authentication Documentation Hub**](authentication/README.md) - **START HERE** - Complete authentication guide index
- [**ENTRA_CIAM_CONFIGURATION_GUIDE**](authentication/ENTRA_CIAM_CONFIGURATION_GUIDE.md) - Step-by-step Entra ID CIAM setup
- [**AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST**](authentication/AZURE_ENTRA_ID_CONFIGURATION_CHECKLIST.md) - Configuration checklist
- [**ENVIRONMENT_VARIABLES_GUIDE**](authentication/ENVIRONMENT_VARIABLES_GUIDE.md) - All Entra ID environment variables explained
- [**JWT_INVALID_TOKEN_TROUBLESHOOTING**](authentication/JWT_INVALID_TOKEN_TROUBLESHOOTING.md) - JWT troubleshooting guide
- [**ENTRA_ID_EXCEPTIONS_REFERENCE**](authentication/ENTRA_ID_EXCEPTIONS_REFERENCE.md) - Complete exception reference
- [**APPLICATION_INSIGHTS_QUERIES**](authentication/APPLICATION_INSIGHTS_QUERIES.md) - KQL queries for monitoring

**Legacy Authentication Docs** (refer to new docs above):

- [MICROSOFT_ENTRA_ID_CONFIG](MICROSOFT_ENTRA_ID_CONFIG.md) - ⚠️ Legacy - See new ENTRA_CIAM_CONFIGURATION_GUIDE
- [IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES](IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) - Entra ID roles implementation
- [MIGRATION_GUIDE_ENTRA_ID_ROLES](MIGRATION_GUIDE_ENTRA_ID_ROLES.md) - Roles migration guide
- [401_UNAUTHORIZED_RESOLUTION](401_UNAUTHORIZED_RESOLUTION.md) - 401 error resolution
- [AUTHORIZATION_FIX_DOCUMENTATION](AUTHORIZATION_FIX_DOCUMENTATION.md) - Authorization fix details
- [JWT_KEY_ROTATION_FIX](JWT_KEY_ROTATION_FIX.md) - JWT key rotation fix
- [REFRESH_ON_ISSUER_KEY_NOT_FOUND](REFRESH_ON_ISSUER_KEY_NOT_FOUND.md) - Key refresh configuration
- [AUTHENTICATED_FUNCTION_LOGGING_IMPLEMENTATION](AUTHENTICATED_FUNCTION_LOGGING_IMPLEMENTATION.md) - Logging implementation

### 🔧 Development & Setup

Documentation for local development and configuration:

- [**DEVELOPMENT_SCRIPTS**](DEVELOPMENT_SCRIPTS.md) - Development automation scripts (UpdateAndRun.ps1, etc.)
- [**LOCAL_SETTINGS_SETUP**](LOCAL_SETTINGS_SETUP.md) - Local development configuration
- [**ConfigurationValidation**](ConfigurationValidation.md) - Configuration validation patterns
- [**ConfigurationMaskingStandardization**](ConfigurationMaskingStandardization.md) - Sensitive data masking
- [**ITELEMETRYINITIALIZER_BUG**](ITELEMETRYINITIALIZER_BUG.md) - Application Insights version mismatch fix
- [**AZURE_STORAGE_EMULATOR_SETUP**](AZURE_STORAGE_EMULATOR_SETUP.md) - Storage emulator configuration
- [**AZURE_COMMUNICATION_SERVICES_SETUP**](AZURE_COMMUNICATION_SERVICES_SETUP.md) - Email services setup
- [**DOTNET_10_UPGRADE**](DOTNET_10_UPGRADE.md) - .NET 10 upgrade guide

### 🚀 Deployment & Infrastructure

Complete deployment workflow documentation:

- [**DEPLOYMENT_GUIDE**](DEPLOYMENT_GUIDE.md) - **START HERE** - Complete deployment workflow (includes WHMCS Worker Service)
- [**DEPLOYMENT_ARCHITECTURE**](DEPLOYMENT_ARCHITECTURE.md) - Infrastructure architecture overview (includes WHMCS proxy diagram)
- [**GITHUB_SECRETS_REFERENCE**](GITHUB_SECRETS_REFERENCE.md) - **Essential** - All GitHub Secrets explained
- [**SECRETS_QUICK_REFERENCE**](SECRETS_QUICK_REFERENCE.md) - Quick reference card for all secrets
- [**COSMOS_APPINSIGHTS**](COSMOS_APPINSIGHTS.md) - Cosmos DB and Application Insights deployment (quick start, configuration, implementation details)
- [**GITHUB_ACTIONS_UPDATE**](GITHUB_ACTIONS_UPDATE.md) - CI/CD pipeline updates
- [**BACKWARD_COMPATIBILITY_PLAN**](BACKWARD_COMPATIBILITY_PLAN.md) - API backward compatibility strategy

### 📈 Monitoring & Telemetry

- [**APPLICATION_INSIGHTS_UPGRADE_MIGRATION_PLAN**](APPLICATION_INSIGHTS_UPGRADE_MIGRATION_PLAN.md) - Plan to move to latest App Insights approach

### 📖 API Documentation

Comprehensive API reference and guides:

- [**API_ENDPOINT_INVENTORY**](API_ENDPOINT_INVENTORY.md) ⭐ — **Exhaustive endpoint listing** — all HTTP routes, request/response schemas, authorization requirements, service dependencies, and unit test coverage
- [**COPILOT_IMPLEMENTATION_PROMPTS**](COPILOT_IMPLEMENTATION_PROMPTS.md) ⭐ — **Copilot prompts** to implement every endpoint in the new consolidated single-Function-App architecture (enqueue pattern for POST/PUT/DELETE, Notification Hub for completions)
- [**WHMCS_WORKER_REBUILD_PROMPT**](WHMCS_WORKER_REBUILD_PROMPT.md) — Copilot prompt to recreate the `WhmcsWorkerService` from scratch
- [**GITHUB_ACTIONS_SINGLE_APP_PROMPT**](GITHUB_ACTIONS_SINGLE_APP_PROMPT.md) — Copilot prompt to rebuild the GitHub Actions CI/CD workflow for the consolidated single Function App
- [**ADMIN_DOMAIN_CREATION_API**](ADMIN_DOMAIN_CREATION_API.md) - **Admin endpoint** — complete domain provisioning without Stripe (JS/TS client guide)
- [**API-Documentation**](API-Documentation.md) - **Complete API reference** with TypeScript examples
- [**AB_TESTING**](AB_TESTING.md) - A/B testing configuration API: endpoint reference, frontend integration, bucketing algorithm, and implementation details
- [**API_CATEGORIZATION**](API_CATEGORIZATION.md) - API endpoint categorization and grouping reference
- [**Complete-System-Documentation**](Complete-System-Documentation.md) - Auto-generated system overview
- [**README-Documentation**](README-Documentation.md) - Documentation generation system
- [**LocalizationREADME**](LocalizationREADME.md) - Internationalization and localization guide
- [**GET_TLD_PRICING_API**](GET_TLD_PRICING_API.md) - TLD pricing API reference with TypeScript/JavaScript examples
- [**WIKIPEDIA_API**](WIKIPEDIA_API.md) - Wikipedia API integration
- [**FEDIVERSE_INTEGRATION**](FEDIVERSE_INTEGRATION.md) - Fediverse and ActivityPub integration investigation

### 🌐 Social Media & Decentralized Protocols

Documentation for social media integrations and decentralized protocols:

- [**AT_PROTOCOL_IMPLEMENTATION**](AT_PROTOCOL_IMPLEMENTATION.md) - **Complete AT Protocol guide** - Implementation plan for Bluesky integration
- [**AT_PROTOCOL_QUICK_REFERENCE**](AT_PROTOCOL_QUICK_REFERENCE.md) - Quick reference for developers working with AT Protocol

### 🏗️ Implementation Summaries

Detailed implementation documentation for specific features:

#### Core Features

- [**AUTHOR_INVITATION_INITIAL_IMPLEMENTATION**](AUTHOR_INVITATION_INITIAL_IMPLEMENTATION.md) - Author Invitation System initial implementation (Dec 2024)
- [**IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES**](IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) - Entra ID RBAC
- [**IMPLEMENTATION_SUMMARY_LANGUAGES**](IMPLEMENTATION_SUMMARY_LANGUAGES.md) - Multi-language support
- [**IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT**](IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT.md) - Multi-function architecture
- [**IMPLEMENTATION_SUMMARY_MULTI_DOMAIN_INVITATIONS**](IMPLEMENTATION_SUMMARY_MULTI_DOMAIN_INVITATIONS.md) - Multi-domain invitation system

#### Infrastructure

- [**COSMOS_APPINSIGHTS**](COSMOS_APPINSIGHTS.md) - Cosmos DB and Application Insights deployment and implementation
- [**IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS**](IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS.md) - Configuration patterns
- [**IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX**](IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX.md) - Permission fixes

#### Data & Geography

- [**COUNTRIES_IMPLEMENTATION_SUMMARY**](COUNTRIES_IMPLEMENTATION_SUMMARY.md) - Country data
- [**STATEPROVINCE_BOILERPLATE_SUMMARY**](STATEPROVINCE_BOILERPLATE_SUMMARY.md) - Geographic entities

#### Integrations

- [**WHMCS_INTEGRATION_SUMMARY**](WHMCS_INTEGRATION_SUMMARY.md) - WHMCS domain registration integration (queue-based proxy architecture)
- [**KEY_VAULT_IMPLEMENTATION_SUMMARY**](KEY_VAULT_IMPLEMENTATION_SUMMARY.md) - Secrets management
- [**KEY_VAULT_ROLE_ASSIGNMENT_IMPLEMENTATION**](KEY_VAULT_ROLE_ASSIGNMENT_IMPLEMENTATION.md) - Key Vault RBAC
- [**FEDIVERSE_INTEGRATION**](FEDIVERSE_INTEGRATION.md) - Fediverse and ActivityPub integration (NEW)
- [**GET_TLD_PRICING_API**](GET_TLD_PRICING_API.md) - TLD pricing API for JS/TS developers

#### User Features

- [**DNS_ZONE_CREATION_IMPLEMENTATION**](DNS_ZONE_CREATION_IMPLEMENTATION.md) - DNS Zone Creation Azure Function
- [**AUTHOR_INVITATION_SYSTEM**](AUTHOR_INVITATION_SYSTEM.md) - Invitation overview
- [**INK_STAINED_WRETCH_USER_FEATURES**](INK_STAINED_WRETCH_USER_FEATURES.md) - User features

### ⚡ Feature Enhancements

Documentation for specific feature improvements:

- [**CULTURE_SUPPORT_ENHANCEMENT**](CULTURE_SUPPORT_ENHANCEMENT.md) - Multi-language subscription plans
- [**ACTIVE_PRODUCTS_FILTER_ENHANCEMENT**](ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md) - Stripe product filtering
- [**LABEL_VALIDATION_ENHANCEMENT**](LABEL_VALIDATION_ENHANCEMENT.md) - Input validation
- [**SUBSCRIPTION_PLAN_SERVICE_REFACTORING**](SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md) - Service architecture
- [**GETUSERUPN_REFACTORING**](GETUSERUPN_REFACTORING.md) - GetUserUpn refactoring details

### 🔄 Migration & Testing

Guides for migrating systems and testing:

- [**MONOREPO_MIGRATION_PLAN**](MONOREPO_MIGRATION_PLAN.md) ⭐ — **Monorepo consolidation plan** — team roles, GitHub issues, and step-by-step plan for uniting this repo with `ink-stained-wretch` and `one-page-author-page` under a single monorepo owned by the new Ink Stained Wretches legal entity
- [**MIGRATION_GUIDE_ENTRA_ID_ROLES**](MIGRATION_GUIDE_ENTRA_ID_ROLES.md) - Entra ID migration
- [**KEY_VAULT_MIGRATION_GUIDE**](KEY_VAULT_MIGRATION_GUIDE.md) - Key Vault migration
- [**MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES**](MIGRATION_CHECKLIST_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md) - Detailed API rename checklist
- [**MIGRATION_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES**](MIGRATION_ONE_PAGE_AUTHOR_TO_INK_STAINED_WRETCHES.md) - Comprehensive API rename migration guide
- [**TESTING_SCENARIOS_GUIDE**](TESTING_SCENARIOS_GUIDE.md) - Testing scenarios and strategies
- [**SECURITY_AUDIT_REPORT**](SECURITY_AUDIT_REPORT.md) - Security audit findings

### 🛠️ Utilities & Tools

Guides for utility tools and scripts:

- [**FIND_PARTNER_TAG**](FIND_PARTNER_TAG.md) - Amazon Partner Tag setup
- [**UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES**](UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES.md) - Stripe configuration
- [**STRIPE_PAYMENT_DASHBOARD**](STRIPE_PAYMENT_DASHBOARD.md) - Payment dashboard guide
- [**QUICK_START_INVITATION_TOOL**](QUICK_START_INVITATION_TOOL.md) - Invitation tool quick start
- [**ROLE_ASSIGNMENT_PERMISSIONS**](ROLE_ASSIGNMENT_PERMISSIONS.md) - Service principal role assignment: quick fix, detailed solution, and workflow context
- [**GITHUB_SECRETS_SCRIPT_EXAMPLES**](GITHUB_SECRETS_SCRIPT_EXAMPLES.md) - GitHub Secrets initialization script usage examples
- [**STEP_BY_STEP_CLEANUP**](STEP_BY_STEP_CLEANUP.md) - Repository cleanup procedures

## 🎓 Learning Paths

### New Developer Setup

1. Read [Main README](../README.md)
2. Follow [LOCAL_SETTINGS_SETUP](LOCAL_SETTINGS_SETUP.md)
3. Run [DEVELOPMENT_SCRIPTS](DEVELOPMENT_SCRIPTS.md)
4. Review [API-Documentation](API-Documentation.md)

### Deployment Engineer Setup

1. Read [DEPLOYMENT_GUIDE](DEPLOYMENT_GUIDE.md)
2. Configure [GITHUB_SECRETS_REFERENCE](GITHUB_SECRETS_REFERENCE.md)
3. Review [DEPLOYMENT_ARCHITECTURE](DEPLOYMENT_ARCHITECTURE.md)
4. Check [COSMOS_APPINSIGHTS](COSMOS_APPINSIGHTS.md)
5. **Deploy WHMCS Worker**: Follow [WhmcsWorkerService/README.md](../WhmcsWorkerService/README.md)

### WHMCS API Proxy Setup

1. Read the [WHMCS Integration Summary](WHMCS_INTEGRATION_SUMMARY.md) for architecture context
2. Follow the [WhmcsWorkerService/README.md](../WhmcsWorkerService/README.md) step-by-step guide:
   - Provision a Linux VM with a static public IP
   - Install .NET 10 runtime and set up system directories
   - Build and publish `WhmcsWorkerService`
   - Deploy to `/opt/whmcs-worker/` and configure `/etc/whmcs-worker/environment`
   - Install and enable the `whmcs-worker` systemd service
   - Configure WHMCS API credentials with the VM's static IP in the allowlist
   - Verify end-to-end with a test domain registration
3. Configure the Azure Function App with `SERVICE_BUS_CONNECTION_STRING` and `SERVICE_BUS_WHMCS_QUEUE_NAME`
4. Monitor with `sudo journalctl -u whmcs-worker -f` on the VM

### Feature Developer

1. Review [Complete-System-Documentation](Complete-System-Documentation.md)
2. Check relevant implementation summaries
3. Follow [CONTRIBUTING](../CONTRIBUTING.md) guidelines
4. Run [TESTING_SCENARIOS_GUIDE](TESTING_SCENARIOS_GUIDE.md)

## 📊 Documentation Statistics

- **Total Documentation Files**: 65+ markdown files
- **Main README Size**: 4,500+ lines (consolidated overview)
- **Documentation Categories**: 9 major categories
- **Implementation Summaries**: 12+ detailed guides
- **API Endpoints Documented**: 40+ endpoints across 4 function apps

## 🔍 Quick Reference

### Most Important Documents

1. **Getting Started**: [Main README](../README.md)
2. **API Reference**: [API-Documentation](API-Documentation.md)
3. **Deployment**: [DEPLOYMENT_GUIDE](DEPLOYMENT_GUIDE.md)
4. **Secrets Configuration**: [GITHUB_SECRETS_REFERENCE](GITHUB_SECRETS_REFERENCE.md)
5. **Development**: [DEVELOPMENT_SCRIPTS](DEVELOPMENT_SCRIPTS.md)
6. **WHMCS Proxy**: [WhmcsWorkerService/README.md](../WhmcsWorkerService/README.md)

### By Role

**Backend Developer**:

- [API-Documentation](API-Documentation.md)
- [Complete-System-Documentation](Complete-System-Documentation.md)
- [DEVELOPMENT_SCRIPTS](DEVELOPMENT_SCRIPTS.md)

**DevOps Engineer**:

- [DEPLOYMENT_GUIDE](DEPLOYMENT_GUIDE.md)
- [GITHUB_SECRETS_REFERENCE](GITHUB_SECRETS_REFERENCE.md)
- [DEPLOYMENT_ARCHITECTURE](DEPLOYMENT_ARCHITECTURE.md)
- [WhmcsWorkerService/README.md](../WhmcsWorkerService/README.md) — WHMCS VM worker deployment

**Security Specialist**:

- [SECURITY](SECURITY.md)
- [KEY_VAULT_IMPLEMENTATION_SUMMARY](KEY_VAULT_IMPLEMENTATION_SUMMARY.md)
- [SECURITY_AUDIT_REPORT](SECURITY_AUDIT_REPORT.md)

**QA Engineer**:

- [TESTING_SCENARIOS_GUIDE](TESTING_SCENARIOS_GUIDE.md)
- [API-Documentation](API-Documentation.md)

## 📝 Documentation Maintenance

### Contributing to Documentation

1. All documentation follows Markdown best practices
2. Keep documentation close to the code it describes
3. Update implementation summaries when making significant changes
4. Link between related documents
5. Follow the [CONTRIBUTING](../CONTRIBUTING.md) guidelines

### Documentation Standards

- Use clear, descriptive titles
- Include code examples where applicable
- Provide both quick start and detailed sections
- Keep configuration examples up-to-date
- Cross-reference related documentation

## 🤝 Getting Help

If you can't find what you're looking for:

1. Check the [Main README](../README.md) first
2. Search this index for relevant keywords
3. Check implementation summaries for feature-specific docs
4. Review [CONTRIBUTING](../CONTRIBUTING.md) for contribution guidelines
5. Open an issue on GitHub if documentation is missing or unclear

---

**Last Updated**: February 2026
**Maintained By**: OnePageAuthor API Team
