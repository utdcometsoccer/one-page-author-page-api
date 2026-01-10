<#
.SYNOPSIS
    Initializes GitHub Secrets for OnePageAuthor API Platform deployment.

.DESCRIPTION
    This script helps configure GitHub repository secrets required for CI/CD deployment
    of the OnePageAuthor API Platform. It uses the GitHub CLI (gh) to set secrets and
    supports both interactive prompting and file-based configuration.
    
    The script configures secrets for:
    - Core Infrastructure (Azure authentication, resource group, location)
    - InkStainedWretchFunctions (Cosmos DB, Azure AD, Domain Management, External APIs)
    - ImageAPI (Cosmos DB, Azure Storage, Azure AD)
    - InkStainedWretchStripe (Stripe, Cosmos DB, Azure AD)

.PARAMETER Interactive
    Run in interactive mode, prompting for each secret value.
    
.PARAMETER ConfigFile
    Path to a JSON configuration file containing secret values.
    See secrets-template.json for format.
    
.PARAMETER SecretsFile
    Path to a text file containing secret definitions (legacy format).
    Format: SECRET_NAME=secret_value (one per line)
    
.PARAMETER Help
    Display detailed help information.

.EXAMPLE
    .\Initialize-GitHubSecrets.ps1 -Interactive
    Prompts for each secret value interactively.

.EXAMPLE
    .\Initialize-GitHubSecrets.ps1 -ConfigFile secrets.json
    Sets secrets from a JSON configuration file.

.EXAMPLE
    npm run init:secrets:interactive
    Run via NPM script wrapper (optional - PowerShell can be used directly).

.NOTES
    Prerequisites:
    - **GitHub CLI (gh)** must be installed and authenticated (REQUIRED)
    - PowerShell 5.1+ or PowerShell Core 7+ (REQUIRED)
    - npm (OPTIONAL - only needed if using NPM script wrappers)
    
    References:
    - GITHUB_SECRETS_CONFIGURATION.md - Complete secret reference
    - ConfigurationValidation.md - Configuration validation patterns
    
    Security:
    - Never commit secrets to source control
    - Use different credentials for development vs production
    - Rotate secrets regularly
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [switch]$Interactive,
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile,
    
    [Parameter(Mandatory=$false)]
    [string]$SecretsFile,
    
    [Parameter(Mandatory=$false)]
    [switch]$Help
)

# Display help if requested
if ($Help) {
    Get-Help $MyInvocation.MyCommand.Path -Detailed
    exit 0
}

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

# Check prerequisites
function Test-Prerequisites {
    Write-Section "Checking Prerequisites"
    
    # Check for gh CLI
    $ghVersion = gh --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "GitHub CLI (gh) is not installed or not in PATH"
        Write-Info "Install from: https://cli.github.com/"
        return $false
    }
    Write-Success "GitHub CLI is installed: $($ghVersion[0])"
    
    # Check gh authentication
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "GitHub CLI is not authenticated"
        Write-Info "Run: gh auth login"
        return $false
    }
    Write-Success "GitHub CLI is authenticated"
    
    # Check if we're in a git repository
    $repoCheck = git rev-parse --is-inside-work-tree 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Not in a git repository"
        return $false
    }
    Write-Success "Git repository detected"
    
    return $true
}

