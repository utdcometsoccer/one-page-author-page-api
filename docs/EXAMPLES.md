# GitHub Secrets Initialization - Usage Examples

This document provides practical examples of using the `Initialize-GitHubSecrets.ps1` script.

**Note:** This script uses **GitHub CLI (`gh`)** to set repository secrets. The script can be run directly with PowerShell, or optionally via NPM wrappers.

## Prerequisites Check

Before running any examples, ensure you have GitHub CLI installed and authenticated:

```powershell
# Check if GitHub CLI is installed (REQUIRED)
gh --version

# Check if authenticated (REQUIRED)
gh auth status

# Authenticate if needed
gh auth login
```

## Example 1: Interactive Mode - Minimal Setup

Setting up only the required secrets for basic deployment:

```powershell
.\Scripts\Initialize-GitHubSecrets.ps1 -Interactive
```

**Sample interaction:**

```
═══════════════════════════════════════════════════════
  Core Infrastructure
═══════════════════════════════════════════════════════

ISW_RESOURCE_GROUP (Required)
  Description: Azure Resource Group name
  Example: rg-onepageauthor-prod
  Enter value: rg-myproject-prod

ISW_LOCATION (Required)
  Description: Azure region (e.g., eastus, westus2)
  Example: eastus
  Enter value: eastus

ISW_BASE_NAME (Required)
  Description: Base name for all resources
  Example: onepageauthor
  Enter value: myproject

AZURE_CREDENTIALS (Required)
  Description: Service Principal credentials (JSON format)
  Example: {"clientId":"xxx","clientSecret":"xxx","subscriptionId":"xxx","tenantId":"xxx"}
  ⚠ This is a sensitive value - input will be hidden
  Enter value: [input hidden]

═══════════════════════════════════════════════════════
  Cosmos DB (Required)
═══════════════════════════════════════════════════════

COSMOSDB_CONNECTION_STRING (Required)
  ...
```

## Example 2: Configuration File - Complete Setup

**Step 1: Create configuration file**

```powershell
# Copy template
Copy-Item secrets-template.json secrets.json

# Edit with your values
notepad secrets.json  # or code secrets.json for VS Code
```

**Step 2: Sample `secrets.json` for production:**

```json
{
  "_comment": "Production GitHub Secrets",
  
  "ISW_RESOURCE_GROUP": "rg-onepageauthor-prod",
  "ISW_LOCATION": "eastus",
  "ISW_BASE_NAME": "onepageauthor",
  "AZURE_CREDENTIALS": "{\"clientId\":\"12345678-1234-1234-1234-123456789012\",\"clientSecret\":\"your-secret\",\"subscriptionId\":\"12345678-1234-1234-1234-123456789012\",\"tenantId\":\"12345678-1234-1234-1234-123456789012\"}",
  
  "COSMOSDB_CONNECTION_STRING": "AccountEndpoint=https://prod-cosmos.documents.azure.com:443/;AccountKey=your-key;",
  "COSMOSDB_ENDPOINT_URI": "https://prod-cosmos.documents.azure.com:443/",
  "COSMOSDB_PRIMARY_KEY": "your-primary-key==",
  "COSMOSDB_DATABASE_ID": "OnePageAuthorDb",
  
  "AAD_TENANT_ID": "12345678-1234-1234-1234-123456789012",
  "AAD_AUDIENCE": "api://onepageauthor-api",
  
  "AZURE_STORAGE_CONNECTION_STRING": "DefaultEndpointsProtocol=https;AccountName=prodstorage;AccountKey=your-key;EndpointSuffix=core.windows.net",
  
  "STRIPE_API_KEY": "sk_live_your_live_stripe_key",
  "STRIPE_WEBHOOK_SECRET": "whsec_your_webhook_secret"
}
```

**Step 3: Run the script:**

```powershell
.\Scripts\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.json
```

**Output:**

```
═══════════════════════════════════════════════════════
  Loading Secrets from Config File
═══════════════════════════════════════════════════════

✓ Loaded 12 secrets from config file

═══════════════════════════════════════════════════════
  Confirmation
═══════════════════════════════════════════════════════

ℹ Ready to set 12 GitHub secrets
Continue? (y/n): y

═══════════════════════════════════════════════════════
  Setting GitHub Secrets
═══════════════════════════════════════════════════════

✓ Set ISW_RESOURCE_GROUP = rg-onepageauthor-prod
✓ Set ISW_LOCATION = eastus
✓ Set ISW_BASE_NAME = onepageauthor
✓ Set AZURE_CREDENTIALS (value hidden)
✓ Set COSMOSDB_CONNECTION_STRING (value hidden)
...
```

## Example 3: Development Environment Setup

**development-secrets.json:**

```json
{
  "ISW_RESOURCE_GROUP": "rg-onepageauthor-dev",
  "ISW_LOCATION": "westus2",
  "ISW_BASE_NAME": "opadev",
  "AZURE_CREDENTIALS": "{...dev service principal...}",
  
  "COSMOSDB_ENDPOINT_URI": "https://localhost:8081/",
  "COSMOSDB_PRIMARY_KEY": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
  "COSMOSDB_DATABASE_ID": "DevDb",
  
  "STRIPE_API_KEY": "sk_test_your_test_stripe_key",
  "STRIPE_WEBHOOK_SECRET": "whsec_test_webhook_secret"
}
```

```powershell
.\Scripts\Initialize-GitHubSecrets.ps1 -ConfigFile development-secrets.json
```

## Example 4: Using NPM Scripts

```bash
# Interactive mode
npm run init:secrets:interactive

# Config file mode
npm run init:secrets -- -ConfigFile secrets.json

# Display help
npm run init:secrets:help
```

## Example 5: Text File Format (Legacy)

