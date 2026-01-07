#!/usr/bin/env pwsh

# Clean up standalone markdown files after integration into README
# This script removes the standalone markdown files that have been integrated

Write-Host "🧹 Cleaning up standalone markdown files..." -ForegroundColor Blue

$repoRoot = "c:\Users\Idahosa\source\repos\one-page-author-page-api"

# Standard GitHub files to keep
$keepFiles = @(
    'CONTRIBUTING.md',
    'CODE_OF_CONDUCT.md', 
    'SECURITY.md',
    'LICENSE.md',
    'README.md'
)

# Get all markdown files in root directory, excluding files to keep
$filesToDelete = Get-ChildItem -Path $repoRoot -Filter "*.md" | 
    Where-Object { $_.Name -notin $keepFiles } |
    Sort-Object Name

Write-Host "📋 Found $($filesToDelete.Count) files to delete:" -ForegroundColor Yellow
$filesToDelete | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Red }

if ($filesToDelete.Count -eq 0) {
    Write-Host "✅ No files to delete!" -ForegroundColor Green
    exit 0
}

Write-Host ""
$confirm = Read-Host "❓ Are you sure you want to delete these $($filesToDelete.Count) files? All content has been integrated into README.md and backed up to docs-backup/ (y/N)"

if ($confirm -eq 'y' -or $confirm -eq 'Y' -or $confirm -eq 'yes') {
    Write-Host ""
    Write-Host "🗑️  Deleting files..." -ForegroundColor Red
    
    foreach ($file in $filesToDelete) {
        try {
            Remove-Item -Path $file.FullName -Force
            Write-Host "  ✓ Deleted: $($file.Name)" -ForegroundColor Gray
        }
        catch {
            Write-Host "  ❌ Failed to delete: $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    Write-Host "✅ Cleanup complete!" -ForegroundColor Green
    Write-Host "💡 All content is now in README.md" -ForegroundColor Yellow
    Write-Host "💡 Original files are backed up in docs-backup/" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ Cleanup cancelled by user" -ForegroundColor Yellow
    Write-Host "💡 Files remain unchanged" -ForegroundColor Cyan
}