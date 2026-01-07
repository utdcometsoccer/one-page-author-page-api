param(
    [Parameter(Mandatory=$false)]
    [string[]] $Paths = @(
        "ImageAPI/local.settings.json",
        "InkStainedWretchFunctions/local.settings.json",
        "InkStainedWretchStripe/local.settings.json",
        "function-app/local.settings.json"
    ),
    [Parameter(Mandatory=$false)]
    [string[]] $Keys = @(
        "COSMOSDB_ENDPOINT_URI",
        "COSMOSDB_PRIMARY_KEY",
        "COSMOSDB_DATABASE_ID",
        "COSMOSDB_CONNECTION_STRING",
        "AZURE_STORAGE_CONNECTION_STRING",
        "AAD_TENANT_ID",
        "AAD_AUDIENCE",
        "AAD_CLIENT_ID",
        "STRIPE_API_KEY",
        "STRIPE_WEBHOOK_SECRET"
    )
)

function Remove-WrappingQuotes([string]$value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return $value }
    $trimmed = $value.Trim()
    if (($trimmed.StartsWith('"') -and $trimmed.EndsWith('"')) -or ($trimmed.StartsWith("'") -and $trimmed.EndsWith("'"))) {
        return $trimmed.Substring(1, $trimmed.Length - 2).Trim()
    }
    return $trimmed
}

Write-Host "🧹 Cleaning quoted config values in local.settings.json files..." -ForegroundColor Cyan

# Get solution root (parent of Scripts directory)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionRoot = Split-Path -Parent $ScriptDir

foreach ($relativePath in $Paths) {
    $path = Join-Path $SolutionRoot $relativePath
    if (-not (Test-Path $path)) { 
        Write-Host "Skipping missing file: $relativePath" -ForegroundColor DarkGray
        continue 
    }

    try {
        $json = Get-Content $path -Raw | ConvertFrom-Json
    } catch {
        Write-Host "❌ Failed to read JSON: $relativePath — $($_.Exception.Message)" -ForegroundColor Red
        continue
    }

    if ($null -eq $json.Values) { 
        Write-Host "No 'Values' section in: $relativePath" -ForegroundColor Yellow
        continue 
    }

    $changes = 0
    foreach ($key in $Keys) {
        if ($json.Values.PSObject.Properties.Name -contains $key) {
            $before = [string]$json.Values.$key
            $after = Remove-WrappingQuotes $before
            if ($after -ne $before) {
                $json.Values.$key = $after
                $changes++
                Write-Host "✔ $relativePath :: $key cleaned" -ForegroundColor Green
            }
        }
    }

    if ($changes -gt 0) {
        $json | ConvertTo-Json -Depth 6 | Out-File $path -Encoding UTF8
        Write-Host "💾 Saved: $relativePath ($changes change(s))" -ForegroundColor Cyan
    } else {
        Write-Host "✓ No changes needed: $relativePath" -ForegroundColor Gray
    }
}

Write-Host "Done." -ForegroundColor Cyan