# Azure Functions Development Scripts

This directory contains PowerShell scripts to streamline development workflow for the One Page Author API solution.

## Scripts Overview

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

### Install Prerequisites
```powershell
# Install Azure Functions Core Tools (if needed)
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# dotnet-update tool will be auto-installed by the script
# Or install manually:
dotnet tool install --global dotnet-update
```

## Typical Development Workflow

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