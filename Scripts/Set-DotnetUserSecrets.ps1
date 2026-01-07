<#
.SYNOPSIS
    Generates and sets dotnet user-secrets for all Azure Function projects based on secrets.config.json.

.DESCRIPTION
    This script reads a secrets configuration file (typically secrets.config.json) and sets
    dotnet user-secrets for each Azure Function project in the repository. This allows for
    secure local development without storing secrets in source control or local.settings.json.
    
    The script:
    - Identifies all Azure Function projects automatically
    - Initializes user-secrets for each project if not already initialized
    - Sets secrets from the configuration file
    - Handles per-function filtering (only sets relevant secrets for each function)
    - Provides detailed logging of operations
    
    Azure Function Projects:
    - ImageAPI - Image upload and management
    - InkStainedWretchFunctions - Main functions (domain registration, localization, external APIs)
    - InkStainedWretchStripe - Stripe payment processing
    - function-app - Core author data functions
    - InkStainedWretchesConfig - Configuration management

.PARAMETER ConfigFile
    Path to the secrets configuration file (JSON format).
    Default: "secrets.config.json"

.PARAMETER ProjectFilter
    Optional filter to only update specific projects.
    Examples: "ImageAPI", "InkStainedWretchStripe", "*Stripe*"

.PARAMETER DryRun
    Show what would be set without actually setting user-secrets.

.PARAMETER Force
    Overwrite existing user-secrets values.
    Default: $false (preserves existing values)

.EXAMPLE
    .\Set-DotnetUserSecrets.ps1
    Sets user-secrets for all projects using secrets.config.json

.EXAMPLE
    .\Set-DotnetUserSecrets.ps1 -ConfigFile my-secrets.json
    Sets user-secrets using a custom configuration file

.EXAMPLE
    .\Set-DotnetUserSecrets.ps1 -ProjectFilter "ImageAPI"
    Only updates ImageAPI project

.EXAMPLE
    .\Set-DotnetUserSecrets.ps1 -DryRun
    Shows what would be set without making changes

.EXAMPLE
    .\Set-DotnetUserSecrets.ps1 -Force
    Overwrites existing user-secrets values

.NOTES
    Prerequisites:
    - .NET SDK must be installed
    - Projects must be .NET Azure Functions projects
    
    User Secrets Storage:
    - Windows: %APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json
    - Linux/macOS: ~/.microsoft/usersecrets/<user_secrets_id>/secrets.json
    
    Security:
    - User secrets are stored outside the project directory
    - Never committed to source control
    - Separate from application settings in production
    
    References:
    - https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
    - https://learn.microsoft.com/en-us/azure/azure-functions/functions-develop-local
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = "secrets.config.json",
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectFilter = "*",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

# Color functions for better output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" "Green"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" "Red"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" "Yellow"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ $Message" "Cyan"
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "═══════════════════════════════════════════════════════" "Magenta"
    Write-ColorOutput "  $Title" "Magenta"
    Write-ColorOutput "═══════════════════════════════════════════════════════" "Magenta"
    Write-Host ""
}

# Function to check if dotnet is available
function Test-DotnetInstalled {
    try {
        $null = dotnet --version
        return $true
    }
    catch {
        return $false
    }
}

# Function to get Azure Function projects
function Get-AzureFunctionProjects {
    param([string]$Filter = "*")
    
    $projects = @()
    
    # Known Azure Function project directories
    $functionDirs = @(
        "ImageAPI",
        "InkStainedWretchFunctions",
        "InkStainedWretchStripe",
        "function-app",
        "InkStainedWretchesConfig"
    )
    
    foreach ($dir in $functionDirs) {
        if ($dir -like $Filter) {
            $csprojFiles = Get-ChildItem -Path $dir -Filter "*.csproj" -ErrorAction SilentlyContinue
            foreach ($csproj in $csprojFiles) {
                $projects += @{
                    Name = $dir
                    Path = $csproj.FullName
                    Directory = $csproj.DirectoryName
                }
            }
        }
    }
    
    return $projects
}

