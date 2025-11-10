#!/usr/bin/env pwsh

# Transform standalone markdown files into README sections
# Excludes standard GitHub files: CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md, LICENSE.md, README.md

Write-Host "🔄 Transforming standalone markdown files into README sections..." -ForegroundColor Blue

$repoRoot = "c:\Users\Idahosa\source\repos\one-page-author-page-api"
$readmePath = Join-Path $repoRoot "README.md"

# Standard GitHub files to exclude
$excludeFiles = @(
    'CONTRIBUTING.md',
    'CODE_OF_CONDUCT.md', 
    'SECURITY.md',
    'LICENSE.md',
    'README.md',
    'README-Documentation.md'
)

# Get all markdown files in root directory, excluding standard GitHub files
$markdownFiles = Get-ChildItem -Path $repoRoot -Filter "*.md" | 
    Where-Object { $_.Name -notin $excludeFiles } |
    Sort-Object Name

Write-Host "📋 Found $($markdownFiles.Count) markdown files to transform:" -ForegroundColor Green
$markdownFiles | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Yellow }

# Read current README content
$readmeContent = Get-Content -Path $readmePath -Raw

# Find insertion point (before "## 📄 License" section)
$licenseSection = "## 📄 License"
$insertionPoint = $readmeContent.IndexOf($licenseSection)

if ($insertionPoint -eq -1) {
    Write-Host "❌ Could not find License section in README.md" -ForegroundColor Red
    exit 1
}

# Build new sections content
$newSections = @()

# Categorize files for better organization
$categories = @{
    "Implementation Summaries" = @(
        'IMPLEMENTATION_SUMMARY.md',
        'IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md',
        'IMPLEMENTATION_SUMMARY_LANGUAGES.md',
        'COUNTRIES_IMPLEMENTATION_SUMMARY.md',
        'GOOGLE_DOMAINS_IMPLEMENTATION_SUMMARY.md',
        'STATEPROVINCE_BOILERPLATE_SUMMARY.md'
    )
    "Enhancement Documentation" = @(
        'ACTIVE_PRODUCTS_FILTER_ENHANCEMENT.md',
        'CULTURE_SUPPORT_ENHANCEMENT.md',
        'LABEL_VALIDATION_ENHANCEMENT.md',
        'SUBSCRIPTION_PLAN_SERVICE_REFACTORING.md'
    )
    "Configuration & Setup" = @(
        'ConfigurationValidation.md',
        'ConfigurationMaskingStandardization.md',
        'AZURE_STORAGE_EMULATOR_SETUP.md',
        'FIND_PARTNER_TAG.md'
    )
    "Migration & Guides" = @(
        'MIGRATION_GUIDE_ENTRA_ID_ROLES.md',
        'TESTING_SCENARIOS_GUIDE.md',
        'STEP_BY_STEP_CLEANUP.md'
    )
    "API & System Documentation" = @(
        'API_DOCUMENTATION.md',
        'API-Documentation.md',
        'Complete-System-Documentation.md',
        'LocalizationREADME.md',
        'DEVELOPMENT_SCRIPTS.md'
    )
    "Development & Maintenance" = @(
        'REFACTORING_SUMMARY.md',
        'SECURITY_AUDIT_REPORT.md',
        'UPDATE_STRIPE_PRICE_NICKNAME_EXAMPLES.md',
        'INK_STAINED_WRETCH_USER_FEATURES.md'
    )
}

# Function to convert markdown content to section
function Convert-MarkdownToSection {
    param(
        [string]$FilePath,
        [string]$FileName
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "⚠️  File not found: $FilePath" -ForegroundColor Yellow
        return ""
    }
    
    $content = Get-Content -Path $FilePath -Raw
    
    # Remove the main title (first # header) and convert subsequent headers
    $lines = $content -split "`n"
    $processedLines = @()
    $skipFirstHeader = $true
    
    foreach ($line in $lines) {
        if ($line -match '^# (.+)$' -and $skipFirstHeader) {
            # Skip the first main header
            $skipFirstHeader = $false
            continue
        } elseif ($line -match '^(#{1,6}) (.+)$') {
            # Add one more # to all headers to make them sub-sections
            $headerLevel = $matches[1]
            $headerText = $matches[2]
            $processedLines += "$headerLevel# $headerText"
        } else {
            $processedLines += $line
        }
    }
    
    return ($processedLines -join "`n").Trim()
}

