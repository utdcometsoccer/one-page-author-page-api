<#
.SYNOPSIS
    Updates an existing secrets.config.json file with any missing variables from the template.

.DESCRIPTION
    This script reads an existing secrets.config.json file and compares it with the template
    (secrets-template.json) to identify any missing configuration variables. It then adds
    the missing variables with empty values while preserving existing values and comments.
    
    This is useful when:
    - New configuration variables are added to the template
    - You want to ensure your secrets file is up-to-date
    - You need to migrate from an older secrets file format

.PARAMETER SecretsFile
    Path to the existing secrets configuration file to update.
    Default: "secrets.config.json"

.PARAMETER TemplateFile
    Path to the template file to use as reference.
    Default: "secrets-template.json"

.PARAMETER BackupOriginal
    Create a backup of the original file before updating.
    Default: $true

.PARAMETER DryRun
    Show what would be added without actually modifying the file.

.EXAMPLE
    .\Update-SecretsConfig.ps1
    Updates secrets.config.json using secrets-template.json

.EXAMPLE
    .\Update-SecretsConfig.ps1 -SecretsFile my-secrets.json -DryRun
    Shows what would be added to my-secrets.json without making changes

.EXAMPLE
    .\Update-SecretsConfig.ps1 -BackupOriginal:$false
    Updates secrets.config.json without creating a backup

.NOTES
    The script preserves:
    - Existing values (never overwrites)
    - Comment fields (those starting with "_")
    - JSON structure and formatting
    
    Security:
    - Never commits secrets to source control
    - The backup file should also be kept secure
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$SecretsFile = "secrets.config.json",
    
    [Parameter(Mandatory=$false)]
    [string]$TemplateFile = "secrets-template.json",
    
    [Parameter(Mandatory=$false)]
    [bool]$BackupOriginal = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun
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

# Main script
Write-Section "Update Secrets Configuration"

# Check if template file exists
if (-not (Test-Path $TemplateFile)) {
    Write-Error "Template file not found: $TemplateFile"
    exit 1
}

# Check if secrets file exists
if (-not (Test-Path $SecretsFile)) {
    Write-Warning "Secrets file not found: $SecretsFile"
    Write-Info "Creating new secrets file from template..."
    Copy-Item $TemplateFile $SecretsFile
    Write-Success "Created $SecretsFile from template"
    exit 0
}

try {
    # Read both files
    Write-Info "Reading template: $TemplateFile"
    $template = Get-Content $TemplateFile -Raw | ConvertFrom-Json
    
    Write-Info "Reading existing secrets: $SecretsFile"
    $secrets = Get-Content $SecretsFile -Raw | ConvertFrom-Json
    
    # Create backup if requested
    if ($BackupOriginal -and -not $DryRun) {
        $backupFile = "$SecretsFile.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $SecretsFile $backupFile
        Write-Success "Created backup: $backupFile"
    }
    
    # Get all properties from both objects
    $templateProps = $template.PSObject.Properties | Where-Object { $_.Name -notlike "_*" } | Select-Object -ExpandProperty Name
    $secretsProps = $secrets.PSObject.Properties | Where-Object { $_.Name -notlike "_*" } | Select-Object -ExpandProperty Name
    
    # Find missing properties
    $missingProps = $templateProps | Where-Object { $_ -notin $secretsProps }
    
    if ($missingProps.Count -eq 0) {
        Write-Success "No missing variables found. Secrets file is up-to-date!"
        exit 0
    }
    
    Write-Section "Missing Variables Detected"
    Write-Warning "Found $($missingProps.Count) missing variable(s):"
    
    # Add missing properties to secrets object
    $addedCount = 0
    foreach ($prop in $missingProps) {
        $value = $template.$prop
        
        # Skip comment sections
        if ($value -is [PSCustomObject] -and ($value.PSObject.Properties | Where-Object { $_.Name -eq "_description" })) {
            Write-Info "  Skipping section: $prop"
            continue
        }
        
        Write-ColorOutput "  + $prop" "Yellow"
        
        if (-not $DryRun) {
            # Add property with value from template
            $secrets | Add-Member -MemberType NoteProperty -Name $prop -Value $value -Force
            $addedCount++
        }
    }
    
    if ($DryRun) {
        Write-Section "Dry Run Complete"
        Write-Info "No changes were made. Run without -DryRun to apply changes."
    }
    else {
        # Save updated secrets file with nice formatting
        Write-Info "Saving updated secrets file..."
        $secrets | ConvertTo-Json -Depth 10 | Set-Content $SecretsFile -Encoding UTF8
        
        Write-Section "Update Complete"
        Write-Success "Added $addedCount variable(s) to $SecretsFile"
        Write-Info "Please review and fill in values for the new variables"
    }
}
catch {
    Write-Error "Error updating secrets file: $_"
    exit 1
}
