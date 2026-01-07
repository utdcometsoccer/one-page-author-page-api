# GitHub Secrets Configuration System

This document provides comprehensive information about the GitHub Secrets configuration system for the OnePageAuthor API Platform. It covers the complete workflow from initial setup to local development and production deployment.

## Overview

The secrets configuration system supports three key workflows:

1. **GitHub Secrets Setup** - Configure secrets for CI/CD deployment
2. **Local Development** - Use dotnet user-secrets for secure local development
3. **Configuration Management** - Keep secrets files up-to-date as requirements evolve

## Table of Contents

- [Quick Start](#quick-start)
- [Available Scripts](#available-scripts)
- [Complete Workflow](#complete-workflow)
- [Environment Variables Reference](#environment-variables-reference)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Quick Start

### First Time Setup

1. **Copy the template**
   ```powershell
   Copy-Item secrets-template.json secrets.config.json
   ```

2. **Edit with your values**
   ```powershell
   code secrets.config.json  # or your preferred editor
   ```

3. **Set GitHub Secrets** (for CI/CD)
   ```powershell
   npm run init-secrets -- -ConfigFile secrets.config.json
   ```

4. **Set Local User Secrets** (for development)
   ```powershell
   .\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.config.json
   ```

### Updating Existing Configuration

When new environment variables are added to the platform:

```powershell
# Update your existing secrets file with new variables
.\Update-SecretsConfig.ps1 -SecretsFile secrets.config.json

# Review and fill in the new variables
code secrets.config.json

# Update GitHub secrets
npm run init-secrets -- -ConfigFile secrets.config.json

# Update local user-secrets
.\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.config.json
```

## Available Scripts

### 1. Initialize-GitHubSecrets.ps1

**Purpose**: Configure GitHub repository secrets for CI/CD deployment.

**Usage**:
```powershell
# Interactive mode (prompts for each secret)
.\Scripts\Initialize-GitHubSecrets.ps1 -Interactive

# From configuration file
.\Scripts\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.config.json

# Via NPM scripts
npm run init-secrets                    # Base command
npm run init-secrets:interactive        # Interactive mode
npm run init-secrets:help               # Show help
```

**Features**:
- Validates prerequisites (GitHub CLI, authentication)
- Supports interactive prompts or file-based configuration
- Automatically handles sensitive value masking
- Removes erroneous quotes from values
- Provides detailed progress and confirmation

**Prerequisites**:
- GitHub CLI (`gh`) installed and authenticated
- PowerShell 7+ (Core)
- Repository access with secrets write permission

### 2. Update-SecretsConfig.ps1

**Purpose**: Update an existing secrets configuration file with missing variables from the template.

**Usage**:
```powershell
# Update with default files
.\Update-SecretsConfig.ps1

# Update specific file
.\Update-SecretsConfig.ps1 -SecretsFile my-secrets.json

# Dry run (show what would be added)
.\Update-SecretsConfig.ps1 -DryRun

# Skip backup
.\Update-SecretsConfig.ps1 -BackupOriginal:$false
```

**Features**:
- Compares your secrets file with the latest template
- Adds missing variables with empty values
- Preserves existing values (never overwrites)
- Creates automatic backups
- Maintains JSON structure and formatting

**Use Cases**:
- New environment variables added to the platform
- Migrating from an older secrets file format
- Ensuring your configuration is complete

### 3. Set-DotnetUserSecrets.ps1

**Purpose**: Configure dotnet user-secrets for all Azure Function projects from a configuration file.

**Usage**:
```powershell
# Set for all projects
.\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.config.json

# Set for specific project
.\Set-DotnetUserSecrets.ps1 -ProjectFilter "ImageAPI"

# Dry run (show what would be set)
.\Set-DotnetUserSecrets.ps1 -DryRun

# Force overwrite existing values
.\Set-DotnetUserSecrets.ps1 -Force
```

**Features**:
- Automatically discovers Azure Function projects
- Initializes user-secrets if not already set up
- Filters secrets per project (only sets relevant ones)
- Preserves existing secrets unless `-Force` is used
- Provides detailed operation logging

**Project-Specific Filtering**:
The script intelligently sets only relevant secrets for each project:

- **Common** (all projects): Cosmos DB, Azure AD authentication
- **ImageAPI**: Azure Blob Storage
- **InkStainedWretchStripe**: Stripe API keys
- **InkStainedWretchFunctions**: External APIs, domain management, testing
- **InkStainedWretchesConfig**: Key Vault configuration

### 4. secrets-template.json

**Purpose**: Master template containing all possible configuration variables.

**Structure**:
```json
{
  "_comment": "Descriptive comment",
  "_usage": "Usage instructions",
  "_security": "Security reminder",
  
  "Category Name": {
    "_description": "Category description"
  },
  "VARIABLE_NAME": "default_or_empty_value"
}
```

**Maintenance**:
- Keep this file updated when adding new environment variables
- Use descriptive comments for each category
- Provide sensible defaults where applicable
- Mark required vs optional variables

## Complete Workflow

### Initial Project Setup

```powershell
# 1. Create your secrets file from template
Copy-Item secrets-template.json secrets.config.json

# 2. Fill in your values
code secrets.config.json

# 3. Configure GitHub Secrets (for CI/CD)
npm run init-secrets -- -ConfigFile secrets.config.json

# 4. Configure Local Development (user-secrets)
.\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.config.json

# 5. Verify local configuration
dotnet user-secrets list --project ImageAPI
```

### Adding New Environment Variables

When you add a new feature that requires configuration:

```powershell
# 1. Update secrets-template.json
# Add new variables to the appropriate section

# 2. Update Initialize-GitHubSecrets.ps1
# Add new secret definitions to $secretDefinitions

# 3. Update your secrets.config.json
.\Update-SecretsConfig.ps1

# 4. Fill in values for new variables
code secrets.config.json

# 5. Update GitHub Secrets
npm run init-secrets -- -ConfigFile secrets.config.json

# 6. Update local user-secrets
.\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.config.json
```

### Environment-Specific Configuration

**Development**:
```powershell
# Use test credentials
Copy-Item secrets-template.json secrets.dev.json
# Edit with test/sandbox credentials
.\Set-DotnetUserSecrets.ps1 -ConfigFile secrets.dev.json
```

**Production**:
```powershell
# Use production credentials
Copy-Item secrets-template.json secrets.prod.json
# Edit with production credentials
npm run init-secrets -- -ConfigFile secrets.prod.json
```

## Environment Variables Reference

### Core Infrastructure (Required)

| Variable | Description | Example | Used By |
|----------|-------------|---------|---------|
| `COSMOSDB_ENDPOINT_URI` | Cosmos DB endpoint | `https://account.documents.azure.com:443/` | All |
| `COSMOSDB_PRIMARY_KEY` | Cosmos DB key | `xxx...==` | All |
| `COSMOSDB_DATABASE_ID` | Database name | `OnePageAuthorDb` | All |
| `COSMOSDB_CONNECTION_STRING` | Full connection | `AccountEndpoint=...;AccountKey=...;` | All |

### Authentication (Optional)

| Variable | Description | Example | Used By |
|----------|-------------|---------|---------|
| `AAD_TENANT_ID` | Azure AD tenant | `xxxxxxxx-xxxx-...` | All |
| `AAD_AUDIENCE` | Client/App ID | `xxxxxxxx-xxxx-...` | All |
| `AAD_CLIENT_ID` | Client ID | `xxxxxxxx-xxxx-...` | All |
| `AAD_AUTHORITY` | Authority URL | `https://login.microsoftonline.com/{tenant}/v2.0` | All |

### Storage (Required for ImageAPI)

| Variable | Description | Example | Used By |
|----------|-------------|---------|---------|
| `AZURE_STORAGE_CONNECTION_STRING` | Blob storage | `DefaultEndpointsProtocol=https;...` | ImageAPI |

### Payments (Required for Stripe)

| Variable | Description | Example | Used By |
|----------|-------------|---------|---------|
| `STRIPE_API_KEY` | Stripe secret key | `sk_test_...` or `sk_live_...` | InkStainedWretchStripe |
| `STRIPE_WEBHOOK_SECRET` | Webhook secret | `whsec_...` | InkStainedWretchStripe |

### External APIs (Optional)

**Amazon Product API**:
- `AMAZON_PRODUCT_ACCESS_KEY` - AWS access key
- `AMAZON_PRODUCT_SECRET_KEY` - AWS secret key
- `AMAZON_PRODUCT_PARTNER_TAG` - Associates tag
- `AMAZON_PRODUCT_REGION` - AWS region
- `AMAZON_PRODUCT_MARKETPLACE` - Marketplace domain
- `AMAZON_PRODUCT_API_ENDPOINT` - API endpoint

**Penguin Random House**:
- `PENGUIN_RANDOM_HOUSE_API_URL` - Base URL
- `PENGUIN_RANDOM_HOUSE_API_KEY` - API key
- `PENGUIN_RANDOM_HOUSE_API_DOMAIN` - Domain
- `PENGUIN_RANDOM_HOUSE_SEARCH_API` - Search endpoint
- `PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API` - Author titles endpoint
- `PENGUIN_RANDOM_HOUSE_URL` - Website URL

### Domain Management (Optional)

- `AZURE_SUBSCRIPTION_ID` - Azure subscription
- `AZURE_DNS_RESOURCE_GROUP` - DNS resource group
- `AZURE_RESOURCE_GROUP_NAME` - Front Door resource group
- `AZURE_FRONTDOOR_PROFILE_NAME` - Front Door profile
- `GOOGLE_CLOUD_PROJECT_ID` - Google Cloud project
- `GOOGLE_DOMAINS_LOCATION` - Domain location

### Testing (Optional)

- `TESTING_MODE` - Enable testing mode
- `MOCK_AZURE_INFRASTRUCTURE` - Mock Azure calls
- `MOCK_GOOGLE_DOMAINS` - Mock Google Domains
- `MOCK_STRIPE_PAYMENTS` - Mock Stripe
- `STRIPE_TEST_MODE` - Use Stripe test mode
- Plus additional testing configuration...

See [secrets-template.json](../secrets-template.json) for complete list.

## Best Practices

### Security

1. **Never commit secrets files**
   - `secrets.config.json` is in `.gitignore`
   - User-secrets are stored outside project directory
   - Always use `.gitignore` for any custom secrets files

2. **Use different credentials per environment**
   - Development: Test/sandbox credentials
   - Production: Live credentials with proper access controls
   - Keep secrets separate and rotated regularly

3. **Principle of least privilege**
   - Only set secrets needed for your work
   - Use minimal Azure service principal permissions
   - Limit access to production secrets

### Development Workflow

1. **Use user-secrets for local development**
   - Never put secrets in `local.settings.json`
   - User-secrets are automatically loaded in development
   - They're stored securely in your user profile

2. **Keep secrets files up-to-date**
   - Run `Update-SecretsConfig.ps1` regularly
   - Review template changes in pull requests
   - Update local and GitHub secrets together

3. **Document custom configuration**
   - Add comments to your secrets file
   - Note any non-standard values
   - Document environment-specific settings

### Maintenance

1. **Regular audits**
   - Review all secrets quarterly
   - Remove unused secrets
   - Rotate sensitive credentials

2. **Template maintenance**
   - Update template when adding features
   - Keep examples current and helpful
   - Document defaults and optional values

3. **Script updates**
   - Test scripts after PowerShell updates
   - Validate against new .NET SDK versions
   - Update prerequisites as needed

## Troubleshooting

### GitHub CLI Issues

**Error**: "GitHub CLI is not authenticated"
```powershell
# Solution: Authenticate
gh auth login
```

**Error**: "Failed to set secret"
- Check repository permissions
- Verify you have Secrets write access
- Try refreshing authentication: `gh auth refresh`

### Dotnet User-Secrets Issues

**Error**: ".NET SDK is not installed"
```powershell
# Solution: Install .NET SDK
# Windows: winget install Microsoft.DotNet.SDK.9
# macOS: brew install dotnet
# Linux: See https://dotnet.microsoft.com/download
```

**Error**: "User-secrets not initialized"
```powershell
# Solution: Initialize manually
dotnet user-secrets init --project ImageAPI/ImageAPI.csproj
```

**Verify user-secrets**:
```powershell
# List secrets for a project
dotnet user-secrets list --project ImageAPI/ImageAPI.csproj

# Remove a specific secret
dotnet user-secrets remove "SECRET_NAME" --project ImageAPI/ImageAPI.csproj

# Clear all secrets
dotnet user-secrets clear --project ImageAPI/ImageAPI.csproj
```

### Configuration Issues

**Error**: "Configuration file not found"
- Verify file path is correct
- Check file is in the repository root
- Ensure file extension is `.json`

**Error**: "Failed to parse configuration file"
- Validate JSON syntax
- Check for missing commas or brackets
- Use a JSON validator or formatter

**Missing values in application**:
1. Verify secrets are set: `dotnet user-secrets list --project [project]`
2. Check variable name spelling
3. Restart the Function App or development environment
4. Verify the secret is relevant for that project

### Script Issues

**Error**: PowerShell execution policy
```powershell
# Solution: Set execution policy (run as administrator)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Error**: "Cannot run script"
- Check PowerShell version: `pwsh --version` (need 7+)
- Verify file path
- Try running with explicit PowerShell: `pwsh -File .\script.ps1`

## Additional Resources

### Documentation
- [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) - Initial setup guide
- [GITHUB_SECRETS_REFERENCE.md](GITHUB_SECRETS_REFERENCE.md) - Complete reference
- [ConfigurationValidation.md](ConfigurationValidation.md) - Validation patterns
- [ConfigurationMaskingStandardization.md](ConfigurationMaskingStandardization.md) - Security masking

### External Resources
- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Functions Configuration](https://learn.microsoft.com/en-us/azure/azure-functions/functions-app-settings)
- [GitHub CLI Manual](https://cli.github.com/manual/)

### Getting Help

For issues or questions:
1. Check this documentation and related guides
2. Review error messages and troubleshooting section
3. Verify prerequisites are met
4. Open an issue on the repository with:
   - Script name and command used
   - Error message (redact any secrets!)
   - PowerShell/dotnet versions
   - Operating system

---

**Last Updated**: January 2026  
**Maintained By**: OnePageAuthor Team
