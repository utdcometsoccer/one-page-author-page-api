# PowerShell script to move secrets from local.settings.json to .NET User Secrets
# Usage: .\MoveSecretsToUserSecrets.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = ".",
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

# Set error handling
$ErrorActionPreference = "Stop"

# Get the script directory and project paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Resolve-Path (Join-Path $scriptDir $ProjectPath)
$localSettingsPath = Join-Path $projectDir "local.settings.json"
$csprojPath = Get-ChildItem -Path $projectDir -Filter "*.csproj" | Select-Object -First 1 -ExpandProperty FullName

Write-Host "🔐 Moving Secrets to .NET User Secrets" -ForegroundColor Cyan
Write-Host "Project Directory: $projectDir" -ForegroundColor Gray
Write-Host "Local Settings: $localSettingsPath" -ForegroundColor Gray
Write-Host "Project File: $csprojPath" -ForegroundColor Gray
Write-Host ""

# Check if required files exist
if (-not (Test-Path $localSettingsPath)) {
    Write-Error "❌ local.settings.json not found at: $localSettingsPath"
    exit 1
}

if (-not (Test-Path $csprojPath)) {
    Write-Error "❌ Project file (.csproj) not found in: $projectDir"
    exit 1
}

# Define which keys are secrets (contain sensitive data)
$secretKeys = @(
    "COSMOSDB_PRIMARY_KEY",
    "COSMOSDB_CONNECTION_STRING", 
    "CosmosDBConnection",
    "PENGUIN_RANDOM_HOUSE_API_KEY",
    "AMAZON_PRODUCT_ACCESS_KEY",
    "AMAZON_PRODUCT_SECRET_KEY",
    "AAD_CLIENT_ID",
    "AAD_TENANT_ID",
    "STRIPE_SECRET_KEY",
    "AZURE_STRIPE_SECRET_KEY",
    "GOOGLE_CLOUD_PROJECT_ID"
)

# Define which keys should remain in local.settings.json (non-sensitive configuration)
$publicKeys = @(
    "AzureWebJobsStorage",
    "FUNCTIONS_WORKER_RUNTIME",
    "COSMOSDB_ENDPOINT_URI",
    "COSMOSDB_DATABASE_ID",
    "PENGUIN_RANDOM_HOUSE_API_URL",
    "PENGUIN_RANDOM_HOUSE_API_DOMAIN",
    "PENGUIN_RANDOM_HOUSE_SEARCH_API",
    "PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API",
    "PENGUIN_RANDOM_HOUSE_URL",
    "AMAZON_PRODUCT_PARTNER_TAG",
    "AMAZON_PRODUCT_REGION",
    "AMAZON_PRODUCT_MARKETPLACE",
    "AMAZON_PRODUCT_API_ENDPOINT",
    "AZURE_SUBSCRIPTION_ID",
    "AZURE_DNS_RESOURCE_GROUP",
    "AZURE_RESOURCE_GROUP_NAME",
    "AZURE_FRONTDOOR_PROFILE_NAME",
    "GOOGLE_DOMAINS_LOCATION",
    "GOOGLE_DOMAINS_PROJECT",
    "TESTING_MODE",
    "MOCK_AZURE_INFRASTRUCTURE",
    "MOCK_GOOGLE_DOMAINS",
    "MOCK_STRIPE_PAYMENTS",
    "STRIPE_TEST_MODE",
    "MOCK_EXTERNAL_APIS",
    "ENABLE_TEST_LOGGING",
    "TEST_SCENARIO",
    "MAX_TEST_COST_LIMIT",
    "TEST_DOMAIN_SUFFIX",
    "SKIP_DOMAIN_PURCHASE"
)

