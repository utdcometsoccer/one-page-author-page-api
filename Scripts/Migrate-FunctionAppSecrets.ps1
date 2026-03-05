<#
.SYNOPSIS
    One-time migration script to rename AZURE_RESOURCE_GROUP / AZURE_LOCATION GitHub
    secrets to the function-app-specific names FUNCTION_APP_RESOURCE_GROUP /
    FUNCTION_APP_LOCATION.

.DESCRIPTION
    In a previous refactoring the generic secrets AZURE_RESOURCE_GROUP and AZURE_LOCATION
    were renamed to FUNCTION_APP_RESOURCE_GROUP and FUNCTION_APP_LOCATION to avoid
    confusion with the existing ISW_RESOURCE_GROUP / ISW_LOCATION and
    AZURE_RESOURCE_GROUP_NAME secrets.

    Because GitHub Secrets are write-only (their values cannot be read back via the API or
    CLI), this script cannot copy the values automatically.  Instead it:

      1. Reports which old secrets are still present in the repository.
      2. Accepts the new values either interactively or from a JSON config file
         (same format as secrets-template.json / secrets.config.json).
      3. Sets the two new GitHub Secrets.
      4. Optionally deletes the two stale old secrets.

    Run this script once per repository after merging the PR that introduced the rename.
    It is safe to run multiple times.

.PARAMETER ConfigFile
    Path to a JSON secrets config file (e.g. secrets.config.json).
    The file should contain the new keys:
        "FUNCTION_APP_RESOURCE_GROUP": "rg-onepageauthor-prod"
        "FUNCTION_APP_LOCATION":       "eastus"
    If omitted the script runs in interactive mode.

.PARAMETER RemoveOldSecrets
    When specified, the script deletes AZURE_RESOURCE_GROUP and AZURE_LOCATION from the
    repository after successfully setting the new secrets.  You will be prompted to confirm
    unless -Force is also specified.

.PARAMETER Force
    Skip the confirmation prompts (useful for CI/CD automation).
    Requires -RemoveOldSecrets to suppress the deletion confirmation prompt.

.PARAMETER DryRun
    Show what would happen without making any changes.

.PARAMETER Help
    Display detailed help information.

.EXAMPLE
    .\Migrate-FunctionAppSecrets.ps1
    Interactive mode — prompts for both new secret values.

.EXAMPLE
    .\Migrate-FunctionAppSecrets.ps1 -ConfigFile secrets.config.json
    Reads new values from a local config file.

.EXAMPLE
    .\Migrate-FunctionAppSecrets.ps1 -ConfigFile secrets.config.json -RemoveOldSecrets
    Sets new secrets and (after confirmation) deletes the old ones.

.EXAMPLE
    .\Migrate-FunctionAppSecrets.ps1 -ConfigFile secrets.config.json -RemoveOldSecrets -Force
    Non-interactive: sets new secrets and deletes old ones without prompting.

.EXAMPLE
    .\Migrate-FunctionAppSecrets.ps1 -DryRun
    Shows what the script would do without making any changes.

.NOTES
    Prerequisites:
    - GitHub CLI (gh) must be installed and authenticated
    - PowerShell 5.1+ or PowerShell Core 7+

    Security:
    - Never commit your secrets.config.json to source control
    - The old secrets are NOT automatically removed unless -RemoveOldSecrets is specified
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ConfigFile,

    [Parameter(Mandatory = $false)]
    [switch]$RemoveOldSecrets,

    [Parameter(Mandatory = $false)]
    [switch]$Force,

    [Parameter(Mandatory = $false)]
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [switch]$Help
)

if ($Help) {
    Get-Help $MyInvocation.MyCommand.Path -Detailed
    exit 0
}

# ─── Colour helpers (mirrors Initialize-GitHubSecrets.ps1 style) ────────────

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success { param([string]$Message); Write-ColorOutput "✓ $Message" "Green" }
function Write-Err     { param([string]$Message); Write-ColorOutput "✗ $Message" "Red" }
function Write-Warn    { param([string]$Message); Write-ColorOutput "⚠ $Message" "Yellow" }
function Write-Info    { param([string]$Message); Write-ColorOutput "ℹ $Message" "Cyan" }

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "═══════════════════════════════════════════════════════" "Magenta"
    Write-ColorOutput "  $Title" "Magenta"
    Write-ColorOutput "═══════════════════════════════════════════════════════" "Magenta"
    Write-Host ""
}

# ─── Rename map ─────────────────────────────────────────────────────────────

$Renames = @(
    @{
        OldName     = "AZURE_RESOURCE_GROUP"
        NewName     = "FUNCTION_APP_RESOURCE_GROUP"
        Description = "Resource group that contains the standalone function-app"
        Example     = "rg-onepageauthor-prod"
    },
    @{
        OldName     = "AZURE_LOCATION"
        NewName     = "FUNCTION_APP_LOCATION"
        Description = "Azure region for the standalone function-app infrastructure deployment"
        Example     = "eastus"
    }
)

