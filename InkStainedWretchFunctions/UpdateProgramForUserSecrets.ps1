# PowerShell script to update Program.cs to support User Secrets
# This script is called automatically by MoveSecretsToUserSecrets.ps1 or can be run separately

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = ".",
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Get the script directory and project paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Resolve-Path (Join-Path $scriptDir $ProjectPath)
$programPath = Join-Path $projectDir "Program.cs"

Write-Host "🔧 Updating Program.cs for User Secrets Support" -ForegroundColor Cyan
Write-Host "Program.cs Path: $programPath" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $programPath)) {
    Write-Error "❌ Program.cs not found at: $programPath"
    exit 1
}

try {
    # Read current Program.cs content
    $programContent = Get-Content $programPath -Raw
    
    # Check if user secrets is already configured
    if ($programContent -match 'AddUserSecrets' -or $programContent -match 'user-secrets') {
        Write-Host "✅ User Secrets already configured in Program.cs" -ForegroundColor Green
        exit 0
    }
    
    Write-Host "📝 Adding User Secrets configuration to Program.cs..." -ForegroundColor Yellow
    
    # Define the new Program.cs content with user secrets support
    $newProgramContent = @'
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Functions.Testing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Add User Secrets support for development
// This allows secrets to be stored securely outside of source code
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

var configuration = builder.Configuration;
var endpointUri = configuration["COSMOSDB_ENDPOINT_URI"];
var primaryKey = configuration["COSMOSDB_PRIMARY_KEY"];
var databaseId = configuration["COSMOSDB_DATABASE_ID"];

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddCosmosClient(endpointUri!, primaryKey!)
    .AddCosmosDatabase(databaseId!)
    .AddUserProfileRepository()
    .AddAuthorDataService() // Add Author data service for GetAuthors function
    .AddInkStainedWretchServices()
    .AddPenguinRandomHouseServices()
    .AddAmazonProductServices() // Add Amazon Product API services
    .AddJwtAuthentication() // Add JWT authentication services from OnePageAuthorLib
    .AddUserProfileServices()
    .AddDomainRegistrationRepository() // Add domain registration repository
    .AddDomainRegistrationServices() // Add domain registration services
    .AddStateProvinceRepository() // Add StateProvince repository
    .AddStateProvinceServices() // Add StateProvince services
    .AddDnsZoneService() // Add DNS zone service for domain registration triggers
    .AddFrontDoorServices() // Add Azure Front Door services for domain management
    .AddTestingServices() // Add testing services for mock implementations and test harnesses
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();

// Program class needed for User Secrets generic type parameter
public partial class Program { }
'@
    
    if (-not $WhatIf) {
        # Backup original Program.cs
        $backupPath = "$programPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $programPath $backupPath
        Write-Host "📦 Backup created: $(Split-Path -Leaf $backupPath)" -ForegroundColor Gray
        
        # Write updated Program.cs
        Set-Content $programPath $newProgramContent -Encoding UTF8
        Write-Host "✅ Program.cs updated with User Secrets support" -ForegroundColor Green
    } else {
        Write-Host "🔍 [WHAT-IF] Would update Program.cs with User Secrets configuration" -ForegroundColor Magenta
    }
    
    Write-Host ""
    Write-Host "📋 Changes made to Program.cs:" -ForegroundColor Cyan
    Write-Host "1. ➕ Added Microsoft.Extensions.Configuration using statement" -ForegroundColor White
    Write-Host "2. ➕ Added User Secrets configuration in development environment" -ForegroundColor White
    Write-Host "3. ➕ Added Program class for User Secrets generic type parameter" -ForegroundColor White
    Write-Host ""
    Write-Host "🔍 The configuration will now load secrets from:" -ForegroundColor Gray
    Write-Host "   - local.settings.json (public configuration)" -ForegroundColor Gray
    Write-Host "   - User Secrets (sensitive data in development)" -ForegroundColor Gray
    Write-Host "   - Environment variables (in production)" -ForegroundColor Gray
    
}
catch {
    Write-Error "❌ Failed to update Program.cs: $($_.Exception.Message)"
    exit 1
}