# Secret definitions
$secretDefinitions = @{
    "Core Infrastructure" = @(
        @{
            Name = "ISW_RESOURCE_GROUP"
            Description = "Azure Resource Group name"
            Required = $true
            Example = "rg-onepageauthor-prod"
            Category = "Infrastructure"
        },
        @{
            Name = "ISW_LOCATION"
            Description = "Azure region (e.g., eastus, westus2)"
            Required = $true
            Example = "eastus"
            Category = "Infrastructure"
        },
        @{
            Name = "ISW_BASE_NAME"
            Description = "Base name for all resources"
            Required = $true
            Example = "onepageauthor"
            Category = "Infrastructure"
        },
        @{
            Name = "AZURE_CREDENTIALS"
            Description = "Service Principal credentials (JSON format)"
            Required = $true
            Example = '{"clientId":"xxx","clientSecret":"xxx","subscriptionId":"xxx","tenantId":"xxx"}'
            Category = "Infrastructure"
            Sensitive = $true
        }
    )
    
    "Cosmos DB (Required)" = @(
        @{
            Name = "COSMOSDB_CONNECTION_STRING"
            Description = "Cosmos DB connection string"
            Required = $true
            Example = "AccountEndpoint=https://...;AccountKey=...;"
            Category = "Database"
            Sensitive = $true
        },
        @{
            Name = "COSMOSDB_ENDPOINT_URI"
            Description = "Cosmos DB endpoint URL"
            Required = $true
            Example = "https://your-account.documents.azure.com:443/"
            Category = "Database"
        },
        @{
            Name = "COSMOSDB_PRIMARY_KEY"
            Description = "Cosmos DB primary key"
            Required = $true
            Example = "your-primary-key=="
            Category = "Database"
            Sensitive = $true
        },
        @{
            Name = "COSMOSDB_DATABASE_ID"
            Description = "Cosmos DB database name"
            Required = $true
            Example = "OnePageAuthorDb"
            Category = "Database"
        }
    )
    
    "Azure AD Authentication (Optional)" = @(
        @{
            Name = "AAD_TENANT_ID"
            Description = "Azure AD tenant ID"
            Required = $false
            Example = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Category = "Authentication"
        },
        @{
            Name = "AAD_AUDIENCE"
            Description = "Azure AD client ID / audience"
            Required = $false
            Example = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Category = "Authentication"
        },
        @{
            Name = "AAD_CLIENT_ID"
            Description = "Azure AD client ID (if different from audience)"
            Required = $false
            Example = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Category = "Authentication"
        },
        @{
            Name = "AAD_AUTHORITY"
            Description = "Azure AD authority URL for JWT validation"
            Required = $false
            Example = "https://login.microsoftonline.com/{tenant-id}/v2.0"
            Category = "Authentication"
        },
        @{
            Name = "OPEN_ID_CONNECT_METADATA_URL"
            Description = "OpenID Connect metadata URL for JWT validation"
            Required = $false
            Example = "https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration"
            Category = "Authentication"
        },
        @{
            Name = "AAD_VALID_ISSUERS"
            Description = "Comma-delimited list of allowed issuer URLs"
            Required = $false
            Example = "https://login.microsoftonline.com/<tenant-1>/v2.0, https://login.microsoftonline.com/<tenant-2>/v2.0"
            Category = "Authentication"
        }
    )
    
    "Azure Storage (Required for ImageAPI)" = @(
        @{
            Name = "AZURE_STORAGE_CONNECTION_STRING"
            Description = "Azure Blob Storage connection string"
            Required = $false
            Example = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
            Category = "Storage"
            Sensitive = $true
        }
    )
    
    "Stripe (Required for InkStainedWretchStripe)" = @(
        @{
            Name = "STRIPE_API_KEY"
            Description = "Stripe secret key"
            Required = $false
            Example = "sk_test_..."
            Category = "Payment"
            Sensitive = $true
        },
        @{
            Name = "STRIPE_WEBHOOK_SECRET"
            Description = "Stripe webhook signing secret"
            Required = $false
            Example = "whsec_..."
            Category = "Payment"
            Sensitive = $true
        }
    )
    
    "Domain Management (Optional)" = @(
        @{
            Name = "AZURE_SUBSCRIPTION_ID"
            Description = "Azure subscription ID for DNS/Front Door"
            Required = $false
            Example = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Category = "Domain"
        },
        @{
            Name = "AZURE_DNS_RESOURCE_GROUP"
            Description = "Resource group for DNS zones"
            Required = $false
            Example = "rg-dns-prod"
            Category = "Domain"
        },
        @{
            Name = "AZURE_RESOURCE_GROUP_NAME"
            Description = "Resource group name for Azure Front Door"
            Required = $false
            Example = "rg-onepageauthor-prod"
            Category = "Domain"
        },
        @{
            Name = "AZURE_FRONTDOOR_PROFILE_NAME"
            Description = "Azure Front Door profile name"
            Required = $false
            Example = "afd-onepageauthor"
            Category = "Domain"
        },
        @{
            Name = "ISW_DNS_ZONE_NAME"
            Description = "DNS zone name"
            Required = $false
            Example = "example.com"
            Category = "Domain"
        }
    )
    
    "Google Domains (Optional)" = @(
        @{
            Name = "GOOGLE_CLOUD_PROJECT_ID"
            Description = "Google Cloud project ID"
            Required = $false
            Example = "my-project-123456"
            Category = "Domain"
        },
        @{
            Name = "GOOGLE_DOMAINS_LOCATION"
            Description = "Location for domain operations"
            Required = $false
            Example = "global"
            Category = "Domain"
        }
    )
    
    "Amazon Product API (Optional)" = @(
        @{
            Name = "AMAZON_PRODUCT_ACCESS_KEY"
            Description = "AWS access key ID"
            Required = $false
            Example = "AKIA..."
            Category = "ExternalAPI"
            Sensitive = $true
        },
        @{
            Name = "AMAZON_PRODUCT_SECRET_KEY"
            Description = "AWS secret access key"
            Required = $false
            Example = "..."
            Category = "ExternalAPI"
            Sensitive = $true
        },
        @{
            Name = "AMAZON_PRODUCT_PARTNER_TAG"
            Description = "Amazon Associates tracking ID"
            Required = $false
            Example = "yourtag-20"
            Category = "ExternalAPI"
        },
        @{
            Name = "AMAZON_PRODUCT_REGION"
            Description = "AWS region"
            Required = $false
            Example = "us-east-1"
            Category = "ExternalAPI"
        },
        @{
            Name = "AMAZON_PRODUCT_MARKETPLACE"
            Description = "Target marketplace"
            Required = $false
            Example = "www.amazon.com"
            Category = "ExternalAPI"
        },
        @{
            Name = "AMAZON_PRODUCT_API_ENDPOINT"
            Description = "Amazon Product API endpoint URL"
            Required = $false
            Example = "https://webservices.amazon.com/paapi5/..."
            Category = "ExternalAPI"
        }
    )
    
    "Penguin Random House API (Optional)" = @(
        @{
            Name = "PENGUIN_RANDOM_HOUSE_API_URL"
            Description = "PRH API base URL"
            Required = $false
            Example = "https://api.penguinrandomhouse.com"
            Category = "ExternalAPI"
        },
        @{
            Name = "PENGUIN_RANDOM_HOUSE_API_KEY"
            Description = "PRH API authentication key"
            Required = $false
            Example = "your-api-key"
            Category = "ExternalAPI"
            Sensitive = $true
        },
        @{
            Name = "PENGUIN_RANDOM_HOUSE_API_DOMAIN"
            Description = "PRH API domain"
            Required = $false
            Example = "PRH.US"
            Category = "ExternalAPI"
        },
        @{
            Name = "PENGUIN_RANDOM_HOUSE_SEARCH_API"
            Description = "PRH search API endpoint template"
            Required = $false
            Example = "/resources/titles/domains/{domain}/search"
            Category = "ExternalAPI"
        },
        @{
            Name = "PENGUIN_RANDOM_HOUSE_LIST_TITLES_BY_AUTHOR_API"
            Description = "PRH list titles by author API endpoint template"
            Required = $false
            Example = "/resources/authors/{authorId}/titles"
            Category = "ExternalAPI"
        },
        @{
            Name = "PENGUIN_RANDOM_HOUSE_URL"
            Description = "PRH website base URL"
            Required = $false
            Example = "https://www.penguinrandomhouse.com"
            Category = "ExternalAPI"
        }
    )
    
    "Azure Key Vault (Optional)" = @(
        @{
            Name = "KEY_VAULT_URL"
            Description = "Azure Key Vault URL for secure secret management"
            Required = $false
            Example = "https://your-keyvault.vault.azure.net/"
            Category = "Security"
        },
        @{
            Name = "USE_KEY_VAULT"
            Description = "Feature flag to enable Key Vault (true/false)"
            Required = $false
            Example = "false"
            Category = "Security"
        }
    )
    
    "Referral Program (Optional)" = @(
        @{
            Name = "REFERRAL_BASE_URL"
            Description = "Base URL for generating referral links"
            Required = $false
            Example = "https://inkstainedwretches.com"
            Category = "Features"
        }
    )
    
    "Testing Configuration (Optional)" = @(
        @{
            Name = "TESTING_MODE"
            Description = "Enable testing mode (true/false)"
            Required = $false
            Example = "false"
            Category = "Testing"
        },
        @{
            Name = "MOCK_AZURE_INFRASTRUCTURE"
            Description = "Mock Azure infrastructure operations (true/false)"
            Required = $false
            Example = "false"
            Category = "Testing"
        },
        @{
            Name = "MOCK_GOOGLE_DOMAINS"
            Description = "Mock Google Domains API calls (true/false)"
            Required = $false
            Example = "false"
            Category = "Testing"
        },
        @{
            Name = "MOCK_STRIPE_PAYMENTS"
            Description = "Mock Stripe payment operations (true/false)"
            Required = $false
            Example = "false"
            Category = "Testing"
        },
        @{
            Name = "STRIPE_TEST_MODE"
            Description = "Use Stripe test mode (true/false)"
            Required = $false
            Example = "true"
            Category = "Testing"
        },
        @{
            Name = "MOCK_EXTERNAL_APIS"
            Description = "Mock external API calls (true/false)"
            Required = $false
            Example = "false"
            Category = "Testing"
        },
        @{
            Name = "ENABLE_TEST_LOGGING"
            Description = "Enable detailed test logging (true/false)"
            Required = $false
            Example = "false"
            Category = "Testing"
        },
        @{
            Name = "TEST_SCENARIO"
            Description = "Test scenario identifier"
            Required = $false
            Example = "default"
            Category = "Testing"
        },
        @{
            Name = "MAX_TEST_COST_LIMIT"
            Description = "Maximum cost limit for testing operations (USD)"
            Required = $false
            Example = "50.00"
            Category = "Testing"
        },
        @{
            Name = "TEST_DOMAIN_SUFFIX"
            Description = "Test domain suffix for testing"
            Required = $false
            Example = "test-domain.local"
            Category = "Testing"
        },
        @{
            Name = "SKIP_DOMAIN_PURCHASE"
            Description = "Skip actual domain purchases during testing (true/false)"
            Required = $false
            Example = "true"
            Category = "Testing"
        }
    )
}

