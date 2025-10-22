# 🔐 InkStainedWretchStripe User Secrets Setup

## ⚠️ IMPORTANT SECURITY NOTICE
This project requires sensitive configuration values that should NOT be stored in source control.

## 🚀 Setup Instructions

### 1. Initialize User Secrets
```bash
cd InkStainedWretchStripe
dotnet user-secrets init
```

### 2. Add Required Secrets
Replace the placeholder values with your actual credentials:

```bash
# Stripe API Key (Get from Stripe Dashboard)
dotnet user-secrets set "STRIPE_API_KEY" "sk_test_YOUR_ACTUAL_STRIPE_KEY"

# Cosmos DB Primary Key (Get from Azure Portal)
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "YOUR_ACTUAL_COSMOS_KEY"

# Azure AD Tenant ID (Get from Azure Portal)
dotnet user-secrets set "AAD_TENANT_ID" "YOUR_ACTUAL_TENANT_ID"

# Azure AD Client ID (Get from Azure Portal)
dotnet user-secrets set "AAD_CLIENT_ID" "YOUR_ACTUAL_CLIENT_ID"

# Azure AD Audience (Usually same as Client ID)
dotnet user-secrets set "AAD_AUDIENCE" "YOUR_ACTUAL_CLIENT_ID"
```

### 3. Verify Setup
```bash
dotnet user-secrets list
```

You should see your secrets listed (values will be hidden for security).

### 4. Test the Application
```bash
func start
# or
dotnet run
```

## 🏭 Production Deployment

For production deployments, set these values as environment variables or Azure App Settings:

- `STRIPE_API_KEY`
- `COSMOSDB_PRIMARY_KEY` 
- `AAD_TENANT_ID`
- `AAD_CLIENT_ID`
- `AAD_AUDIENCE`

## 📚 Where to Find Your Values

### Stripe API Key
1. Go to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Navigate to Developers > API Keys
3. Copy the "Secret key" (starts with `sk_test_` for test mode)

### Cosmos DB Primary Key
1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to your Cosmos DB account
3. Go to Settings > Keys
4. Copy the "Primary Key"

### Azure AD Values
1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to Azure Active Directory > App registrations
3. Select your app registration
4. Copy the "Application (client) ID" and "Directory (tenant) ID"

## 🔒 Security Best Practices

- ✅ **Never commit secrets to source control**
- ✅ **Use user secrets for local development**
- ✅ **Use environment variables for production**
- ✅ **Use Azure Key Vault for sensitive production data**
- ✅ **Rotate keys regularly**
- ❌ **Don't share secrets via email or chat**
- ❌ **Don't hardcode secrets in your application**

## 🆘 Troubleshooting

### "Configuration value not found" errors
- Make sure you've run `dotnet user-secrets init`
- Verify secrets are set with `dotnet user-secrets list`
- Check that you're in the correct project directory

### Authentication errors
- Verify your Azure AD configuration matches your app registration
- Check that the tenant ID and client ID are correct
- Ensure your app registration has the necessary permissions

### Stripe errors
- Verify you're using the correct API key for your environment (test vs live)
- Check that your Stripe account is active and in good standing