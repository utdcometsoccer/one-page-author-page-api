param(
    [switch]$WhatIf,
    [switch]$Execute
)

Write-Host "🚨 Git History Secret Removal Tool" -ForegroundColor Red
Write-Host "=================================" -ForegroundColor Red
Write-Host ""

if ($WhatIf) {
    Write-Host "WHAT-IF MODE: Showing what would be done" -ForegroundColor Yellow
    Write-Host ""
}

# Check prerequisites
Write-Host "📋 Checking prerequisites..." -ForegroundColor Cyan

$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host "❌ You have uncommitted changes. Please commit or stash them first:" -ForegroundColor Red
    git status --short
    exit 1
}

$currentBranch = git branch --show-current
Write-Host "✅ Current branch: $currentBranch" -ForegroundColor Green

# Create backup
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupDir = "../backup-$timestamp"

if ($WhatIf) {
    Write-Host "📦 WOULD create backup at: $backupDir" -ForegroundColor Yellow
} else {
    Write-Host "📦 Creating backup..." -ForegroundColor Cyan
    git clone --mirror . $backupDir
    Write-Host "✅ Backup created at: $backupDir" -ForegroundColor Green
}

# Show what secrets were found
Write-Host ""
Write-Host "🔍 Secrets found in history:" -ForegroundColor Red
git log --all --oneline | Select-String "secret|key|password" | ForEach-Object { 
    Write-Host "  $_" -ForegroundColor Yellow 
}

Write-Host ""
Write-Host "📄 Files that might contain secrets:" -ForegroundColor Red
$files = @(
    "InkStainedWretchFunctions/local.settings.json*",
    "InkStainedWretchFunctions/Testing/scenario*.json*",
    "*/Program.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎯 RECOMMENDED ACTION:" -ForegroundColor Magenta
Write-Host ""
Write-Host "Use BFG Repo-Cleaner for safe history rewriting:" -ForegroundColor White
Write-Host "1. Download BFG: https://rtyley.github.io/bfg-repo-cleaner/" -ForegroundColor Gray
Write-Host "2. Create mirror: git clone --mirror [repo-url] temp.git" -ForegroundColor Gray
Write-Host "3. Clean history: java -jar bfg.jar --replace-text replacements.txt temp.git" -ForegroundColor Gray
Write-Host "4. Force push: cd temp.git && git push --force" -ForegroundColor Gray
Write-Host ""

if (-not $Execute -and -not $WhatIf) {
    Write-Host "⚠️  Add -Execute to proceed with automated cleanup or -WhatIf to preview" -ForegroundColor Yellow
    Write-Host "⚠️  Or follow the manual BFG process above for safest results" -ForegroundColor Yellow
    exit 0
}

if ($Execute) {
    Write-Host "🚀 EXECUTING AUTOMATED CLEANUP..." -ForegroundColor Red
    Write-Host ""
    
    # Try to install git-filter-repo if not available
    try {
        git filter-repo --version | Out-Null
        Write-Host "✅ git-filter-repo is available" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ git-filter-repo not found. Install with: pip install git-filter-repo" -ForegroundColor Red
        Write-Host "   Alternative: Use the manual BFG process instead" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "⚠️  This will rewrite Git history permanently!" -ForegroundColor Yellow
    Write-Host "   Backup created at: $backupDir" -ForegroundColor Gray
    Write-Host ""
    
    # Set up user secrets
    Write-Host "🔐 Setting up user secrets..." -ForegroundColor Cyan
    
    $projectPath = "InkStainedWretchFunctions"
    if (Test-Path $projectPath) {
        Push-Location $projectPath
        try {
            dotnet user-secrets init --force
            Write-Host "✅ User secrets initialized for $projectPath" -ForegroundColor Green
        }
        finally {
            Pop-Location
        }
    }
    
    Write-Host ""
    Write-Host "🎉 PREPARATION COMPLETED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 NEXT STEPS:" -ForegroundColor Magenta
    Write-Host "1. Follow STEP_BY_STEP_CLEANUP.md for complete process" -ForegroundColor White
    Write-Host "2. Use BFG Repo-Cleaner to clean history safely" -ForegroundColor White
    Write-Host "3. Add your actual secrets to user secrets after cleanup" -ForegroundColor White
    Write-Host "4. Notify team members to re-clone repository" -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  Backup created at: $backupDir" -ForegroundColor Yellow
}

Write-Host "📖 For detailed instructions, see: STEP_BY_STEP_CLEANUP.md" -ForegroundColor Cyan