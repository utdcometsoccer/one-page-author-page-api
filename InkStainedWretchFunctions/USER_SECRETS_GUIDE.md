# Moving Secrets to .NET User Secrets

This guide explains how to move sensitive configuration data from `local.settings.json` to .NET User Secrets for better security.

## 🎯 Why Use User Secrets?

- **Security**: Secrets are stored outside your source code and project directory
- **Team Safety**: No risk of accidentally committing sensitive data to version control
- **Environment Isolation**: Development secrets are separate from production configuration
- **Azure Functions Compatibility**: Works seamlessly with Azure Functions local development

## 🚀 Quick Start

### Step 1: Run the Migration Script

```powershell
# Navigate to your project directory
cd InkStainedWretchFunctions

# Run the migration script (with preview)
.\MoveSecretsToUserSecrets.ps1 -WhatIf

# Run the actual migration
.\MoveSecretsToUserSecrets.ps1
```

### Step 2: Verify the Migration

```powershell
# View your secrets
dotnet user-secrets list

# Test your application
func start
```

## 📋 What Gets Moved

### Secrets (Moved to User Secrets)
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB access key
- `COSMOSDB_CONNECTION_STRING` - Full Cosmos DB connection string
- `CosmosDBConnection` - Alternative connection string format
- `PENGUIN_RANDOM_HOUSE_API_KEY` - Penguin Random House API key
- `AMAZON_PRODUCT_ACCESS_KEY` - Amazon Product API access key
- `AMAZON_PRODUCT_SECRET_KEY` - Amazon Product API secret key
- `AAD_CLIENT_ID` - Azure AD application client ID
- `AAD_TENANT_ID` - Azure AD tenant ID
- `GOOGLE_CLOUD_PROJECT_ID` - Google Cloud project identifier

### Public Configuration (Remains in local.settings.json)
- API URLs and endpoints
- Region settings
- Feature flags
- Testing configuration
- Non-sensitive Azure resource names

## 🛠️ Manual Setup (Alternative)

If you prefer to set up User Secrets manually:

### 1. Initialize User Secrets
```bash
dotnet user-secrets init
```

### 2. Add Your Secrets
```bash
# Example: Add Cosmos DB key
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-actual-cosmos-key-here"

# Add other secrets...
dotnet user-secrets set "PENGUIN_RANDOM_HOUSE_API_KEY" "your-penguin-api-key"
dotnet user-secrets set "AMAZON_PRODUCT_ACCESS_KEY" "your-amazon-access-key"
dotnet user-secrets set "AMAZON_PRODUCT_SECRET_KEY" "your-amazon-secret-key"
```

### 3. Update Program.cs
Add this code to your `Program.cs` after `var builder = FunctionsApplication.CreateBuilder(args);`:

```csharp
// Add User Secrets support for development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
```

And add this at the end of the file:
```csharp
// Program class needed for User Secrets generic type parameter
public partial class Program { }
```

## 📁 File Locations

### User Secrets Storage
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\{user-secrets-id}\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/{user-secrets-id}/secrets.json`

### Project Files
- **User Secrets ID**: Stored in your `.csproj` file as `<UserSecretsId>`
- **Public Config**: Remains in `local.settings.json`

## 🔧 Managing User Secrets

### View All Secrets
```bash
dotnet user-secrets list
```

### Add a Secret
```bash
dotnet user-secrets set "KEY_NAME" "secret-value"
```

### Remove a Secret
```bash
dotnet user-secrets remove "KEY_NAME"
```

### Clear All Secrets
```bash
dotnet user-secrets clear
```

## 🏗️ Production Deployment

User Secrets are only for **development**. For production:

1. **Azure Functions**: Use Application Settings in the Azure Portal
2. **Environment Variables**: Set via deployment pipeline or hosting environment
3. **Azure Key Vault**: For maximum security in production environments

The configuration system will automatically use:
1. User Secrets (development only)
2. Environment Variables
3. local.settings.json (local development fallback)

## 🧪 Testing Scenarios

After migration, your testing scenarios will still work:

```powershell
# Switch to testing configuration (secrets remain in user secrets)
.\Testing\SwitchTestConfig.ps1 -Scenario 1

# Run tests
.\Testing\RunTests.ps1 -Scenario 1 -DomainName "test.example.com"
```

## 🔒 Security Best Practices

1. **Never commit secrets.json** to version control (it's outside your project anyway)
2. **Use different secrets for different environments** (dev, staging, prod)
3. **Rotate secrets regularly** in production
4. **Use Azure Key Vault** for production secrets
5. **Document required secrets** for team members

## 🚨 Troubleshooting

### "Configuration value not found"
- Check if the secret exists: `dotnet user-secrets list`
- Verify the key name matches exactly (case-sensitive)
- Ensure User Secrets is configured in Program.cs

### "User Secrets not initialized"
```bash
dotnet user-secrets init
```

### "Program class not found"
Add to the end of Program.cs:
```csharp
public partial class Program { }
```

### Missing Microsoft.Extensions.Configuration.UserSecrets
```bash
dotnet add package Microsoft.Extensions.Configuration.UserSecrets
```

## 📖 Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Functions Configuration](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection#configuration)
- [Azure Key Vault Integration](https://docs.microsoft.com/en-us/azure/key-vault/)

## 🆘 Getting Help

If you encounter issues:

1. Run with `-WhatIf` first to preview changes
2. Check the backup files created by the script
3. Use `dotnet user-secrets list` to verify secrets were added
4. Review the error messages for specific guidance

Remember: User Secrets are stored per user, so each developer needs to set up their own secrets on their machine!