# Build sections for each category
foreach ($categoryName in $categories.Keys) {
    $categoryFiles = $categories[$categoryName] | Where-Object { $_ -in $markdownFiles.Name }
    
    if ($categoryFiles.Count -eq 0) {
        continue
    }
    
    $newSections += ""
    $newSections += "## 📖 $categoryName"
    $newSections += ""
    
    foreach ($fileName in $categoryFiles) {
        $filePath = Join-Path $repoRoot $fileName
        $sectionTitle = $fileName -replace '\.md$', '' -replace '_', ' ' -replace '-', ' '
        $sectionTitle = (Get-Culture).TextInfo.ToTitleCase($sectionTitle.ToLower())
        
        Write-Host "  📄 Processing: $fileName -> $sectionTitle" -ForegroundColor Cyan
        
        $newSections += "### $sectionTitle"
        $newSections += ""
        
        $sectionContent = Convert-MarkdownToSection -FilePath $filePath -FileName $fileName
        if ($sectionContent) {
            $newSections += $sectionContent
        } else {
            $newSections += "*Content not available*"
        }
        
        $newSections += ""
        $newSections += "---"
        $newSections += ""
    }
}

# Handle any uncategorized files
$uncategorizedFiles = $markdownFiles | Where-Object { $_.Name -notin ($categories.Values | ForEach-Object { $_ }) }

if ($uncategorizedFiles.Count -gt 0) {
    $newSections += ""
    $newSections += "## 📋 Additional Documentation"
    $newSections += ""
    
    foreach ($file in $uncategorizedFiles) {
        $sectionTitle = $file.BaseName -replace '_', ' ' -replace '-', ' '
        $sectionTitle = (Get-Culture).TextInfo.ToTitleCase($sectionTitle.ToLower())
        
        Write-Host "  📄 Processing uncategorized: $($file.Name) -> $sectionTitle" -ForegroundColor Cyan
        
        $newSections += "### $sectionTitle"
        $newSections += ""
        
        $sectionContent = Convert-MarkdownToSection -FilePath $file.FullName -FileName $file.Name
        if ($sectionContent) {
            $newSections += $sectionContent
        } else {
            $newSections += "*Content not available*"
        }
        
        $newSections += ""
        $newSections += "---"
        $newSections += ""
    }
}

# Construct new README content
$beforeInsertion = $readmeContent.Substring(0, $insertionPoint).TrimEnd()
$afterInsertion = $readmeContent.Substring($insertionPoint)

# Remove the old "Additional Documentation" section if it exists
$additionalDocsPattern = '## 📚 Additional Documentation.*?(?=## |\z)'
$afterInsertion = $afterInsertion -replace $additionalDocsPattern, '', 'Singleline'

$newReadmeContent = $beforeInsertion + "`n`n" + ($newSections -join "`n") + "`n`n" + $afterInsertion.TrimStart()

# Write new README
Set-Content -Path $readmePath -Value $newReadmeContent -Encoding UTF8

Write-Host ""
Write-Host "✅ Successfully transformed $($markdownFiles.Count) markdown files into README sections!" -ForegroundColor Green
Write-Host "📝 Updated README.md with $($categories.Count) categories" -ForegroundColor Green

# Create backup of original files
$backupDir = Join-Path $repoRoot "docs-backup"
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
}

Write-Host ""
Write-Host "📦 Creating backup of original files in docs-backup/" -ForegroundColor Blue

foreach ($file in $markdownFiles) {
    $backupPath = Join-Path $backupDir $file.Name
    Copy-Item -Path $file.FullName -Destination $backupPath -Force
    Write-Host "  ✓ Backed up: $($file.Name)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🎉 Transformation complete!" -ForegroundColor Green
Write-Host "💡 Original files backed up to docs-backup/" -ForegroundColor Yellow
Write-Host "💡 You can now delete the original standalone markdown files if desired" -ForegroundColor Yellow