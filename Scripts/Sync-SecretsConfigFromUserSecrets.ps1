<#
.SYNOPSIS
  Syncs secrets.config.json values from local dotnet user-secrets for the repo's Azure Function projects.

.DESCRIPTION
  Reads the Windows user-secrets store (%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json) for:
    - ImageAPI
    - InkStainedWretchFunctions
    - InkStainedWretchStripe
    - function-app
    - InkStainedWretchesConfig

  For any key present in secrets.config.json, if the key exists in any project user-secrets, this script
  updates secrets.config.json with that value.

  Security:
    - Never prints secret VALUES.
    - Only prints counts and key names (for conflicts).

.PARAMETER ConfigFile
  Path to the secrets configuration file.
  Default: secrets.config.json

.PARAMETER ProjectOrder
  Optional override list of project directories (precedence order). Default favors InkStainedWretchFunctions.

.EXAMPLE
  pwsh -File .\Scripts\Sync-SecretsConfigFromUserSecrets.ps1

#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $false)]
  [string]$ConfigFile = "secrets.config.json",

  [Parameter(Mandatory = $false)]
  [switch]$OverwriteExisting,

  [Parameter(Mandatory = $false)]
  [string[]]$ProjectOrder = @(
    "InkStainedWretchFunctions",
    "function-app",
    "InkStainedWretchStripe",
    "ImageAPI",
    "InkStainedWretchesConfig",
    "WhmcsTestHarness"
  )
)

$ErrorActionPreference = 'Stop'

function Get-UserSecretsIdFromCsproj([string]$csprojPath) {
  $xml = [xml](Get-Content -Raw -Path $csprojPath)
  $ids = @(
    $xml.Project.PropertyGroup |
      ForEach-Object { $_.UserSecretsId } |
      Where-Object { $_ -and $_.Trim() -ne '' } |
      ForEach-Object { $_.Trim() }
  )

  if ($ids.Count -gt 0) {
    return $ids[0]
  }

  return $null
}

function Get-ProjectCsproj([string]$projectDir) {
  $csprojs = @(Get-ChildItem -Path $projectDir -Filter "*.csproj" -ErrorAction SilentlyContinue)
  if ($csprojs.Count -eq 0) { return $null }
  return $csprojs[0].FullName
}

if (-not (Test-Path $ConfigFile)) {
  throw "Config file not found: $ConfigFile"
}

# Load config
$config = Get-Content -Raw -Path $ConfigFile | ConvertFrom-Json

# Merge user-secrets from projects by precedence
$selected = @{}      # key -> value
$source = @{}        # key -> project
$conflicts = New-Object System.Collections.Generic.List[string]
$projectsLoaded = 0

foreach ($projectDir in $ProjectOrder) {
  $csproj = Get-ProjectCsproj -projectDir $projectDir
  if (-not $csproj) {
    Write-Host "SKIP_${projectDir}=NoCsproj"
    continue
  }

  $id = Get-UserSecretsIdFromCsproj -csprojPath $csproj
  if (-not $id) {
    Write-Host "SKIP_${projectDir}=NoUserSecretsId"
    continue
  }

  if (-not $env:APPDATA) {
    throw "APPDATA is not set; expected Windows user-secrets location."
  }

  $secretsPath = Join-Path $env:APPDATA ("Microsoft\\UserSecrets\\{0}\\secrets.json" -f $id)
  if (-not (Test-Path $secretsPath)) {
    Write-Host "SKIP_${projectDir}=NoSecretsFile"
    continue
  }

  $projSecrets = Get-Content -Raw -Path $secretsPath | ConvertFrom-Json
  $projectsLoaded++

  foreach ($prop in $projSecrets.PSObject.Properties) {
    $k = $prop.Name
    $v = [string]$prop.Value

    if (-not $selected.ContainsKey($k)) {
      $selected[$k] = $v
      $source[$k] = $projectDir
      continue
    }

    if ($selected[$k] -ne $v) {
      if (-not $conflicts.Contains($k)) {
        $conflicts.Add($k) | Out-Null
      }
    }
  }
}

# Apply to secrets.config.json for matching keys only
$updated = 0
$missingInConfig = New-Object System.Collections.Generic.List[string]

foreach ($k in $selected.Keys) {
  $hasProp = $config.PSObject.Properties.Name -contains $k
  if (-not $hasProp) {
    $missingInConfig.Add($k) | Out-Null
    continue
  }

  # Skip section objects
  if ($config.$k -is [System.Management.Automation.PSCustomObject]) {
    continue
  }

  if (-not $OverwriteExisting) {
    if (-not [string]::IsNullOrWhiteSpace([string]$config.$k)) {
      continue
    }
  }

  if ([string]$config.$k -ne [string]$selected[$k]) {
    $config.$k = $selected[$k]
    $updated++
  }
}

# Save
$config | ConvertTo-Json -Depth 25 | Set-Content -Path $ConfigFile -Encoding UTF8

Write-Host ("PROJECTS_LOADED={0}" -f $projectsLoaded)
Write-Host ("KEYS_AVAILABLE_FROM_USERSECRETS={0}" -f $selected.Keys.Count)
Write-Host ("KEYS_UPDATED_IN_CONFIG={0}" -f $updated)
Write-Host ("CONFLICTING_KEYS_COUNT={0}" -f $conflicts.Count)
if ($conflicts.Count -gt 0) {
  $conflictList = ($conflicts | Sort-Object -Unique)
  Write-Host ("CONFLICTING_KEYS={0}" -f ($conflictList -join ','))
}
Write-Host ("USERSECRETS_KEYS_NOT_IN_CONFIG_COUNT={0}" -f $missingInConfig.Count)