# Function to initialize user secrets for a project
function Initialize-UserSecrets {
    param(
        [string]$ProjectPath,
        [bool]$DryRun
    )
    
    if ($DryRun) {
        Write-Info "  [DRY RUN] Would initialize user-secrets"
        return $true
    }
    
    try {
        $output = dotnet user-secrets init --project $ProjectPath 2>&1
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

# Function to check if user secrets are initialized
function Test-UserSecretsInitialized {
    param([string]$ProjectPath)
    
    try {
        $output = dotnet user-secrets list --project $ProjectPath 2>&1
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

# Function to set a user secret
function Set-UserSecret {
    param(
        [string]$ProjectPath,
        [string]$Key,
        [string]$Value,
        [bool]$DryRun
    )
    
    if ($DryRun) {
        Write-Info "    [DRY RUN] Would set: $Key = [REDACTED]"
        return $true
    }
    
    try {
        # Note: dotnet user-secrets set handles value escaping automatically
        # We pass the value as-is and let the command handle special characters
        $output = dotnet user-secrets set $Key $Value --project $ProjectPath 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            return $true
        }
        else {
            Write-Warning "    Failed to set $Key : $output"
            return $false
        }
    }
    catch {
        Write-Warning "    Error setting ${Key}: $_"
        return $false
    }
}

# Function to determine which secrets are relevant for each project
function Get-RelevantSecretsForProject {
    param(
        [string]$ProjectName,
        [hashtable]$AllSecrets
    )
    
    # Common secrets for all projects
    $commonSecrets = @(
        "COSMOSDB_ENDPOINT_URI",
        "COSMOSDB_PRIMARY_KEY",
        "COSMOSDB_DATABASE_ID",
        "COSMOSDB_CONNECTION_STRING",
        "AAD_TENANT_ID",
        "AAD_AUDIENCE",
        "AAD_CLIENT_ID",
        "AAD_AUTHORITY",
        "OPEN_ID_CONNECT_METADATA_URL"
    )
    
    # Project-specific secrets
    $projectSecrets = @{
        "ImageAPI" = @(
            "AZURE_STORAGE_CONNECTION_STRING"
        )
        "InkStainedWretchStripe" = @(
            "STRIPE_API_KEY",
            "STRIPE_WEBHOOK_SECRET"
        )
        "InkStainedWretchFunctions" = @(
            "STRIPE_API_KEY",
            "AZURE_SUBSCRIPTION_ID",
            "AZURE_DNS_RESOURCE_GROUP",
            "AZURE_RESOURCE_GROUP_NAME",
            "AZURE_FRONTDOOR_PROFILE_NAME",
            "ISW_DNS_ZONE_NAME",
            "GOOGLE_CLOUD_PROJECT_ID",
            "GOOGLE_DOMAINS_LOCATION",
            "AMAZON_PRODUCT_ACCESS_KEY",
            "AMAZON_PRODUCT_SECRET_KEY",
            "AMAZON_PRODUCT_PARTNER_TAG",
            "AMAZON_PRODUCT_REGION",
            "AMAZON_PRODUCT_MARKETPLACE",
            "AMAZON_PRODUCT_API_ENDPOINT",
            "PENGUIN_RANDOM_HOUSE_API_URL",
            "PENGUIN_RANDOM_HOUSE_API_KEY",
            "PENGUIN_RANDOM_HOUSE_API_DOMAIN",
            "PENGUIN_RANDOM_HOUSE_SEARCH_API",
            "PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API",
            "PENGUIN_RANDOM_HOUSE_URL",
            "KEY_VAULT_URL",
            "USE_KEY_VAULT",
            "REFERRAL_BASE_URL",
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
        "function-app" = @()
        "InkStainedWretchesConfig" = @(
            "KEY_VAULT_URL",
            "USE_KEY_VAULT"
        )
    }
    
    # Combine common and project-specific secrets
    $relevantSecrets = $commonSecrets
    if ($projectSecrets.ContainsKey($ProjectName)) {
        $relevantSecrets += $projectSecrets[$ProjectName]
    }
    
    # Filter the all secrets hashtable to only include relevant ones
    $filtered = @{}
    foreach ($key in $AllSecrets.Keys) {
        if ($key -in $relevantSecrets) {
            $filtered[$key] = $AllSecrets[$key]
        }
    }
    
    return $filtered
}

# Main execution
Write-ColorOutput @"

╔═══════════════════════════════════════════════════════════════════════╗
║                                                                       ║
║   Set Dotnet User-Secrets from Configuration                         ║
║                                                                       ║
║   Configures user-secrets for Azure Function projects                ║
║                                                                       ║
╚═══════════════════════════════════════════════════════════════════════╝

"@ "Cyan"

# Check prerequisites
Write-Section "Checking Prerequisites"

if (-not (Test-DotnetInstalled)) {
    Write-Error ".NET SDK is not installed or not in PATH"
    Write-Info "Install from: https://dotnet.microsoft.com/download"
    exit 1
}

$dotnetVersion = dotnet --version
Write-Success ".NET SDK is installed: $dotnetVersion"

# Check if config file exists
if (-not (Test-Path $ConfigFile)) {
    Write-Error "Configuration file not found: $ConfigFile"
    Write-Info "Create it by copying secrets-template.json and filling in values"
    exit 1
}

Write-Success "Configuration file found: $ConfigFile"

# Load configuration
Write-Section "Loading Configuration"

try {
    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
}
catch {
    Write-Error "Failed to parse configuration file: $_"
    exit 1
}

# Extract secrets (ignore comment fields)
$secrets = @{}
$config.PSObject.Properties | Where-Object { 
    $_.Name -notlike "_*" -and 
    $_.Value -isnot [PSCustomObject] -and
    -not [string]::IsNullOrWhiteSpace($_.Value)
} | ForEach-Object {
    $secrets[$_.Name] = $_.Value
}

Write-Success "Loaded $($secrets.Count) secret(s) from configuration"

# Find Azure Function projects
Write-Section "Finding Azure Function Projects"

$projects = Get-AzureFunctionProjects -Filter $ProjectFilter

if ($projects.Count -eq 0) {
    Write-Warning "No Azure Function projects found matching filter: $ProjectFilter"
    exit 0
}

Write-Success "Found $($projects.Count) project(s):"
foreach ($project in $projects) {
    Write-Info "  - $($project.Name)"
}

# Process each project
Write-Section "Setting User-Secrets"

$totalSet = 0
$totalSkipped = 0
$totalFailed = 0

foreach ($project in $projects) {
    Write-Host ""
    Write-ColorOutput "Processing: $($project.Name)" "Yellow"
    Write-Info "  Project: $($project.Path)"
    
    # Check if user secrets are initialized
    $initialized = Test-UserSecretsInitialized -ProjectPath $project.Path
    
    if (-not $initialized) {
        Write-Warning "  User-secrets not initialized for this project"
        
        if (Initialize-UserSecrets -ProjectPath $project.Path -DryRun $DryRun) {
            Write-Success "  Initialized user-secrets"
        }
        else {
            Write-Error "  Failed to initialize user-secrets"
            $totalFailed++
            continue
        }
    }
    else {
        Write-Success "  User-secrets already initialized"
    }
    
    # Get relevant secrets for this project
    $relevantSecrets = Get-RelevantSecretsForProject -ProjectName $project.Name -AllSecrets $secrets
    
    if ($relevantSecrets.Count -eq 0) {
        Write-Warning "  No relevant secrets to set for this project"
        continue
    }
    
    Write-Info "  Setting $($relevantSecrets.Count) secret(s)..."
    
    $projectSet = 0
    $projectSkipped = 0
    $projectFailed = 0
    
    foreach ($key in $relevantSecrets.Keys) {
        $value = $relevantSecrets[$key]
        
        # Skip empty values
        if ([string]::IsNullOrWhiteSpace($value)) {
            Write-Info "    Skipping $key (empty value)"
            $projectSkipped++
            continue
        }
        
        if (Set-UserSecret -ProjectPath $project.Path -Key $key -Value $value -DryRun $DryRun) {
            $projectSet++
        }
        else {
            $projectFailed++
        }
    }
    
    $totalSet += $projectSet
    $totalSkipped += $projectSkipped
    $totalFailed += $projectFailed
    
    Write-Success "  Set: $projectSet | Skipped: $projectSkipped | Failed: $projectFailed"
}

# Summary
Write-Section "Summary"

if ($DryRun) {
    Write-Info "Dry run complete - no changes were made"
    Write-Info "Run without -DryRun to apply changes"
}
else {
    Write-Success "Total secrets set: $totalSet"
    if ($totalSkipped -gt 0) {
        Write-Warning "Total skipped: $totalSkipped"
    }
    if ($totalFailed -gt 0) {
        Write-Error "Total failed: $totalFailed"
    }
}

Write-Host ""
Write-Info "User-secrets configuration complete!"
Write-Info "Secrets are stored in your user profile, outside the project directory"
Write-Host ""