# Function to read secret from user
function Read-SecretValue {
    param(
        [hashtable]$Secret
    )
    
    $required = if ($Secret.Required) { "(Required)" } else { "(Optional)" }
    Write-Host ""
    Write-ColorOutput "$($Secret.Name) $required" "Yellow"
    Write-ColorOutput "  Description: $($Secret.Description)" "Gray"
    Write-ColorOutput "  Example: $($Secret.Example)" "DarkGray"
    
    if ($Secret.Sensitive) {
        Write-ColorOutput "  ⚠ This is a sensitive value - input will be hidden" "DarkYellow"
        $secureValue = Read-Host "  Enter value (or press Enter to skip)" -AsSecureString
        $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureValue)
        $value = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    } else {
        $value = Read-Host "  Enter value (or press Enter to skip)"
    }
    
    return $value
}

# Function to set GitHub secret
function Set-GitHubSecret {
    param(
        [string]$Name,
        [string]$Value,
        [bool]$IsSensitive = $false
    )
    
    if ([string]::IsNullOrWhiteSpace($Value)) {
        Write-Warning "Skipping $Name (no value provided)"
        return $false
    }
    
    # Remove surrounding quotes if present (guard against erroneous quote insertion)
    $CleanValue = $Value.Trim()
    
    # Only process quote removal if value has at least 2 characters
    if ($CleanValue.Length -ge 2) {
        # Remove leading/trailing single quotes
        if ($CleanValue.StartsWith("'") -and $CleanValue.EndsWith("'")) {
            $CleanValue = $CleanValue.Substring(1, $CleanValue.Length - 2)
            Write-Warning "Removed surrounding single quotes from $Name"
        }
        
        # Remove leading/trailing double quotes (unless it's valid JSON object/array)
        if ($CleanValue.StartsWith('"') -and $CleanValue.EndsWith('"')) {
            # Detect if this is a valid JSON object or array by parsing and checking the type
            # We want to preserve JSON objects and arrays (e.g., Azure credentials, config arrays)
            # but remove quotes from simple strings, numbers, booleans (e.g., "myvalue", "https://example.com", "true", "123")
            # 
            # Design decision: JSON primitives (strings, numbers, booleans, null) should have quotes removed
            # because in the context of GitHub secrets, these are likely erroneously quoted simple values.
            # Only complex JSON structures (objects, arrays) need to preserve their quotes.
            $isJsonObjectOrArray = $false
            
            # Performance optimization: Quick check if content looks like JSON object/array before parsing
            # Peek at the first character inside the quotes
            if ($CleanValue.Length -gt 2) {
                $firstCharInsideQuotes = $CleanValue[1]
                
                # Only attempt JSON parsing if it starts with { or [ (JSON object or array)
                if ($firstCharInsideQuotes -eq '{' -or $firstCharInsideQuotes -eq '[') {
                    try {
                        $parsed = ConvertFrom-Json $CleanValue -ErrorAction Stop
                        # Check if the parsed result is a PSCustomObject (JSON object) or Array (JSON array)
                        if ($parsed -is [System.Management.Automation.PSCustomObject] -or $parsed -is [System.Array]) {
                            $isJsonObjectOrArray = $true
                        }
                    } catch {
                        # Not valid JSON at all
                        $isJsonObjectOrArray = $false
                    }
                }
            }
            
            # Only remove quotes if not a JSON object or array
            if (-not $isJsonObjectOrArray) {
                $CleanValue = $CleanValue.Substring(1, $CleanValue.Length - 2)
                Write-Warning "Removed surrounding double quotes from $Name"
            }
        }
    }
    
    try {
        # Use gh secret set command
        $CleanValue | gh secret set $Name 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            if ($IsSensitive) {
                Write-Success "Set $Name (value hidden)"
            } else {
                $displayValue = if ($CleanValue.Length -gt 30) { "$($CleanValue.Substring(0, 30))..." } else { $CleanValue }
                Write-Success "Set $Name = $displayValue"
            }
            return $true
        } else {
            Write-Error "Failed to set $Name"
            return $false
        }
    }
    catch {
        Write-Error "Error setting ${Name}: $_"
        return $false
    }
}

