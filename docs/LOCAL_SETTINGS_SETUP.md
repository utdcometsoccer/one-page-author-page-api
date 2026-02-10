# Local Settings Configuration Guide

This guide explains how to configure the `local.settings.json` files that have been created for each Azure Functions project in this solution.

## 🔧 Created Files

The following `local.settings.json` files have been created with placeholder values:

- `ImageAPI/local.settings.json`
- `InkStainedWretchFunctions/local.settings.json`  
- `InkStainedWretchStripe/local.settings.json`
- `function-app/local.settings.json`

## ⚠️ IMPORTANT: Replace Placeholder Values

**These files contain placeholder values that MUST be replaced with your actual configuration before the applications will work.**

### Required Replacements

#### Azure AD / Entra ID Configuration

Replace these placeholders in ALL files:

- `your-tenant-id-here` → Your Azure AD Tenant ID
- `your-client-id-here` → Your application's Client ID  

#### Cosmos DB Configuration  

The default values use the Cosmos DB Emulator. For production or cloud development:

- `COSMOSDB_ENDPOINT_URI`: Replace with your actual Cosmos DB endpoint
- `COSMOSDB_PRIMARY_KEY`: Replace with your actual Cosmos DB primary key
- `COSMOSDB_DATABASE_ID`: Keep as "OnePageAuthor" or change to your database name

#### Stripe Configuration (InkStainedWretchStripe only)

- `sk_test_your-stripe-test-key-here` → Your Stripe test API key
- `whsec_your-webhook-secret-here` → Your Stripe webhook secret

#### External API Keys (InkStainedWretchFunctions only)

- `your-prh-api-key-here` → Penguin Random House API key (optional)
- `your-amazon-access-key-here` → Amazon Product API access key (optional)
- `your-amazon-secret-key-here` → Amazon Product API secret key (optional)
- `your-amazon-partner-tag-here` → Amazon Associates partner tag (optional)

#### Azure Infrastructure (InkStainedWretchFunctions only)

- `your-azure-subscription-id-here` → Your Azure subscription ID
- `your-dns-resource-group-here` → Resource group for DNS zones
- `your-google-project-id-here` → Google Cloud project ID (optional)

## 🚀 Quick Setup Options

### Option 1: Use Cosmos DB Emulator (Recommended for Development)

1. Install [Azure Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
2. Start the emulator
3. Keep the default Cosmos DB settings in the files (they're already configured for the emulator)
4. Only replace the Azure AD placeholders with your actual values

### Option 2: Use dotnet user-secrets (Recommended for Security)

Instead of putting secrets directly in `local.settings.json`, use user secrets:

```bash
# Navigate to each function app directory and set secrets
cd ImageAPI
dotnet user-secrets init
dotnet user-secrets set "AAD_TENANT_ID" "your-actual-tenant-id"
dotnet user-secrets set "AAD_AUDIENCE" "your-actual-client-id"

# Repeat for other projects...
```

### Option 3: Use Environment Variables

Set environment variables instead of modifying the files:

```powershell
$env:AAD_TENANT_ID = "your-actual-tenant-id"  
$env:AAD_AUDIENCE = "your-actual-client-id"
$env:COSMOSDB_ENDPOINT_URI = "your-cosmos-endpoint"
# etc...
```

## 📋 Configuration by Project

### ImageAPI

**Purpose**: Image upload and management
**Required**:

- Azure Storage (default uses emulator)
- Cosmos DB (default uses emulator)
- Azure AD for authentication

### InkStainedWretchFunctions  

**Purpose**: Domain registration and external API integration
**Required**:

- Cosmos DB (default uses emulator)
- Azure AD for authentication
**Optional**:
- Penguin Random House API
- Amazon Product API  
- Azure DNS configuration

### InkStainedWretchStripe

**Purpose**: Stripe payment processing  
**Required**:

- Cosmos DB (default uses emulator)
- Stripe API (test mode by default)
- Azure AD for authentication

### function-app

**Purpose**: Core application functions
**Required**:

- Cosmos DB (default uses emulator)
- Azure AD for authentication

## 🧪 Testing Scenarios

The files are pre-configured for "frontend-safe" testing (no real costs). To change testing scenarios, modify these values in the appropriate files:

- `TEST_SCENARIO`: "frontend-safe", "individual-testing", or "production-test"
- `MOCK_*` settings: Set to "false" to enable real services
- `MAX_TEST_COST_LIMIT`: Adjust based on your testing budget

See [TESTING_SCENARIOS_GUIDE.md](TESTING_SCENARIOS_GUIDE.md) for details.

## 🔒 Security Notes

1. **Never commit these files to git** - They're already in `.gitignore`
2. **Use user secrets for sensitive data** - More secure than local files
3. **Use test keys for development** - Never use production keys locally
4. **Rotate keys regularly** - Especially if they're accidentally exposed

## 🆘 Troubleshooting

### Common Issues

**"Configuration not found" errors**:

- Check that placeholder values have been replaced
- Verify Azure AD tenant/client IDs are correct
- Ensure Cosmos DB emulator is running (if using local setup)

**Authentication failures**:  

- Verify AAD_TENANT_ID and AAD_AUDIENCE are correct
- Check that your Azure AD app registration is configured properly

**Cosmos DB connection errors**:

- Start the Cosmos DB emulator if using local setup
- Verify endpoint URI and primary key if using cloud Cosmos DB

**Stripe errors**:

- Ensure you're using test keys (start with `sk_test_`)
- Verify webhook secret matches your Stripe dashboard

## 📚 Additional Resources

- [Azure Functions local development](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-local)
- [User Secrets in .NET](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Cosmos DB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator)

## 🤝 Getting Help

If you need help configuring these files:

1. Check the main [README.md](README.md) for configuration details
2. Review the project documentation in the `docs/` folder  
3. Ask team members for the actual configuration values
4. Create an issue in the repository if you're stuck

---

**Next Steps**: Replace the placeholder values with your actual configuration and start developing! 🚀