# ─── Prerequisites ───────────────────────────────────────────────────────────

function Test-Prerequisites {
    Write-Section "Checking Prerequisites"

    $ghVersion = gh --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Err "GitHub CLI (gh) is not installed or not in PATH"
        Write-Info "Install from: https://cli.github.com/"
        return $false
    }
    Write-Success "GitHub CLI installed: $($ghVersion[0])"

    gh auth status 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Err "GitHub CLI is not authenticated. Run: gh auth login"
        return $false
    }
    Write-Success "GitHub CLI is authenticated"

    git rev-parse --is-inside-work-tree 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Not inside a git repository"
        return $false
    }
    Write-Success "Git repository detected"

    return $true
}

# ─── Secret existence check ──────────────────────────────────────────────────

function Test-SecretExists {
    param([string]$Name)
    $list = gh secret list 2>&1
    return ($list | Select-String -Pattern "^$Name\b") -ne $null
}

# ─── Set a GitHub secret ─────────────────────────────────────────────────────

function Set-GitHubSecret {
    param([string]$Name, [string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        Write-Warn "Skipping $Name (no value provided)"
        return $false
    }

    if ($DryRun) {
        $display = if ($Value.Length -gt 30) { "$($Value.Substring(0,30))..." } else { $Value }
        Write-Info "[DRY RUN] Would set $Name = $display"
        return $true
    }

    $Value | gh secret set $Name 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $display = if ($Value.Length -gt 30) { "$($Value.Substring(0,30))..." } else { $Value }
        Write-Success "Set $Name = $display"
        return $true
    }

    Write-Err "Failed to set $Name"
    return $false
}

# ─── Delete a GitHub secret ──────────────────────────────────────────────────

function Remove-GitHubSecret {
    param([string]$Name)

    if ($DryRun) {
        Write-Info "[DRY RUN] Would delete secret: $Name"
        return $true
    }

    gh secret delete $Name 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Deleted old secret: $Name"
        return $true
    }

    Write-Warn "Could not delete $Name (it may have already been removed)"
    return $false
}

# ─── Load values from a config file ─────────────────────────────────────────

function Get-ConfigValues {
    param([string]$FilePath)

    if (-not (Test-Path $FilePath)) {
        Write-Err "Config file not found: $FilePath"
        return $null
    }

    try {
        return Get-Content $FilePath -Raw | ConvertFrom-Json
    }
    catch {
        Write-Err "Failed to parse config file: $_"
        return $null
    }
}

# ─── Prompt for a value interactively ────────────────────────────────────────

function Read-SecretValue {
    param([hashtable]$Rename)

    Write-Host ""
    Write-ColorOutput "$($Rename.NewName)" "Yellow"
    Write-ColorOutput "  $($Rename.Description)" "Gray"
    Write-ColorOutput "  Example: $($Rename.Example)" "DarkGray"
    Write-ColorOutput "  (Previously stored as: $($Rename.OldName))" "DarkGray"

    return Read-Host "  Enter value (or press Enter to skip)"
}

# ─── Main ─────────────────────────────────────────────────────────────────────

