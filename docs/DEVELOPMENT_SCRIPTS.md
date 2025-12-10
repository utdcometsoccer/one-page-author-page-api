# Azure Functions Development Scripts

This directory contains PowerShell scripts to streamline development workflow for the One Page Author API solution.

## Scripts Overview

### `Initialize-GitHubSecrets.ps1` - GitHub Secrets Configuration Script

Automates the process of setting up GitHub repository secrets required for CI/CD deployment of the OnePageAuthor API Platform.

**Usage:**

```powershell
# Interactive mode - prompts for each secret
.\Initialize-GitHubSecrets.ps1 -Interactive

# Using a configuration file
.\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.json

# Using a text file (legacy format)
.\Initialize-GitHubSecrets.ps1 -SecretsFile secrets.txt

# Via NPM scripts
npm run init:secrets:interactive
npm run init:secrets -- -ConfigFile secrets.json

# Display help
.\Initialize-GitHubSecrets.ps1 -Help
npm run init:secrets:help
```

**What it does:**

1. ✅ **Validates prerequisites** - checks for GitHub CLI (gh) and authentication
2. ✅ **Prompts for secret values** - interactive mode with clear descriptions and examples
3. ✅ **Sets GitHub secrets** - uses `gh secret set` command to configure repository secrets
4. ✅ **Handles sensitive values** - masks sensitive input and displays appropriately
5. ✅ **Organizes by category** - groups secrets by Core Infrastructure, Cosmos DB, Azure AD, Stripe, etc.
6. ✅ **Validates configuration** - ensures required secrets are provided before setting
7. ✅ **Provides feedback** - clear success/failure messages and summary

**Secret Categories:**

- **Core Infrastructure** - Azure Resource Group, Location, Base Name, Azure Credentials
- **Cosmos DB** - Connection String, Endpoint URI, Primary Key, Database ID
- **Azure AD Authentication** - Tenant ID, Audience, Client ID (optional)
- **Azure Storage** - Connection String for ImageAPI (optional)
- **Stripe** - API Key, Webhook Secret for InkStainedWretchStripe (optional)
- **Domain Management** - Azure Subscription, DNS Resource Group, DNS Zone Name (optional)
- **Google Domains** - Project ID, Location (optional)
- **Amazon Product API** - Access Key, Secret Key, Partner Tag, Region, Marketplace (optional)
- **Penguin Random House API** - API Key, Domain (optional)

**Configuration File Format:**

See `secrets-template.json` for a complete template. Copy it to `secrets.json` and fill in your values:

```json
{
  "ISW_RESOURCE_GROUP": "rg-onepageauthor-prod",
  "ISW_LOCATION": "eastus",
  "COSMOSDB_ENDPOINT_URI": "https://your-account.documents.azure.com:443/",
  "COSMOSDB_PRIMARY_KEY": "your-key-here",
  "STRIPE_API_KEY": "sk_test_...",
  ...
}
```

**Security Notes:**

- 🔒 Never commit `secrets.json` to source control (already in `.gitignore`)
- 🔒 Sensitive values are masked in output
- 🔒 Use separate credentials for development vs production
- 🔒 Rotate secrets regularly

### `UpdateAndRun.ps1` - Main Development Script

Comprehensive script that updates packages, builds the solution, and runs Azure Functions projects.

**Usage:**

```powershell
.\UpdateAndRun.ps1 [-SkipUpdate] [-SkipBuild] [-Help]

```

**Parameters:**

- `-SkipUpdate` - Skip the dotnet-update step
- `-SkipBuild` - Skip the build step (restore will still run)
- `-Help` - Show detailed help information

**What it does:**

1. ✅ **Updates NuGet packages** using `dotnet-update -u` across the entire solution
2. ✅ **Restores packages** using `dotnet restore`
3. ✅ **Builds solution** using `dotnet build --no-restore`
4. ✅ **Starts Azure Functions** as background jobs on dedicated ports:

   - **ImageAPI** → `http://localhost:7000`
   - **InkStainedWretchFunctions** → `http://localhost:7001`
   - **InkStainedWretchStripe** → `http://localhost:7002`

### `StopFunctions.ps1` - Cleanup Script

Stops all running Azure Functions background jobs and cleans up completed jobs.

**Usage:**

```powershell
.\StopFunctions.ps1 [-Help]

```

**What it does:**

1. ✅ **Identifies running functions** - finds background jobs for ImageAPI, InkStainedWretchFunctions, and InkStainedWretchStripe
2. ✅ **Stops all function jobs** gracefully
3. ✅ **Removes completed jobs** to clean up the job queue
4. ✅ **Reports status** of remaining background jobs

## Prerequisites

### Required Tools

- **PowerShell 5.1+** or **PowerShell Core 7+**
- **.NET SDK 9.0** or later
- **Azure Functions Core Tools v4** (`func`)
- **dotnet-update tool** (automatically installed if missing)
- **GitHub CLI (gh)** (required for Initialize-GitHubSecrets.ps1)
- **npm** (optional, for running scripts via package.json)

### Install Prerequisites


```powershell
# Install Azure Functions Core Tools (if needed)
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Install GitHub CLI (for Initialize-GitHubSecrets.ps1)
# Windows: winget install --id GitHub.cli
# macOS: brew install gh
# Linux: See https://github.com/cli/cli/blob/trunk/docs/install_linux.md

# Authenticate with GitHub CLI
gh auth login

# dotnet-update tool will be auto-installed by the script
# Or install manually:
dotnet tool install --global dotnet-update
```

## Typical Development Workflow

