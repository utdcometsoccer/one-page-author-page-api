# Secrets Configuration Quick Reference

This is a quick reference guide for the GitHub Secrets Configuration system. For comprehensive documentation, see [docs/GITHUB_SECRETS_CONFIGURATION.md](docs/GITHUB_SECRETS_CONFIGURATION.md).

## Quick Commands

### Initial Setup

```powershell
# 1. Create your secrets file from template
Copy-Item secrets-template.json secrets.config.json

# 2. Edit with your values (use your preferred editor)
code secrets.config.json

# 3. Set GitHub Secrets for CI/CD
npm run init-secrets -- -ConfigFile secrets.config.json

# 4. Set local user-secrets for development
npm run set-user-secrets -- -ConfigFile secrets.config.json
```

### Update Existing Configuration

```powershell
# 1. Update secrets file with new variables
npm run update-secrets

# 2. Review and fill in new values
code secrets.config.json

# 3. Update GitHub Secrets
npm run init-secrets -- -ConfigFile secrets.config.json

# 4. Update local user-secrets
npm run set-user-secrets -- -ConfigFile secrets.config.json
```

## Available NPM Scripts

| Script | Description | Example |
|--------|-------------|---------|
| `init-secrets` | Set GitHub Secrets from config file | `npm run init-secrets -- -ConfigFile secrets.json` |
| `init-secrets:interactive` | Interactive prompts for each secret | `npm run init-secrets:interactive` |
| `init-secrets:help` | Show help for Initialize-GitHubSecrets.ps1 | `npm run init-secrets:help` |
| `update-secrets` | Update existing secrets file with new variables | `npm run update-secrets` |
| `update-secrets:dry-run` | Preview changes without modifying file | `npm run update-secrets:dry-run` |
| `set-user-secrets` | Set dotnet user-secrets for all projects | `npm run set-user-secrets` |
| `set-user-secrets:dry-run` | Preview what would be set | `npm run set-user-secrets:dry-run` |

## Direct PowerShell Usage

You can also run the scripts directly with PowerShell:

```powershell
# Initialize GitHub Secrets
.\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.config.json
.\Initialize-GitHubSecrets.ps1 -Interactive

# Update secrets configuration
.\Update-SecretsConfig.ps1
.\Update-SecretsConfig.ps1 -DryRun
.\Update-SecretsConfig.ps1 -SecretsFile my-secrets.json

# Set user-secrets
.\Set-DotnetUserSecrets.ps1
.\Set-DotnetUserSecrets.ps1 -DryRun
.\Set-DotnetUserSecrets.ps1 -ProjectFilter "ImageAPI"
```

## Required Environment Variables

### Core (All Projects)

- `COSMOSDB_ENDPOINT_URI` - Cosmos DB endpoint
- `COSMOSDB_PRIMARY_KEY` - Cosmos DB key
- `COSMOSDB_DATABASE_ID` - Database name (typically "OnePageAuthorDb")
- `COSMOSDB_CONNECTION_STRING` - Full connection string

### Authentication (Optional but Recommended)

- `AAD_TENANT_ID` - Azure AD tenant ID
- `AAD_AUDIENCE` - Application/Client ID
- `AAD_CLIENT_ID` - Client ID (if different from audience)
- `AAD_AUTHORITY` - Authority URL (optional, auto-constructed if not provided)

### ImageAPI

- `AZURE_STORAGE_CONNECTION_STRING` - Azure Blob Storage

### InkStainedWretchStripe

- `STRIPE_API_KEY` - Stripe secret key (sk_test_... or sk_live_...)
- `STRIPE_WEBHOOK_SECRET` - Webhook signing secret

### Azure Communication Services Email (Optional)

- `ACS_CONNECTION_STRING` - Azure Communication Services connection string (Email)
- `ACS_SENDER_ADDRESS` - Sender email address used by ACS Email

### Optional External APIs

- **Amazon Product API**: ACCESS_KEY, SECRET_KEY, PARTNER_TAG, REGION, MARKETPLACE, API_ENDPOINT
- **Penguin Random House**: API_URL, API_KEY, API_DOMAIN, SEARCH_API, LIST_TITLES_BY_AUTHOR_API, URL
- **Domain Management**: SUBSCRIPTION_ID, DNS_RESOURCE_GROUP, RESOURCE_GROUP_NAME, FRONTDOOR_PROFILE_NAME

See [secrets-template.json](secrets-template.json) for the complete list.

## Troubleshooting

### GitHub CLI Not Authenticated

```powershell
gh auth login
```

### .NET SDK Not Found

```powershell
# Windows
winget install Microsoft.DotNet.SDK.9

# macOS
brew install dotnet

# Linux
# See https://dotnet.microsoft.com/download
```

### View User-Secrets

```powershell
# List secrets for a project
dotnet user-secrets list --project ImageAPI/ImageAPI.csproj

# Remove a secret
dotnet user-secrets remove "SECRET_NAME" --project ImageAPI/ImageAPI.csproj

# Clear all secrets
dotnet user-secrets clear --project ImageAPI/ImageAPI.csproj
```

### PowerShell Execution Policy

```powershell
# Run as administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Migration from Old Scripts

If you were using the old NPM script names:

**Old**: `npm run init:secrets`  
**New**: `npm run init-secrets`

Simply replace the first colon with a hyphen. Subcommands still use colons (standard NPM convention).

## Files

- `secrets-template.json` - Master template with all variables
- `Initialize-GitHubSecrets.ps1` - Configure GitHub Secrets
- `Update-SecretsConfig.ps1` - Update existing secrets files
- `Set-DotnetUserSecrets.ps1` - Configure dotnet user-secrets

## Documentation

- [docs/GITHUB_SECRETS_CONFIGURATION.md](docs/GITHUB_SECRETS_CONFIGURATION.md) - Comprehensive guide
- [docs/GITHUB_SECRETS_SETUP.md](docs/GITHUB_SECRETS_SETUP.md) - Initial setup guide
- [docs/GITHUB_SECRETS_REFERENCE.md](docs/GITHUB_SECRETS_REFERENCE.md) - Complete reference
- [docs/GITHUB_SECRETS_IMPLEMENTATION_SUMMARY.md](docs/GITHUB_SECRETS_IMPLEMENTATION_SUMMARY.md) - Implementation details

## Security Notes

1. ✅ Never commit `secrets.config.json` to source control (it's in .gitignore)
2. ✅ Use test credentials for development (e.g., `sk_test_...` for Stripe)
3. ✅ Rotate secrets regularly
4. ✅ User-secrets are stored outside the project directory
5. ✅ GitHub Secrets are encrypted and masked in logs

## Support

For issues or questions:

1. Check the comprehensive documentation
2. Review the troubleshooting section
3. Open an issue on the repository

---

**Quick Links**:

- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Functions Configuration](https://learn.microsoft.com/en-us/azure/azure-functions/functions-app-settings)