function Invoke-Migration {
    Write-ColorOutput @"

╔═══════════════════════════════════════════════════════════════════════╗
║                                                                       ║
║   OnePageAuthor API — Function-App Secrets Migration                 ║
║                                                                       ║
║   Renames:                                                            ║
║     AZURE_RESOURCE_GROUP  →  FUNCTION_APP_RESOURCE_GROUP            ║
║     AZURE_LOCATION        →  FUNCTION_APP_LOCATION                  ║
║                                                                       ║
╚═══════════════════════════════════════════════════════════════════════╝

"@ "Cyan"

    if ($DryRun) {
        Write-Warn "DRY RUN MODE — no changes will be made"
        Write-Host ""
    }

    if (-not (Test-Prerequisites)) {
        Write-Err "Prerequisites check failed. Please resolve the issues above."
        exit 1
    }

    # ── Report old-secret status ──────────────────────────────────────────────

    Write-Section "Checking for Old Secrets"

    $oldSecretsFound = @()
    foreach ($rename in $Renames) {
        if (Test-SecretExists -Name $rename.OldName) {
            Write-Warn "Old secret found: $($rename.OldName)"
            $oldSecretsFound += $rename.OldName
        }
        else {
            Write-Info "Old secret not present (already removed or never set): $($rename.OldName)"
        }
    }

    # ── Collect new values ────────────────────────────────────────────────────

    Write-Section "Collecting New Secret Values"

    $config = $null
    if ($ConfigFile) {
        Write-Info "Reading values from config file: $ConfigFile"
        $config = Get-ConfigValues -FilePath $ConfigFile
        if ($null -eq $config) { exit 1 }
    }

    $newValues = @{}
    foreach ($rename in $Renames) {
        if ($config) {
            $val = $config.($rename.NewName)
            if ([string]::IsNullOrWhiteSpace($val)) {
                Write-Warn "$($rename.NewName) not found in config file — will prompt interactively"
                $val = Read-SecretValue -Rename $rename
            }
            else {
                $display = if ($val.Length -gt 40) { "$($val.Substring(0,40))..." } else { $val }
                Write-Info "  $($rename.NewName) = $display  (from config file)"
            }
        }
        else {
            $val = Read-SecretValue -Rename $rename
        }
        $newValues[$rename.NewName] = $val
    }

    # ── Summary before acting ─────────────────────────────────────────────────

    Write-Section "Migration Plan"

    $toSet = $newValues.GetEnumerator() | Where-Object { -not [string]::IsNullOrWhiteSpace($_.Value) }
    if ($toSet.Count -eq 0) {
        Write-Warn "No new values provided. Nothing to migrate."
        exit 0
    }

    Write-Info "Secrets to SET:"
    foreach ($kv in $toSet) {
        $display = if ($kv.Value.Length -gt 40) { "$($kv.Value.Substring(0,40))..." } else { $kv.Value }
        Write-ColorOutput "    + $($kv.Key) = $display" "Green"
    }

    if ($RemoveOldSecrets -and $oldSecretsFound.Count -gt 0) {
        Write-Info "Secrets to DELETE (old names):"
        foreach ($name in $oldSecretsFound) {
            Write-ColorOutput "    - $name" "Yellow"
        }
    }
    elseif (-not $RemoveOldSecrets -and $oldSecretsFound.Count -gt 0) {
        Write-Warn "Old secrets will NOT be deleted (pass -RemoveOldSecrets to remove them):"
        foreach ($name in $oldSecretsFound) {
            Write-ColorOutput "    ~ $name  (left in place)" "DarkYellow"
        }
    }

    # ── Confirmation ──────────────────────────────────────────────────────────

    if (-not $Force -and -not $DryRun) {
        Write-Host ""
        $answer = Read-Host "Proceed with migration? (y/n)"
        if ($answer -ne 'y' -and $answer -ne 'Y') {
            Write-Warn "Migration cancelled by user."
            exit 0
        }
    }

    # ── Set new secrets ───────────────────────────────────────────────────────

    Write-Section "Setting New Secrets"

    $setOk = $true
    foreach ($rename in $Renames) {
        $val = $newValues[$rename.NewName]
        if (-not [string]::IsNullOrWhiteSpace($val)) {
            if (-not (Set-GitHubSecret -Name $rename.NewName -Value $val)) {
                $setOk = $false
            }
        }
        else {
            Write-Warn "Skipping $($rename.NewName) — no value provided"
        }
    }

    # ── Optionally delete old secrets ─────────────────────────────────────────

    if ($RemoveOldSecrets -and $oldSecretsFound.Count -gt 0) {
        Write-Section "Removing Old Secrets"

        if (-not $setOk -and -not $Force) {
            Write-Warn "One or more new secrets failed to set. Old secrets will NOT be deleted."
            Write-Info "Fix the errors above, then re-run with -RemoveOldSecrets."
        }
        else {
            if (-not $Force -and -not $DryRun) {
                Write-Warn "About to permanently delete: $($oldSecretsFound -join ', ')"
                $confirm = Read-Host "Are you sure? (yes/no)"
                if ($confirm -ne 'yes') {
                    Write-Warn "Deletion skipped by user."
                    $oldSecretsFound = @()
                }
            }

            foreach ($name in $oldSecretsFound) {
                Remove-GitHubSecret -Name $name | Out-Null
            }
        }
    }

    # ── Done ──────────────────────────────────────────────────────────────────

    Write-Section "Migration Complete"

    if ($DryRun) {
        Write-Info "Dry run finished — no changes were made."
    }
    else {
        Write-Success "Migration finished."
        $repoName = gh repo view --json nameWithOwner -q .nameWithOwner 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Verify secrets at: https://github.com/$repoName/settings/secrets/actions"
        }
    }

    Write-Host ""
    Write-Info "Next steps:"
    Write-ColorOutput "  1. Verify the new secrets appear in your repository settings." "White"
    Write-ColorOutput "  2. Trigger a workflow run to confirm the function-app deploy steps work." "White"
    if ($oldSecretsFound.Count -gt 0 -and -not $RemoveOldSecrets) {
        Write-ColorOutput "  3. Run again with -RemoveOldSecrets once you have confirmed the workflow." "White"
    }
    Write-Host ""
}

Invoke-Migration