### 🔐 Initial GitHub Secrets Setup (One-Time)

Before deploying to Azure, configure GitHub repository secrets:

```powershell
# Option 1: Interactive mode (recommended for first-time setup)
.\Initialize-GitHubSecrets.ps1 -Interactive
# or via NPM
npm run init:secrets:interactive

# Option 2: Using a configuration file
# 1. Copy the template
Copy-Item secrets-template.json secrets.json

# 2. Edit secrets.json with your values (use your favorite editor)
code secrets.json  # VS Code
notepad secrets.json  # Windows Notepad

# 3. Run the script with the config file
.\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.json
# or via NPM
npm run init:secrets -- -ConfigFile secrets.json

# Verify secrets were set
gh secret list
```

**What to configure:**
- ✅ **Core Infrastructure** - Azure credentials, resource group, location (REQUIRED)
- ✅ **Cosmos DB** - Connection string, endpoint, key, database ID (REQUIRED)
- ⚙️ **Azure Storage** - Required if using ImageAPI
- ⚙️ **Stripe** - Required if using InkStainedWretchStripe
- ⚙️ **External APIs** - Amazon, PRH, Google Domains (optional)

### 🚀 Start Development Session


```powershell
# Full update and run (recommended daily)
.\UpdateAndRun.ps1

# Quick start (skip package updates)
.\UpdateAndRun.ps1 -SkipUpdate

# Build and run only (skip updates and initial build)
.\UpdateAndRun.ps1 -SkipUpdate -SkipBuild

```

### 🔧 During Development


```powershell
# Check function status
Get-Job

# View function logs
Receive-Job -Name "ImageAPI" -Keep
Receive-Job -Name "InkStainedWretchFunctions" -Keep
Receive-Job -Name "InkStainedWretchStripe" -Keep

# Test endpoints
Invoke-RestMethod http://localhost:7000/api/health
Invoke-RestMethod http://localhost:7001/api/health  
Invoke-RestMethod http://localhost:7002/api/health

```

### 🛑 End Development Session


```powershell
# Stop all functions cleanly
.\StopFunctions.ps1

# Or manually stop specific functions
Stop-Job -Name "ImageAPI"
Stop-Job -Name "InkStainedWretchFunctions" 
Stop-Job -Name "InkStainedWretchStripe"

```

## Job Management Commands

### View Jobs


```powershell
# List all background jobs
Get-Job

# View detailed job information
Get-Job | Format-List

# Check job output (keep output for multiple views)
Receive-Job -Id <JobId> -Keep

# View live output (removes output after viewing)
Receive-Job -Id <JobId>

```

### Control Jobs


```powershell
# Stop specific job
Stop-Job -Id <JobId>
Stop-Job -Name "ImageAPI"

# Stop all function jobs at once
Get-Job | Where-Object { @('ImageAPI', 'InkStainedWretchFunctions', 'InkStainedWretchStripe') -contains $_.Name } | Stop-Job

# Remove completed jobs
Get-Job | Remove-Job

# Force remove all jobs (stopped and running)
Get-Job | Remove-Job -Force

```

## Function Endpoints

Once started, the Azure Functions will be available at:

| Function | URL | Purpose |
|----------|-----|---------|
| **ImageAPI** | `http://localhost:7000` | Image upload, processing, and management |
| **InkStainedWretchFunctions** | `http://localhost:7001` | Author data, StateProvinces, domain registration |
| **InkStainedWretchStripe** | `http://localhost:7002` | Stripe payment processing |

### Common API Endpoints


```powershell
# StateProvince endpoints (InkStainedWretchFunctions)
GET http://localhost:7001/api/stateprovinces/en-US
GET http://localhost:7001/api/stateprovinces/US/en-US

# Author endpoints (InkStainedWretchFunctions)  
GET http://localhost:7001/api/authors/example/com

# Image endpoints (ImageAPI)
POST http://localhost:7000/api/upload
GET http://localhost:7000/api/images/{id}

# Stripe endpoints (InkStainedWretchStripe)
POST http://localhost:7002/api/CreateStripeCustomer
POST http://localhost:7002/api/CreateSubscription

```

## Troubleshooting

### Common Issues

**❌ "dotnet-update command not found"**

```powershell
# Install the tool manually
dotnet tool install --global dotnet-update

```

**❌ "func command not found"**

```powershell
# Install Azure Functions Core Tools
npm install -g azure-functions-core-tools@4 --unsafe-perm true

```

**❌ Functions fail to start**

- Check that ports 7000-7002 are not in use
- Verify Azure Functions Core Tools is installed
- Check function app configuration and dependencies

**❌ Build failures**

- Run `dotnet clean` before trying again
- Check for package compatibility issues
- Verify .NET SDK version compatibility

### Port Conflicts

If ports 7000-7002 are in use, you can manually start functions on different ports:

```powershell
# Start on custom ports
func start --port 8000  # In ImageAPI directory
func start --port 8001  # In InkStainedWretchFunctions directory  
func start --port 8002  # In InkStainedWretchStripe directory

```

### Performance Tips


- Use `-SkipUpdate` flag when packages don't need updating (faster startup)
- Use `-SkipBuild` when only restarting functions after minor changes
- Monitor job output with `Receive-Job -Keep` to preserve logs for debugging

## Script Customization

The scripts can be modified to suit your development preferences:

- **Change default ports** by modifying the `$Port = 7000 + $PortCounter` line
- **Add more projects** by updating the `$Projects` array
- **Customize build options** by modifying the `dotnet build` command
- **Add pre/post build steps** by inserting commands in the appropriate sections
