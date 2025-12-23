# GitHub Secrets Setup Guide

This guide explains how to use the `Initialize-GitHubSecrets.ps1` script to configure GitHub repository secrets for CI/CD deployment of the OnePageAuthor API Platform.

**Primary Tool:** The script uses **GitHub CLI (`gh`)** to set repository secrets. NPM scripts are optional convenience wrappers.

## Quick Start

### Prerequisites

**Required:**
1. **GitHub CLI (gh)** - Primary tool for setting secrets
   - Windows: `winget install --id GitHub.cli`
   - macOS: `brew install gh`
   - Linux: See [installation guide](https://github.com/cli/cli/blob/trunk/docs/install_linux.md)

2. **Authenticate with GitHub CLI**
   ```bash
   gh auth login
   ```

3. **PowerShell 7+**
   ```bash
   pwsh --version
   ```

**Optional:**
- **npm** - Only needed if using NPM script wrappers

### Method 1: Interactive Mode (Recommended for First-Time Setup)

This mode prompts you for each secret value with helpful descriptions and examples.

```powershell
# Direct PowerShell execution (recommended)
.\Initialize-GitHubSecrets.ps1 -Interactive

# Or using NPM wrapper (optional)
npm run init:secrets:interactive
```

**What to expect:**
- The script will check prerequisites (GitHub CLI, authentication)
- You'll be prompted for each secret, organized by category
- Required secrets are marked as "(Required)"
- Optional secrets are marked as "(Optional)" - press Enter to skip
- Sensitive values (API keys, passwords) will have hidden input
- You'll see a confirmation before secrets are set
- A summary shows how many secrets were successfully configured

### Method 2: Configuration File (Recommended for Production)

This mode reads secret values from a JSON file, which is useful for automation or when you have many secrets to configure.

**Step 1: Copy the template**
```powershell
Copy-Item secrets-template.json secrets.json
```

**Step 2: Edit secrets.json with your values**
```powershell
# Using VS Code
code secrets.json

# Or any text editor
notepad secrets.json  # Windows
vim secrets.json      # Linux/macOS
```

**Step 3: Run the script**
```powershell
# Direct PowerShell execution (recommended)
.\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.json

# Or using NPM wrapper (optional)
npm run init:secrets -- -ConfigFile secrets.json
```

**Security Note:** The `secrets.json` file is automatically excluded from git via `.gitignore`. Never commit this file!

### Method 3: Text File (Legacy Format)

For simple key=value format files:

```powershell
.\Initialize-GitHubSecrets.ps1 -SecretsFile my-secrets.txt
```

Format of `my-secrets.txt`:
```
ISW_RESOURCE_GROUP=rg-onepageauthor-prod
ISW_LOCATION=eastus
COSMOSDB_ENDPOINT_URI=https://your-account.documents.azure.com:443/
STRIPE_API_KEY=sk_test_your_key_here
```

## Secret Categories

### Required for All Deployments

#### Core Infrastructure
- `ISW_RESOURCE_GROUP` - Azure Resource Group name
- `ISW_LOCATION` - Azure region (e.g., "eastus", "westus2")
- `ISW_BASE_NAME` - Base name for all resources
- `AZURE_CREDENTIALS` - Service Principal credentials (JSON format)

#### Cosmos DB
- `COSMOSDB_CONNECTION_STRING` - Full connection string
- `COSMOSDB_ENDPOINT_URI` - Cosmos DB endpoint URL
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB primary key
- `COSMOSDB_DATABASE_ID` - Database name (typically "OnePageAuthorDb")

### Optional - Azure AD Authentication
- `AAD_TENANT_ID` - Azure AD tenant ID
- `AAD_AUDIENCE` - Azure AD client ID / audience
- `AAD_CLIENT_ID` - Azure AD client ID
- `OPEN_ID_CONNECT_METADATA_URL` - OpenID Connect metadata URL for JWT validation

### Optional - ImageAPI (if using image features)
- `AZURE_STORAGE_CONNECTION_STRING` - Azure Blob Storage connection

### Optional - InkStainedWretchStripe (if using payment features)
- `STRIPE_API_KEY` - Stripe secret key (sk_test_... or sk_live_...)
- `STRIPE_WEBHOOK_SECRET` - Stripe webhook signing secret

### Optional - Domain Management
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID
- `AZURE_DNS_RESOURCE_GROUP` - Resource group for DNS zones
- `AZURE_RESOURCE_GROUP_NAME` - Resource group name for Azure Front Door
- `AZURE_FRONTDOOR_PROFILE_NAME` - Azure Front Door profile name
- `ISW_DNS_ZONE_NAME` - DNS zone name
- `GOOGLE_CLOUD_PROJECT_ID` - Google Cloud project ID
- `GOOGLE_DOMAINS_LOCATION` - Location for domain operations

### Optional - External APIs
- `AMAZON_PRODUCT_ACCESS_KEY` - AWS access key ID
- `AMAZON_PRODUCT_SECRET_KEY` - AWS secret access key
- `AMAZON_PRODUCT_PARTNER_TAG` - Amazon Associates tracking ID
- `AMAZON_PRODUCT_REGION` - AWS region
- `AMAZON_PRODUCT_MARKETPLACE` - Target marketplace
- `PENGUIN_RANDOM_HOUSE_API_KEY` - PRH API key
- `PENGUIN_RANDOM_HOUSE_API_DOMAIN` - PRH API domain

### Optional - Azure Key Vault (for secure secret management)
- `KEY_VAULT_URL` - Azure Key Vault URL
- `USE_KEY_VAULT` - Feature flag to enable Key Vault (true/false)

### Optional - Referral Program
- `REFERRAL_BASE_URL` - Base URL for generating referral links

### Optional - Testing Configuration
- `TESTING_MODE` - Enable testing mode (true/false)
- `MOCK_AZURE_INFRASTRUCTURE` - Mock Azure infrastructure operations (true/false)
- `MOCK_GOOGLE_DOMAINS` - Mock Google Domains API calls (true/false)
- `MOCK_STRIPE_PAYMENTS` - Mock Stripe payment operations (true/false)
- `STRIPE_TEST_MODE` - Use Stripe test mode (true/false)
- `MOCK_EXTERNAL_APIS` - Mock external API calls (true/false)
- `ENABLE_TEST_LOGGING` - Enable detailed test logging (true/false)
- `TEST_SCENARIO` - Test scenario identifier
- `MAX_TEST_COST_LIMIT` - Maximum cost limit for testing operations (USD)
- `TEST_DOMAIN_SUFFIX` - Test domain suffix for testing
- `SKIP_DOMAIN_PURCHASE` - Skip actual domain purchases during testing (true/false)

## Verification

After running the script, verify your secrets were set:

```bash
# List all secrets
gh secret list

# Check a specific secret exists (won't show the value)
gh secret list | grep COSMOSDB_ENDPOINT_URI
```

You can also view them in the GitHub web interface:
- Navigate to your repository on GitHub
- Go to **Settings** → **Secrets and variables** → **Actions**

## Getting Secret Values

### Azure Portal

**Cosmos DB:**
1. Go to Azure Portal → Cosmos DB → Your account
2. Navigate to **Keys** section
3. Copy the required values:
   - URI (COSMOSDB_ENDPOINT_URI)
   - Primary Key (COSMOSDB_PRIMARY_KEY)
   - Primary Connection String (COSMOSDB_CONNECTION_STRING)

**Azure Storage:**
1. Go to Azure Portal → Storage accounts → Your account
2. Navigate to **Access keys** section
3. Copy **Connection string**

**Azure AD:**
1. Go to Azure Portal → Microsoft Entra ID
2. **Overview** → Copy Tenant ID
3. **App registrations** → Your app → Copy Application (client) ID

### Stripe Dashboard

1. Go to [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to **Developers** → **API keys**
3. Copy the **Secret key** (starts with `sk_test_` or `sk_live_`)
4. For webhook secret:
   - Go to **Developers** → **Webhooks**
   - Select your endpoint
   - Copy the **Signing secret**

### AWS Console

1. Go to [AWS Console](https://console.aws.amazon.com)
2. Navigate to **Security Credentials**
3. Create a new **Access key**
4. Save both Access Key ID and Secret Access Key immediately

### Azure Service Principal

Create Azure credentials for deployment:

```bash
az ad sp create-for-rbac --name "github-actions-onepageauthor" \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/{resource-group} \
  --sdk-auth
```

Copy the entire JSON output to use as `AZURE_CREDENTIALS`.

## Troubleshooting

### "GitHub CLI is not authenticated"

**Solution:** Run `gh auth login` and follow the prompts.

### "Not in a git repository"

**Solution:** Make sure you're running the script from within the repository directory.

### "Failed to set secret"

**Possible causes:**
1. You don't have permission to set secrets in the repository
2. Secret name contains invalid characters
3. Network issues connecting to GitHub

**Solution:** Check your GitHub permissions and repository access.

### Secret value contains special characters

**Solution:** When using a config file, ensure special characters in JSON are properly escaped:
- `"` → `\"`
- `\` → `\\`
- newlines → `\n`

For interactive mode, special characters are handled automatically.

## Security Best Practices

1. ✅ **Never commit secrets to source control**
   - `secrets.json` is already in `.gitignore`
   - Don't add secrets to code, comments, or commit messages

2. ✅ **Use different credentials for environments**
   - Development: Use test/sandbox credentials (e.g., `sk_test_...` for Stripe)
   - Production: Use live credentials with proper access controls

3. ✅ **Rotate secrets regularly**
   - Change API keys and passwords periodically
   - Update GitHub Secrets when you rotate credentials
   - Use the script to quickly update secrets

4. ✅ **Principle of least privilege**
   - Grant minimal permissions to service principals
   - Only set secrets that are actually needed for your deployment

5. ✅ **Monitor secret usage**
   - Review GitHub Actions logs for authentication issues
   - Set up alerts for failed deployments
   - Audit secret access regularly

## Additional Resources

- [GITHUB_SECRETS_CONFIGURATION.md](docs/GITHUB_SECRETS_CONFIGURATION.md) - Complete secret reference
- [ConfigurationValidation.md](docs/ConfigurationValidation.md) - Configuration validation patterns
- [DEVELOPMENT_SCRIPTS.md](docs/DEVELOPMENT_SCRIPTS.md) - All development scripts
- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Functions Configuration](https://learn.microsoft.com/en-us/azure/azure-functions/functions-app-settings)

## Getting Help

```powershell
# Display detailed help
.\Initialize-GitHubSecrets.ps1 -Help

# Via NPM
npm run init:secrets:help
```

For issues or questions, please open an issue on the repository.
