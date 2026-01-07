<#
.SYNOPSIS
    Copies user secrets from SeedInkStainedWretchesLocale to SeedLocalizationData

.DESCRIPTION
    This script copies the Cosmos DB connection user secrets (EndpointUri, PrimaryKey, DatabaseId)
    from the SeedInkStainedWretchesLocale project to the SeedLocalizationData project.
    
    Both projects need these secrets to connect to the same Cosmos DB instance for seeding
    localization data.

.PARAMETER WhatIf
    Shows what would be copied without making any changes

.EXAMPLE
    .\Copy-LocalizationSeederSecrets.ps1
    Copies the secrets from SeedInkStainedWretchesLocale to SeedLocalizationData

.EXAMPLE
    .\Copy-LocalizationSeederSecrets.ps1 -WhatIf
    Shows what secrets would be copied without making changes
#>

param(
    [switch]$WhatIf
)

Write-Host "🔐 Copy User Secrets to SeedLocalizationData" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

# Project paths
$sourceProject = "SeedInkStainedWretchesLocale"
$targetProject = "SeedLocalizationData"

# Secret keys to copy
$secretKeys = @("EndpointUri", "PrimaryKey", "DatabaseId")

Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "  Source: $sourceProject" -ForegroundColor Gray
Write-Host "  Target: $targetProject" -ForegroundColor Gray
Write-Host "  Secrets: $($secretKeys -join ', ')" -ForegroundColor Gray
Write-Host ""

# Check if projects exist
if (-not (Test-Path $sourceProject)) {
    Write-Host "❌ Source project not found: $sourceProject" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $targetProject)) {
    Write-Host "❌ Target project not found: $targetProject" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Both projects found" -ForegroundColor Green
Write-Host ""

# Read secrets from source project
Write-Host "🔍 Reading secrets from $sourceProject..." -ForegroundColor Cyan

$secrets = @{}
$missingSecrets = @()

foreach ($key in $secretKeys) {
    try {
        Push-Location $sourceProject
        $value = dotnet user-secrets list 2>&1 | Select-String "^$key\s*=" | ForEach-Object {
            if ($_ -match "$key\s*=\s*(.+)$") {
                $matches[1].Trim()
            }
        }
        Pop-Location
        
        if ($value) {
            $secrets[$key] = $value
            Write-Host "  ✓ Found: $key" -ForegroundColor Green
        }
        else {
            $missingSecrets += $key
            Write-Host "  ⚠️  Missing: $key" -ForegroundColor Yellow
        }
    }
    catch {
        Pop-Location
        Write-Host "  ❌ Error reading $key : $_" -ForegroundColor Red
        $missingSecrets += $key
    }
}

Write-Host ""

if ($missingSecrets.Count -eq $secretKeys.Count) {
    Write-Host "❌ No secrets found in $sourceProject" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Configure secrets in $sourceProject first:" -ForegroundColor Yellow
    Write-Host "   cd $sourceProject" -ForegroundColor Gray
    Write-Host "   dotnet user-secrets set `"EndpointUri`" `"https://your-account.documents.azure.com:443/`"" -ForegroundColor Gray
    Write-Host "   dotnet user-secrets set `"PrimaryKey`" `"your-primary-key`"" -ForegroundColor Gray
    Write-Host "   dotnet user-secrets set `"DatabaseId`" `"your-database-name`"" -ForegroundColor Gray
    exit 1
}

if ($missingSecrets.Count -gt 0) {
    Write-Host "⚠️  Some secrets are missing: $($missingSecrets -join ', ')" -ForegroundColor Yellow
    Write-Host "   These will be skipped" -ForegroundColor Gray
    Write-Host ""
}

if ($secrets.Count -eq 0) {
    Write-Host "❌ No secrets to copy" -ForegroundColor Red
    exit 1
}

# Copy secrets to target project
if ($WhatIf) {
    Write-Host "📝 WHAT-IF MODE`: Would copy the following secrets to $targetProject`:" -ForegroundColor Yellow
    Write-Host ""
    foreach ($key in $secrets.Keys) {
        $displayValue = $secrets[$key]
        # Mask sensitive values in output
        if ($key -eq "PrimaryKey" -and $displayValue.Length -gt 8) {
            $displayValue = $displayValue.Substring(0, 4) + "..." + $displayValue.Substring($displayValue.Length - 4)
        }
        Write-Host "  $key = $displayValue" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "💡 Run without -WhatIf to apply changes" -ForegroundColor Cyan
}
else {
    Write-Host "📝 Copying secrets to $targetProject..." -ForegroundColor Cyan
    Write-Host ""
    
    $copiedCount = 0
    $errorCount = 0
    
    foreach ($key in $secrets.Keys) {
        try {
            Push-Location $targetProject
            dotnet user-secrets set $key $secrets[$key] | Out-Null
            Pop-Location
            Write-Host "  ✓ Copied: $key" -ForegroundColor Green
            $copiedCount++
        }
        catch {
            Pop-Location
            Write-Host "  ❌ Failed to copy $key : $_" -ForegroundColor Red
            $errorCount++
        }
    }
    
    Write-Host ""
    
    if ($errorCount -eq 0) {
        Write-Host "🎉 Successfully copied $copiedCount secret(s) to $targetProject" -ForegroundColor Green
        Write-Host ""
        Write-Host "✅ $targetProject is now configured with the same Cosmos DB connection" -ForegroundColor Green
        Write-Host ""
        Write-Host "💡 You can now run:" -ForegroundColor Cyan
        Write-Host "   cd $targetProject" -ForegroundColor Gray
        Write-Host "   dotnet run" -ForegroundColor Gray
    }
    else {
        Write-Host "⚠️  Copied $copiedCount secret(s) with $errorCount error(s)" -ForegroundColor Yellow
    }
}

Write-Host ""
