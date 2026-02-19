# Documentation Index

This index provides a comprehensive guide to all documentation in the OnePageAuthor API repository.

## 📚 Documentation Organization

All documentation is organized in the [`docs/`](.) directory with the following structure:

### 🎯 Getting Started

Start here if you're new to the project:

1. [**Main README**](../README.md) - Project overview, quick start, and architecture
2. [**PRODUCT_ROADMAP**](../PRODUCT_ROADMAP.md) - **Strategic roadmap** - Features, technical debt, and release planning
3. [**CONTRIBUTING**](CONTRIBUTING.md) - How to contribute to the project
4. [**CODE_OF_CONDUCT**](CODE_OF_CONDUCT.md) - Community guidelines
5. [**SECURITY**](SECURITY.md) - Security policies and vulnerability reporting

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

- [**DEPLOYMENT_GUIDE**](DEPLOYMENT_GUIDE.md) - **START HERE** - Complete deployment workflow
- [**DEPLOYMENT_ARCHITECTURE**](DEPLOYMENT_ARCHITECTURE.md) - Infrastructure architecture overview
- [**GITHUB_SECRETS_REFERENCE**](GITHUB_SECRETS_REFERENCE.md) - **Essential** - All GitHub Secrets explained
- [**COSMOS_APPINSIGHTS_DEPLOYMENT**](COSMOS_APPINSIGHTS_DEPLOYMENT.md) - Monitoring deployment
- [**QUICKSTART_COSMOS_APPINSIGHTS**](QUICKSTART_COSMOS_APPINSIGHTS.md) - Quick start for monitoring
- [**GITHUB_ACTIONS_UPDATE**](GITHUB_ACTIONS_UPDATE.md) - CI/CD pipeline updates

### 📈 Monitoring & Telemetry

- [**APPLICATION_INSIGHTS_UPGRADE_MIGRATION_PLAN**](APPLICATION_INSIGHTS_UPGRADE_MIGRATION_PLAN.md) - Plan to move to latest App Insights approach

### 📖 API Documentation

Comprehensive API reference and guides:

- [**ADMIN_DOMAIN_CREATION_API**](ADMIN_DOMAIN_CREATION_API.md) - **Admin endpoint** — complete domain provisioning without Stripe (JS/TS client guide)
- [**API-Documentation**](API-Documentation.md) - **Complete API reference** with TypeScript examples
- [**Complete-System-Documentation**](Complete-System-Documentation.md) - Auto-generated system overview
- [**README-Documentation**](README-Documentation.md) - Documentation generation system
- [**LocalizationREADME**](LocalizationREADME.md) - Internationalization and localization guide
- [**WIKIPEDIA_API**](WIKIPEDIA_API.md) - Wikipedia API integration
- [**FEDIVERSE_INTEGRATION**](FEDIVERSE_INTEGRATION.md) - Fediverse and ActivityPub integration investigation

### 🌐 Social Media & Decentralized Protocols

Documentation for social media integrations and decentralized protocols:

- [**AT_PROTOCOL_IMPLEMENTATION**](AT_PROTOCOL_IMPLEMENTATION.md) - **Complete AT Protocol guide** - Implementation plan for Bluesky integration
- [**AT_PROTOCOL_QUICK_REFERENCE**](AT_PROTOCOL_QUICK_REFERENCE.md) - Quick reference for developers working with AT Protocol

### 🏗️ Implementation Summaries

Detailed implementation documentation for specific features:

#### Core Features

- [**IMPLEMENTATION_SUMMARY**](IMPLEMENTATION_SUMMARY.md) - DNS zone automation
- [**IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES**](IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) - Entra ID RBAC
- [**IMPLEMENTATION_SUMMARY_LANGUAGES**](IMPLEMENTATION_SUMMARY_LANGUAGES.md) - Multi-language support
- [**IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT**](IMPLEMENTATION_SUMMARY_MULTI_FUNCTION_DEPLOYMENT.md) - Multi-function architecture

#### Infrastructure

- [**IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS**](IMPLEMENTATION_SUMMARY_COSMOS_APPINSIGHTS.md) - Monitoring implementation
- [**IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS**](IMPLEMENTATION_SUMMARY_CONDITIONAL_ENV_VARS.md) - Configuration patterns
- [**IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX**](IMPLEMENTATION_SUMMARY_PERMISSIONS_FIX.md) - Permission fixes

#### Data & Geography

- [**COUNTRIES_IMPLEMENTATION_SUMMARY**](COUNTRIES_IMPLEMENTATION_SUMMARY.md) - Country data
- [**STATEPROVINCE_BOILERPLATE_SUMMARY**](STATEPROVINCE_BOILERPLATE_SUMMARY.md) - Geographic entities

#### Integrations

- [**GOOGLE_DOMAINS_IMPLEMENTATION_SUMMARY**](GOOGLE_DOMAINS_IMPLEMENTATION_SUMMARY.md) - Domain registration
- [**KEY_VAULT_IMPLEMENTATION_SUMMARY**](KEY_VAULT_IMPLEMENTATION_SUMMARY.md) - Secrets management
- [**KEY_VAULT_ROLE_ASSIGNMENT_IMPLEMENTATION**](KEY_VAULT_ROLE_ASSIGNMENT_IMPLEMENTATION.md) - Key Vault RBAC
- [**FEDIVERSE_INTEGRATION**](FEDIVERSE_INTEGRATION.md) - Fediverse and ActivityPub integration (NEW)