try {
    # Load current local.settings.json
    Write-Host "📄 Loading local.settings.json..." -ForegroundColor Yellow
    $localSettings = Get-Content $localSettingsPath | ConvertFrom-Json
    $currentValues = $localSettings.Values
    
    # Check if user secrets are already initialized
    Write-Host "🔍 Checking user secrets initialization..." -ForegroundColor Yellow
    $userSecretsId = $null
    
    # Try to get existing user secrets ID from project file
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match '<UserSecretsId>([^<]+)</UserSecretsId>') {
        $userSecretsId = $matches[1]
        Write-Host "✅ Found existing User Secrets ID: $userSecretsId" -ForegroundColor Green
    } else {
        # Initialize user secrets
        Write-Host "🆕 Initializing user secrets..." -ForegroundColor Yellow
        if (-not $WhatIf) {
            Push-Location $projectDir
            try {
                $initOutput = dotnet user-secrets init 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ User secrets initialized successfully" -ForegroundColor Green
                    
                    # Get the new user secrets ID
                    $csprojContent = Get-Content $csprojPath -Raw
                    if ($csprojContent -match '<UserSecretsId>([^<]+)</UserSecretsId>') {
                        $userSecretsId = $matches[1]
                        Write-Host "📝 New User Secrets ID: $userSecretsId" -ForegroundColor Gray
                    }
                } else {
                    Write-Error "❌ Failed to initialize user secrets: $initOutput"
                }
            }
            finally {
                Pop-Location
            }
        } else {
            Write-Host "🔍 [WHAT-IF] Would initialize user secrets" -ForegroundColor Magenta
        }
    }
    
    # Add Microsoft.Extensions.Configuration.UserSecrets package if not present
    Write-Host "📦 Checking for UserSecrets package..." -ForegroundColor Yellow
    if ($csprojContent -notmatch 'Microsoft\.Extensions\.Configuration\.UserSecrets') {
        Write-Host "➕ Adding Microsoft.Extensions.Configuration.UserSecrets package..." -ForegroundColor Yellow
        if (-not $WhatIf) {
            Push-Location $projectDir
            try {
                $packageOutput = dotnet add package Microsoft.Extensions.Configuration.UserSecrets 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ UserSecrets package added successfully" -ForegroundColor Green
                } else {
                    Write-Warning "⚠️ Failed to add UserSecrets package: $packageOutput"
                    Write-Host "📝 Please manually add this package reference to your .csproj file:" -ForegroundColor Yellow
                    Write-Host '<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="9.0.0" />' -ForegroundColor Gray
                }
            }
            finally {
                Pop-Location
            }
        } else {
            Write-Host "🔍 [WHAT-IF] Would add Microsoft.Extensions.Configuration.UserSecrets package" -ForegroundColor Magenta
        }
    } else {
        Write-Host "✅ UserSecrets package already present" -ForegroundColor Green
    }
    
    # Identify secrets to move and public configs to keep
    $secretsToMove = @{}
    $publicConfig = @{}
    $unknownKeys = @()
    
    Write-Host "🔍 Analyzing configuration keys..." -ForegroundColor Yellow
    
    foreach ($key in $currentValues.PSObject.Properties.Name) {
        $value = $currentValues.$key
        
        if ($secretKeys -contains $key) {
            $secretsToMove[$key] = $value
            Write-Host "   🔐 Secret: $key" -ForegroundColor Red
        }
        elseif ($publicKeys -contains $key) {
            $publicConfig[$key] = $value
            Write-Host "   📋 Public: $key" -ForegroundColor Green
        }
        else {
            $unknownKeys += $key
            # Default unknown keys to public (safer than making them secrets)
            $publicConfig[$key] = $value
            Write-Host "   ❓ Unknown (treating as public): $key" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "📊 Summary:" -ForegroundColor Cyan
    Write-Host "   Secrets to move: $($secretsToMove.Count)" -ForegroundColor Red
    Write-Host "   Public configs to keep: $($publicConfig.Count)" -ForegroundColor Green
    Write-Host "   Unknown keys (treated as public): $($unknownKeys.Count)" -ForegroundColor Yellow
    
    if ($unknownKeys.Count -gt 0) {
        Write-Host ""
        Write-Host "❓ Unknown keys found:" -ForegroundColor Yellow
        foreach ($key in $unknownKeys) {
            Write-Host "   - $key" -ForegroundColor Gray
        }
        Write-Host "These will be treated as public configuration. If any contain secrets, update the script." -ForegroundColor Yellow
    }
    
    if (-not $Force -and -not $WhatIf) {
        Write-Host ""
        $confirm = Read-Host "Continue with moving $($secretsToMove.Count) secrets to user secrets? (y/N)"
        if ($confirm -notmatch '^[Yy]') {
            Write-Host "❌ Operation cancelled by user" -ForegroundColor Red
            exit 0
        }
    }
    
    # Move secrets to user secrets
    if ($secretsToMove.Count -gt 0) {
        Write-Host ""
        Write-Host "🔐 Moving secrets to user secrets..." -ForegroundColor Yellow
        
        Push-Location $projectDir
        try {
            foreach ($key in $secretsToMove.Keys) {
                $value = $secretsToMove[$key]
                $maskedValue = if ($value.Length -gt 8) { $value.Substring(0, 4) + "****" + $value.Substring($value.Length - 4) } else { "****" }
                
                if (-not $WhatIf) {
                    $setOutput = dotnet user-secrets set $key $value 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "   ✅ $key = $maskedValue" -ForegroundColor Green
                    } else {
                        Write-Error "❌ Failed to set secret $key : $setOutput"
                    }
                } else {
                    Write-Host "   🔍 [WHAT-IF] Would set: $key = $maskedValue" -ForegroundColor Magenta
                }
            }
        }
        finally {
            Pop-Location
        }
    }
    
    # Create new local.settings.json with only public configuration
    Write-Host ""
    Write-Host "📝 Creating new local.settings.json with public configuration only..." -ForegroundColor Yellow
    
    $newLocalSettings = @{
        IsEncrypted = $localSettings.IsEncrypted
        Values = $publicConfig
        Host = $localSettings.Host
    }
    
    # Backup original file
    $backupPath = "$localSettingsPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    if (-not $WhatIf) {
        Copy-Item $localSettingsPath $backupPath
        Write-Host "📦 Backup created: $(Split-Path -Leaf $backupPath)" -ForegroundColor Gray
    } else {
        Write-Host "🔍 [WHAT-IF] Would create backup: $(Split-Path -Leaf $backupPath)" -ForegroundColor Magenta
    }
    
    # Write new local.settings.json
    if (-not $WhatIf) {
        $newLocalSettings | ConvertTo-Json -Depth 10 | Set-Content $localSettingsPath -Encoding UTF8
        Write-Host "✅ Updated local.settings.json (secrets removed)" -ForegroundColor Green
    } else {
        Write-Host "🔍 [WHAT-IF] Would update local.settings.json with $($publicConfig.Count) public keys" -ForegroundColor Magenta
    }
    
    # Update Program.cs to support user secrets
    Write-Host ""
    Write-Host "🔧 Updating Program.cs for User Secrets..." -ForegroundColor Yellow
    
    $updateProgramScript = Join-Path $scriptDir "UpdateProgramForUserSecrets.ps1"
    if (Test-Path $updateProgramScript) {
        if (-not $WhatIf) {
            & $updateProgramScript -ProjectPath $ProjectPath
        } else {
            & $updateProgramScript -ProjectPath $ProjectPath -WhatIf
        }
    } else {
        Write-Warning "⚠️ UpdateProgramForUserSecrets.ps1 not found. You'll need to manually update Program.cs"
        Write-Host "📝 Add this to your Program.cs after 'var builder = FunctionsApplication.CreateBuilder(args);':" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "// Add User Secrets support for development" -ForegroundColor Gray
        Write-Host "if (builder.Environment.IsDevelopment())" -ForegroundColor Gray
        Write-Host "{" -ForegroundColor Gray
        Write-Host "    builder.Configuration.AddUserSecrets<Program>();" -ForegroundColor Gray
        Write-Host "}" -ForegroundColor Gray
        Write-Host ""
        Write-Host "And add this at the end of Program.cs:" -ForegroundColor Yellow
        Write-Host "public partial class Program { }" -ForegroundColor Gray
    }
    
    # Update testing scenario files
    Write-Host ""
    Write-Host "🧪 Updating testing scenario files..." -ForegroundColor Yellow
    
    $testingDir = Join-Path $projectDir "Testing"
    if (Test-Path $testingDir) {
        $scenarioFiles = Get-ChildItem -Path $testingDir -Filter "scenario*.local.settings.json"
        
        foreach ($scenarioFile in $scenarioFiles) {
            try {
                $scenarioContent = Get-Content $scenarioFile.FullName | ConvertFrom-Json
                
                # Remove secrets from scenario files too, keeping only public config
                $updatedScenarioValues = @{}
                foreach ($key in $scenarioContent.Values.PSObject.Properties.Name) {
                    if ($publicKeys -contains $key -or $unknownKeys -contains $key) {
                        $updatedScenarioValues[$key] = $scenarioContent.Values.$key
                    }
                }
                
                $updatedScenario = @{
                    IsEncrypted = $scenarioContent.IsEncrypted
                    Values = $updatedScenarioValues
                    Host = $scenarioContent.Host
                }
                
                if (-not $WhatIf) {
                    $backupScenarioPath = "$($scenarioFile.FullName).backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
                    Copy-Item $scenarioFile.FullName $backupScenarioPath
                    $updatedScenario | ConvertTo-Json -Depth 10 | Set-Content $scenarioFile.FullName -Encoding UTF8
                    Write-Host "   ✅ Updated $($scenarioFile.Name)" -ForegroundColor Green
                } else {
                    Write-Host "   🔍 [WHAT-IF] Would update $($scenarioFile.Name)" -ForegroundColor Magenta
                }
            }
            catch {
                Write-Warning "⚠️ Failed to update $($scenarioFile.Name): $($_.Exception.Message)"
            }
        }
    }
    
    Write-Host ""
    Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. 🔄 Update your Program.cs to include user secrets configuration" -ForegroundColor White
    Write-Host "2. 🧪 Test your application to ensure secrets are loaded correctly" -ForegroundColor White
    Write-Host "3. 🗑️  Delete backup files once you've verified everything works" -ForegroundColor White
    Write-Host "4. 📝 Update your README with user secrets setup instructions" -ForegroundColor White
    Write-Host ""
    Write-Host "🔐 User Secrets Location:" -ForegroundColor Gray
    if ($userSecretsId) {
        Write-Host "   Windows: %APPDATA%\Microsoft\UserSecrets\$userSecretsId\secrets.json" -ForegroundColor Gray
        Write-Host "   macOS/Linux: ~/.microsoft/usersecrets/$userSecretsId/secrets.json" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "🔍 To view your secrets: dotnet user-secrets list" -ForegroundColor Gray
    Write-Host "🗑️  To remove a secret: dotnet user-secrets remove <key>" -ForegroundColor Gray
    Write-Host "➕ To add a secret: dotnet user-secrets set <key> <value>" -ForegroundColor Gray
    
}
catch {
    Write-Error "❌ Migration failed: $($_.Exception.Message)"
    Write-Host "📞 Check the error details above and try again" -ForegroundColor Red
    exit 1
}