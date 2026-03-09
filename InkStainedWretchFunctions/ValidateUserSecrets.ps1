# PowerShell script to validate User Secrets configuration
# Usage: .\ValidateUserSecrets.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "."
)

$ErrorActionPreference = "Stop"

# Get the script directory and project paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Resolve-Path (Join-Path $scriptDir $ProjectPath)

Write-Host "🔍 Validating User Secrets Configuration" -ForegroundColor Cyan
Write-Host "Project Directory: $projectDir" -ForegroundColor Gray
Write-Host ""

# Expected secret keys
$expectedSecrets = @(
    "COSMOSDB_PRIMARY_KEY",
    "COSMOSDB_CONNECTION_STRING", 
    "CosmosDBConnection",
    "PENGUIN_RANDOM_HOUSE_API_KEY",
    "AMAZON_PRODUCT_ACCESS_KEY",
    "AMAZON_PRODUCT_SECRET_KEY",
    "AAD_CLIENT_ID",
    "AAD_TENANT_ID"
)

try {
    Push-Location $projectDir
    
    # Check if user secrets is initialized
    Write-Host "1. 🔧 Checking User Secrets initialization..." -ForegroundColor Yellow
    $csprojPath = Get-ChildItem -Filter "*.csproj" | Select-Object -First 1 -ExpandProperty FullName
    
    if (-not $csprojPath) {
        Write-Error "❌ No .csproj file found in project directory"
        exit 1
    }
    
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match '<UserSecretsId>([^<]+)</UserSecretsId>') {
        $userSecretsId = $matches[1]
        Write-Host "   ✅ User Secrets ID: $userSecretsId" -ForegroundColor Green
    } else {
        Write-Host "   ❌ User Secrets not initialized" -ForegroundColor Red
        Write-Host "   Run: dotnet user-secrets init" -ForegroundColor Yellow
        exit 1
    }
    
    # Check if UserSecrets package is referenced
    Write-Host ""
    Write-Host "2. 📦 Checking UserSecrets package reference..." -ForegroundColor Yellow
    if ($csprojContent -match 'Microsoft\.Extensions\.Configuration\.UserSecrets') {
        Write-Host "   ✅ UserSecrets package is referenced" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ UserSecrets package not found in project file" -ForegroundColor Yellow
        Write-Host "   Run: dotnet add package Microsoft.Extensions.Configuration.UserSecrets" -ForegroundColor Gray
    }
    
    # Check Program.cs for user secrets configuration
    Write-Host ""
    Write-Host "3. 📝 Checking Program.cs configuration..." -ForegroundColor Yellow
    $programPath = "Program.cs"
    if (Test-Path $programPath) {
        $programContent = Get-Content $programPath -Raw
        
        if ($programContent -match 'AddUserSecrets' -or $programContent -match 'builder\.Configuration\.AddUserSecrets') {
            Write-Host "   ✅ User Secrets configured in Program.cs" -ForegroundColor Green
        } else {
            Write-Host "   ❌ User Secrets not configured in Program.cs" -ForegroundColor Red
            Write-Host "   Add this after 'var builder = FunctionsApplication.CreateBuilder(args);':" -ForegroundColor Yellow
            Write-Host "   if (builder.Environment.IsDevelopment()) { builder.Configuration.AddUserSecrets<Program>(); }" -ForegroundColor Gray
        }
        
        if ($programContent -match 'public partial class Program') {
            Write-Host "   ✅ Program class defined for User Secrets" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Program class not found" -ForegroundColor Red
            Write-Host "   Add this at the end of Program.cs: public partial class Program { }" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ❌ Program.cs not found" -ForegroundColor Red
    }
    
    # List current user secrets
    Write-Host ""
    Write-Host "4. 🔐 Checking stored secrets..." -ForegroundColor Yellow
    
    $secretsOutput = dotnet user-secrets list 2>&1
    if ($LASTEXITCODE -eq 0) {
        $secretLines = $secretsOutput -split "`n" | Where-Object { $_ -ne "" }
        
        if ($secretLines.Count -eq 0) {
            Write-Host "   ⚠️ No secrets found" -ForegroundColor Yellow
        } else {
            Write-Host "   ✅ Found $($secretLines.Count) secrets:" -ForegroundColor Green
            
            $foundSecrets = @()
            foreach ($line in $secretLines) {
                if ($line -match '^([^=]+)') {
                    $secretKey = $matches[1].Trim()
                    $foundSecrets += $secretKey
                    
                    if ($expectedSecrets -contains $secretKey) {
                        Write-Host "     ✅ $secretKey" -ForegroundColor Green
                    } else {
                        Write-Host "     ❓ $secretKey (not in expected list)" -ForegroundColor Yellow
                    }
                }
            }
            
            # Check for missing expected secrets
            foreach ($expectedSecret in $expectedSecrets) {
                if ($foundSecrets -notcontains $expectedSecret) {
                    Write-Host "     ❌ Missing: $expectedSecret" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "   ❌ Failed to list user secrets: $secretsOutput" -ForegroundColor Red
    }
    
    # Check local.settings.json to ensure secrets were removed
    Write-Host ""
    Write-Host "5. 📄 Checking local.settings.json..." -ForegroundColor Yellow
    $localSettingsPath = "local.settings.json"
    
    if (Test-Path $localSettingsPath) {
        $localSettings = Get-Content $localSettingsPath | ConvertFrom-Json
        $localValues = $localSettings.Values
        
        $secretsInLocal = @()
        foreach ($expectedSecret in $expectedSecrets) {
            if ($localValues.PSObject.Properties.Name -contains $expectedSecret) {
                $secretsInLocal += $expectedSecret
            }
        }
        
        if ($secretsInLocal.Count -eq 0) {
            Write-Host "   ✅ No secrets found in local.settings.json" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️ Found $($secretsInLocal.Count) secrets still in local.settings.json:" -ForegroundColor Yellow
            foreach ($secret in $secretsInLocal) {
                Write-Host "     - $secret" -ForegroundColor Red
            }
            Write-Host "   Consider moving these to user secrets for better security" -ForegroundColor Gray
        }
    } else {
        Write-Host "   ❌ local.settings.json not found" -ForegroundColor Red
    }
    
    # Test configuration loading (if possible)
    Write-Host ""
    Write-Host "6. 🧪 Testing configuration loading..." -ForegroundColor Yellow
    
    # Check if we can build the project
    $buildOutput = dotnet build --configuration Debug --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Project builds successfully" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ Project build has issues:" -ForegroundColor Yellow
        Write-Host "   $buildOutput" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "🏁 Validation Summary:" -ForegroundColor Cyan
    
    $allGood = $true
    
    # Summary check
    if ($userSecretsId) {
        Write-Host "   ✅ User Secrets initialized" -ForegroundColor Green
    } else {
        Write-Host "   ❌ User Secrets not initialized" -ForegroundColor Red
        $allGood = $false
    }
    
    if ($secretLines.Count -gt 0) {
        Write-Host "   ✅ Secrets are stored" -ForegroundColor Green
    } else {
        Write-Host "   ❌ No secrets found" -ForegroundColor Red
        $allGood = $false
    }
    
    if ($secretsInLocal.Count -eq 0) {
        Write-Host "   ✅ Secrets removed from local.settings.json" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️ Some secrets still in local.settings.json" -ForegroundColor Yellow
    }
    
    if ($allGood) {
        Write-Host ""
        Write-Host "🎉 User Secrets configuration is valid!" -ForegroundColor Green
        Write-Host "   Your secrets are now stored securely outside of source control" -ForegroundColor Gray
        Write-Host "   You can start your Function App with: func start" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "⚠️ Configuration needs attention" -ForegroundColor Yellow
        Write-Host "   Review the issues above and follow the suggested actions" -ForegroundColor Gray
    }
    
}
catch {
    Write-Error "❌ Validation failed: $($_.Exception.Message)"
    exit 1
}
finally {
    Pop-Location
}