#### User Features

- [**AUTHOR_INVITATION_IMPLEMENTATION_SUMMARY**](AUTHOR_INVITATION_IMPLEMENTATION_SUMMARY.md) - Invitation system
- [**AUTHOR_INVITATION_SYSTEM**](AUTHOR_INVITATION_SYSTEM.md) - Invitation overview
- [**INK_STAINED_WRETCH_USER_FEATURES**](INK_STAINED_WRETCH_USER_FEATURES.md) - User features

### ⚡ Feature Enhancements

Documentation for specific feature improvements:

- [**CULTURE_SUPPORT_ENHANCEMENT**](CULTURE_SUPPORT_ENHANCEMENT.md) - Multi-language subscription plans
- [**ACTIVE_PRODUCTS_FILTER_ENHANCEMENT**](ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md) - Stripe product filtering
- [**LABEL_VALIDATION_ENHANCEMENT**](LABEL_VALIDATION_ENHANCEMENT.md) - Input validation
- [**SUBSCRIPTION_PLAN_SERVICE_REFACTORING**](SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md) - Service architecture
- [**REFACTORING_SUMMARY**](REFACTORING_SUMMARY.md) - General refactoring documentation

### 🔄 Migration & Testing

Guides for migrating systems and testing:

- [**MIGRATION_GUIDE_ENTRA_ID_ROLES**](MIGRATION_GUIDE_ENTRA_ID_ROLES.md) - Entra ID migration
- [**KEY_VAULT_MIGRATION_GUIDE**](KEY_VAULT_MIGRATION_GUIDE.md) - Key Vault migration
- [**TESTING_SCENARIOS_GUIDE**](TESTING_SCENARIOS_GUIDE.md) - Testing scenarios and strategies
- [**SECURITY_AUDIT_REPORT**](SECURITY_AUDIT_REPORT.md) - Security audit findings

### 🛠️ Utilities & Tools

Guides for utility tools and scripts:

- [**FIND_PARTNER_TAG**](FIND_PARTNER_TAG.md) - Amazon Partner Tag setup
- [**UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES**](UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES.md) - Stripe configuration
- [**STRIPE_PAYMENT_DASHBOARD**](STRIPE_PAYMENT_DASHBOARD.md) - Payment dashboard guide
- [**QUICK_START_INVITATION_TOOL**](QUICK_START_INVITATION_TOOL.md) - Invitation tool quick start
- [**SERVICE_PRINCIPAL_PERMISSIONS_FIX**](SERVICE_PRINCIPAL_PERMISSIONS_FIX.md) - Permission fixes
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
4. Check [COSMOS_APPINSIGHTS_DEPLOYMENT](COSMOS_APPINSIGHTS_DEPLOYMENT.md)

### Feature Developer

1. Review [Complete-System-Documentation](Complete-System-Documentation.md)
2. Check relevant implementation summaries
3. Follow [CONTRIBUTING](CONTRIBUTING.md) guidelines
4. Run [TESTING_SCENARIOS_GUIDE](TESTING_SCENARIOS_GUIDE.md)

## 📊 Documentation Statistics

- **Total Documentation Files**: 70+ markdown files
- **Main README Size**: 4,431 lines (consolidated from 6,868)
- **Documentation Categories**: 8 major categories
- **Implementation Summaries**: 15+ detailed guides
- **API Endpoints Documented**: 40+ endpoints across 4 function apps

## 🔍 Quick Reference

### Most Important Documents

1. **Getting Started**: [Main README](../README.md)
2. **API Reference**: [API-Documentation](API-Documentation.md)
3. **Deployment**: [DEPLOYMENT_GUIDE](DEPLOYMENT_GUIDE.md)
4. **Secrets Configuration**: [GITHUB_SECRETS_REFERENCE](GITHUB_SECRETS_REFERENCE.md)
5. **Development**: [DEVELOPMENT_SCRIPTS](DEVELOPMENT_SCRIPTS.md)

### By Role

**Backend Developer**:

- [API-Documentation](API-Documentation.md)
- [Complete-System-Documentation](Complete-System-Documentation.md)
- [DEVELOPMENT_SCRIPTS](DEVELOPMENT_SCRIPTS.md)

**DevOps Engineer**:

- [DEPLOYMENT_GUIDE](DEPLOYMENT_GUIDE.md)
- [GITHUB_SECRETS_REFERENCE](GITHUB_SECRETS_REFERENCE.md)
- [DEPLOYMENT_ARCHITECTURE](DEPLOYMENT_ARCHITECTURE.md)

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
5. Follow the [CONTRIBUTING](CONTRIBUTING.md) guidelines

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
4. Review [CONTRIBUTING](CONTRIBUTING.md) for contribution guidelines
5. Open an issue on GitHub if documentation is missing or unclear

---

**Last Updated**: December 2024
**Maintained By**: OnePageAuthor API Team