**secrets.txt:**

```
ISW_RESOURCE_GROUP=rg-onepageauthor-prod
ISW_LOCATION=eastus
ISW_BASE_NAME=onepageauthor
COSMOSDB_ENDPOINT_URI=https://prod-cosmos.documents.azure.com:443/
COSMOSDB_PRIMARY_KEY=your-key-here
COSMOSDB_DATABASE_ID=OnePageAuthorDb
STRIPE_API_KEY=sk_live_your_key
```

```powershell
.\Scripts\Initialize-GitHubSecrets.ps1 -SecretsFile secrets.txt
```

## Example 6: Updating Existing Secrets

To update secrets (e.g., after key rotation):

```powershell
# Method 1: Interactive - answer only the secrets you want to update
.\Scripts\Initialize-GitHubSecrets.ps1 -Interactive

# Method 2: Config file with only updated values
# update-secrets.json:
{
  "STRIPE_API_KEY": "sk_live_new_rotated_key",
  "COSMOSDB_PRIMARY_KEY": "new-rotated-key=="
}

.\Scripts\Initialize-GitHubSecrets.ps1 -ConfigFile update-secrets.json
```

## Example 7: Minimal Required Secrets Only

For a minimal deployment (no Stripe, no external APIs):

**minimal-secrets.json:**

```json
{
  "ISW_RESOURCE_GROUP": "rg-minimal",
  "ISW_LOCATION": "eastus",
  "ISW_BASE_NAME": "minimal",
  "AZURE_CREDENTIALS": "{...}",
  
  "COSMOSDB_CONNECTION_STRING": "...",
  "COSMOSDB_ENDPOINT_URI": "...",
  "COSMOSDB_PRIMARY_KEY": "...",
  "COSMOSDB_DATABASE_ID": "MinimalDb"
}
```

## Example 8: Full Featured Deployment

Including all optional features:

**full-secrets.json:**

```json
{
  "Core Infrastructure": {},
  "ISW_RESOURCE_GROUP": "rg-onepageauthor-prod",
  "ISW_LOCATION": "eastus",
  "ISW_BASE_NAME": "onepageauthor",
  "AZURE_CREDENTIALS": "{...}",
  
  "Cosmos DB": {},
  "COSMOSDB_CONNECTION_STRING": "...",
  "COSMOSDB_ENDPOINT_URI": "...",
  "COSMOSDB_PRIMARY_KEY": "...",
  "COSMOSDB_DATABASE_ID": "OnePageAuthorDb",
  
  "Azure AD": {},
  "AAD_TENANT_ID": "...",
  "AAD_AUDIENCE": "...",
  "AAD_CLIENT_ID": "...",
  
  "Storage & Payment": {},
  "AZURE_STORAGE_CONNECTION_STRING": "...",
  "STRIPE_API_KEY": "sk_live_...",
  "STRIPE_WEBHOOK_SECRET": "whsec_...",
  
  "Domain Management": {},
  "AZURE_SUBSCRIPTION_ID": "...",
  "AZURE_DNS_RESOURCE_GROUP": "...",
  "ISW_DNS_ZONE_NAME": "example.com",
  
  "External APIs": {},
  "AMAZON_PRODUCT_ACCESS_KEY": "AKIA...",
  "AMAZON_PRODUCT_SECRET_KEY": "...",
  "AMAZON_PRODUCT_PARTNER_TAG": "mytag-20",
  "AMAZON_PRODUCT_REGION": "us-east-1",
  "AMAZON_PRODUCT_MARKETPLACE": "www.amazon.com",
  "PENGUIN_RANDOM_HOUSE_API_KEY": "...",
  "PENGUIN_RANDOM_HOUSE_API_DOMAIN": "PRH.US"
}
```

## Example 9: Verifying Secrets After Setup

```powershell
# List all secrets
gh secret list

# Expected output:
# COSMOSDB_ENDPOINT_URI         Updated 2025-12-10
# COSMOSDB_PRIMARY_KEY          Updated 2025-12-10
# ISW_RESOURCE_GROUP            Updated 2025-12-10
# STRIPE_API_KEY                Updated 2025-12-10
# ...

# Check if specific secret exists
gh secret list | Select-String "STRIPE_API_KEY"

# View in browser
$repoUrl = gh repo view --json url -q .url
Start-Process "$repoUrl/settings/secrets/actions"
```

## Example 10: Troubleshooting

**Problem: Script won't run**

```powershell
# Check PowerShell execution policy
Get-ExecutionPolicy

# If needed, set it (as Administrator)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Problem: GitHub CLI not authenticated**

```powershell
# Check status
gh auth status

# Login
gh auth login

# Use a token
gh auth login --with-token < token.txt
```

**Problem: Permission denied when setting secrets**

```powershell
# Check your repository permissions
gh repo view --json name,owner,viewerPermission

# You need "admin" or "write" permission with secrets access
```

## Security Best Practices

### ✅ DO

- Use different secrets for dev/staging/prod
- Rotate secrets regularly
- Use test/sandbox API keys for non-production
- Keep secrets.json out of version control (already in .gitignore)
- Use `sk_test_*` for Stripe in development
- Delete secrets.json after use

### ❌ DON'T

- Commit secrets to git
- Share secrets via email or chat
- Use production secrets in development
- Hard-code secrets in your application
- Leave secrets.json on shared drives

## Additional Resources

- [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) - Complete setup guide
- [GITHUB_SECRETS_CONFIGURATION.md](docs/GITHUB_SECRETS_CONFIGURATION.md) - Secret reference
- [DEVELOPMENT_SCRIPTS.md](docs/DEVELOPMENT_SCRIPTS.md) - All development scripts
- [GitHub CLI Documentation](https://cli.github.com/manual/)