# Function to load secrets from config file
function Get-SecretsFromConfigFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "Config file not found: $FilePath"
        return $null
    }
    
    try {
        $config = Get-Content $FilePath -Raw | ConvertFrom-Json
        return $config
    }
    catch {
        Write-Error "Failed to parse config file: $_"
        return $null
    }
}

# Function to load secrets from text file
function Get-SecretsFromTextFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "Secrets file not found: $FilePath"
        return $null
    }
    
    $secrets = @{}
    Get-Content $FilePath | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            $secrets[$matches[1].Trim()] = $matches[2].Trim()
        }
    }
    
    return $secrets
}

# Main execution
function Invoke-SecretInitialization {
    Write-ColorOutput @"

╔═══════════════════════════════════════════════════════════════════════╗
║                                                                       ║
║   OnePageAuthor API - GitHub Secrets Initialization                  ║
║                                                                       ║
║   This script will help you configure GitHub repository secrets      ║
║   required for CI/CD deployment.                                     ║
║                                                                       ║
╚═══════════════════════════════════════════════════════════════════════╝

"@ "Cyan"

    # Check prerequisites
    if (-not (Test-Prerequisites)) {
        Write-Error "Prerequisites check failed. Please resolve the issues above."
        exit 1
    }
    
    $secretsToSet = @{}
    
    # Interactive mode
    if ($Interactive) {
        Write-Section "Interactive Secret Configuration"
        Write-Info "You will be prompted for each secret. Press Enter to skip optional secrets."
        
        foreach ($category in $secretDefinitions.Keys) {
            Write-Section $category
            
            foreach ($secret in $secretDefinitions[$category]) {
                $value = Read-SecretValue -Secret $secret
                
                if (-not [string]::IsNullOrWhiteSpace($value)) {
                    $secretsToSet[$secret.Name] = @{
                        Value = $value
                        Sensitive = $secret.Sensitive -eq $true
                    }
                }
            }
        }
    }
    # Config file mode
    elseif ($ConfigFile) {
        Write-Section "Loading Secrets from Config File"
        $config = Get-SecretsFromConfigFile -FilePath $ConfigFile
        
        if ($null -eq $config) {
            exit 1
        }
        
        # Map config values to secret names
        foreach ($category in $secretDefinitions.Keys) {
            foreach ($secret in $secretDefinitions[$category]) {
                $value = $config.($secret.Name)
                if (-not [string]::IsNullOrWhiteSpace($value)) {
                    $secretsToSet[$secret.Name] = @{
                        Value = $value
                        Sensitive = $secret.Sensitive -eq $true
                    }
                }
            }
        }
        
        Write-Success "Loaded $($secretsToSet.Count) secrets from config file"
    }
    # Text file mode
    elseif ($SecretsFile) {
        Write-Section "Loading Secrets from Text File"
        $secrets = Get-SecretsFromTextFile -FilePath $SecretsFile
        
        if ($null -eq $secrets) {
            exit 1
        }
        
        foreach ($secretName in $secrets.Keys) {
            # Check if this is a known secret
            $secretDef = $null
            foreach ($category in $secretDefinitions.Keys) {
                $secretDef = $secretDefinitions[$category] | Where-Object { $_.Name -eq $secretName }
                if ($secretDef) { break }
            }
            
            $secretsToSet[$secretName] = @{
                Value = $secrets[$secretName]
                Sensitive = $secretDef -and $secretDef.Sensitive -eq $true
            }
        }
        
        Write-Success "Loaded $($secretsToSet.Count) secrets from text file"
    }
    else {
        Write-Error "No input method specified. Use -Interactive, -ConfigFile, or -SecretsFile"
        Write-Info "Run with -Help for more information"
        exit 1
    }
    
    # Confirmation
    if ($secretsToSet.Count -eq 0) {
        Write-Warning "No secrets to set. Exiting."
        exit 0
    }
    
    Write-Section "Confirmation"
    Write-Info "Ready to set $($secretsToSet.Count) GitHub secrets"
    
    $confirmation = Read-Host "Continue? (y/n)"
    if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
        Write-Warning "Cancelled by user"
        exit 0
    }
    
    # Set secrets
    Write-Section "Setting GitHub Secrets"
    $successCount = 0
    $failCount = 0
    
    foreach ($secretName in $secretsToSet.Keys) {
        $secretInfo = $secretsToSet[$secretName]
        if (Set-GitHubSecret -Name $secretName -Value $secretInfo.Value -IsSensitive $secretInfo.Sensitive) {
            $successCount++
        } else {
            $failCount++
        }
    }
    
    # Summary
    Write-Section "Summary"
    Write-Success "Successfully set: $successCount secrets"
    if ($failCount -gt 0) {
        Write-Warning "Failed to set: $failCount secrets"
    }
    
    Write-Host ""
    Write-Info "GitHub secrets have been configured!"
    Write-Info "You can verify them at: https://github.com/$(gh repo view --json nameWithOwner -q .nameWithOwner)/settings/secrets/actions"
    Write-Host ""
}

# Execute main function
Invoke-SecretInitialization
