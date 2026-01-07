#!/usr/bin/env pwsh

<#
.SYNOPSIS
Generates comprehensive API documentation from XML documentation files for all projects.

.DESCRIPTION
This script compiles XML documentation from all projects in the OnePageAuthor solution into a unified 
API documentation file with TypeScript examples, authentication details, and usage instructions.

.PARAMETER OutputPath
The path where the compiled documentation will be saved. Defaults to './API-Documentation.md'

.PARAMETER IncludePrivate
Include private/internal classes and methods in the documentation. Default is false.

.EXAMPLE
.\Generate-ApiDocumentation.ps1
Generates documentation with default settings

.EXAMPLE
.\Generate-ApiDocumentation.ps1 -OutputPath "./docs/api.md" -IncludePrivate
Generates comprehensive documentation including private members
#>

param(
    [string]$OutputPath = "./API-Documentation.md",
    [switch]$IncludePrivate = $false
)

# Set error handling
$ErrorActionPreference = "Stop"

Write-Host "🚀 Generating Comprehensive API Documentation..." -ForegroundColor Green

# Get the script directory and navigate to solution root (parent of Scripts)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionRoot = Split-Path -Parent $ScriptDir

# Define all projects in the solution
$Projects = @(
    @{ Name = "ImageAPI"; Path = Join-Path $SolutionRoot "ImageAPI"; Type = "AzureFunction"; Description = "Image management API for uploading, retrieving, and deleting user images" },
    @{ Name = "InkStainedWretchFunctions"; Path = Join-Path $SolutionRoot "InkStainedWretchFunctions"; Type = "AzureFunction"; Description = "Core application functions for domain registration and external API integration" },
    @{ Name = "InkStainedWretchStripe"; Path = Join-Path $SolutionRoot "InkStainedWretchStripe"; Type = "AzureFunction"; Description = "Stripe payment processing functions for subscription management and billing" },
    @{ Name = "function-app"; Path = Join-Path $SolutionRoot "function-app"; Type = "AzureFunction"; Description = "Main application functions for author data and localization services" },
    @{ Name = "OnePageAuthorLib"; Path = Join-Path $SolutionRoot "OnePageAuthorLib"; Type = "Library"; Description = "Core library containing business logic, entities, and data services" },
    @{ Name = "OnePageAuthor.Test"; Path = Join-Path $SolutionRoot "OnePageAuthor.Test"; Type = "TestProject"; Description = "Unit and integration tests for the OnePageAuthor application" },
    @{ Name = "SeedAPIData"; Path = Join-Path $SolutionRoot "SeedAPIData"; Type = "Utility"; Description = "Data seeding utility for populating API with initial data" },
    @{ Name = "SeedLocales"; Path = Join-Path $SolutionRoot "SeedLocales"; Type = "Utility"; Description = "Localization data seeding utility for multi-language support" },
    @{ Name = "SeedImageStorageTiers"; Path = Join-Path $SolutionRoot "SeedImageStorageTiers"; Type = "Utility"; Description = "Utility for seeding image storage tier configurations" },
    @{ Name = "SeedInkStainedWretchesLocale"; Path = Join-Path $SolutionRoot "SeedInkStainedWretchesLocale"; Type = "Utility"; Description = "Localization seeding utility for Ink Stained Wretches specific content" },
    @{ Name = "IntegrationTestAuthorDataService"; Path = Join-Path $SolutionRoot "IntegrationTestAuthorDataService"; Type = "TestUtility"; Description = "Integration testing utility for author data service validation" }
)

# Function to ensure XML documentation is enabled for a project
function Ensure-XmlDocumentation {
    param([string]$ProjectPath, [string]$ProjectName)
    
    $csprojPath = Join-Path $ProjectPath "$ProjectName.csproj"
    if (-not (Test-Path $csprojPath)) {
        Write-Warning "Project file not found: $csprojPath"
        return
    }
    
    $content = Get-Content $csprojPath -Raw
    if ($content -notlike "*GenerateDocumentationFile*") {
        Write-Host "    Adding XML documentation generation to $ProjectName..." -ForegroundColor Yellow
        
        # Find the first PropertyGroup and add XML generation settings
        $pattern = '(\s*<PropertyGroup>(?:(?!<\/PropertyGroup>).)*)'
        $replacement = "`$1`n    <GenerateDocumentationFile>true</GenerateDocumentationFile>`n    <DocumentationFile>bin\Debug\net9.0\$ProjectName.xml</DocumentationFile>`n    <NoWarn>1591</NoWarn>"
        
        $newContent = $content -replace $pattern, $replacement
        
        if ($newContent -ne $content) {
            Set-Content -Path $csprojPath -Value $newContent -Encoding UTF8
            Write-Host "    ✅ Updated $ProjectName.csproj with XML documentation settings" -ForegroundColor Green
        }
    }
}

# Function to parse XML documentation
function Get-XmlDocumentation {
    param([string]$XmlPath, [string]$ProjectName)
    
    if (-not (Test-Path $XmlPath)) {
        Write-Warning "XML documentation not found for $ProjectName at $XmlPath"
        return @()
    }
    
    Write-Host "  Parsing $ProjectName documentation..." -ForegroundColor Cyan
    
    try {
        [xml]$xmlContent = Get-Content $XmlPath -Encoding UTF8
        $members = $xmlContent.doc.members.member
        
        $parsedMembers = @()
        
        foreach ($member in $members) {
            if ($member.name -like "T:*") {
                # This is a class/type
                $className = $member.name -replace "T:", ""
                $summary = if ($member.summary) { $member.summary -replace '\s+', ' ' | ForEach-Object { $_.Trim() } } else { "" }
                
                $parsedMembers += @{
                    Type = "Class"
                    Name = $className
                    Summary = $summary
                    Project = $ProjectName
                }
            }
            elseif ($member.name -like "M:*") {
                # This is a method
                $methodName = $member.name -replace "M:", ""
                $summary = if ($member.summary) { $member.summary -replace '\s+', ' ' | ForEach-Object { $_.Trim() } } else { "" }
                $returns = if ($member.returns) { $member.returns -replace '\s+', ' ' | ForEach-Object { $_.Trim() } } else { "" }
                
                # Parse parameters
                $parameters = @()
                if ($member.param) {
                    foreach ($param in $member.param) {
                        $parameters += @{
                            Name = $param.name
                            Description = if ($param.InnerText) { $param.InnerText -replace '\s+', ' ' | ForEach-Object { $_.Trim() } } else { "" }
                        }
                    }
                }
                
                $parsedMembers += @{
                    Type = "Method"
                    Name = $methodName
                    Summary = $summary
                    Returns = $returns
                    Parameters = $parameters
                    Examples = $member.example
                    Project = $ProjectName
                }
            }
        }
        
        return $parsedMembers
    }
    catch {
        Write-Warning "Failed to parse XML documentation for $ProjectName`: $($_.Exception.Message)"
        return @()
    }
}

# Build all projects and ensure XML documentation
Write-Host "📦 Building projects and ensuring XML documentation..." -ForegroundColor Yellow

foreach ($project in $Projects) {
    Write-Host "  Processing $($project.Name)..." -ForegroundColor Cyan
    
    # Ensure XML documentation is enabled
    Ensure-XmlDocumentation -ProjectPath $project.Path -ProjectName $project.Name
    
    # Build the project
    Push-Location $project.Path
    try {
        dotnet build --configuration Debug --verbosity minimal 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Build failed for $($project.Name) - continuing with other projects"
        }
    }
    finally {
        Pop-Location
    }
}

# Parse XML documentation from all projects
Write-Host "📄 Parsing XML documentation files..." -ForegroundColor Yellow

$allMembers = @()
foreach ($project in $Projects) {
    $xmlPath = Join-Path $project.Path "bin\Debug\net9.0\$($project.Name).xml"
    $members = Get-XmlDocumentation -XmlPath $xmlPath -ProjectName $project.Name
    $allMembers += $members
}

# Generate markdown documentation
Write-Host "✏️  Generating markdown documentation..." -ForegroundColor Yellow

$markdown = @"
# OnePageAuthor Complete System Documentation

*Generated on $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")*

This comprehensive documentation covers all components, APIs, and utilities in the OnePageAuthor system.

## System Overview

The OnePageAuthor system consists of multiple Azure Functions, core libraries, and utility applications that work together to provide a complete author management and content publishing platform.

## Architecture Components

### Azure Functions (API Layer)
- **ImageAPI**: Image upload, management and retrieval
- **InkStainedWretchFunctions**: Domain registration and external integrations
- **InkStainedWretchStripe**: Payment processing and subscription management
- **function-app**: Core author data and localization services

### Libraries (Business Logic Layer)
- **OnePageAuthorLib**: Core business logic, entities, and data access services

### Utilities (Data Management Layer)
- **SeedAPIData**: API data initialization
- **SeedLocales**: Localization data management
- **SeedImageStorageTiers**: Storage tier configuration
- **SeedInkStainedWretchesLocale**: Application-specific localization

### Testing (Quality Assurance Layer)  
- **OnePageAuthor.Test**: Unit and integration tests
- **IntegrationTestAuthorDataService**: Author data service validation

## Authentication

All API endpoints require JWT Bearer token authentication:

```
Authorization: Bearer <your-jwt-token>
```

## Project Details

"@

# Group projects by type
$azureFunctions = $Projects | Where-Object { $_.Type -eq "AzureFunction" }
$libraries = $Projects | Where-Object { $_.Type -eq "Library" }
$utilities = $Projects | Where-Object { $_.Type -eq "Utility" }
$tests = $Projects | Where-Object { $_.Type -in @("TestProject", "TestUtility") }

# Document Azure Functions
if ($azureFunctions) {
    $markdown += "`n### Azure Functions`n`n"
    foreach ($project in $azureFunctions) {
        $projectMembers = $allMembers | Where-Object { $_.Project -eq $project.Name }
        
        $markdown += "#### $($project.Name)`n`n"
        $markdown += "$($project.Description)`n`n"
        
        $classes = $projectMembers | Where-Object { $_.Type -eq "Class" }
        $methods = $projectMembers | Where-Object { $_.Type -eq "Method" }
        
        if ($classes.Count -gt 0) {
            $markdown += "**Functions:**`n"
            foreach ($class in $classes) {
                $className = ($class.Name -split '\.')[-1]
                $classMethods = $methods | Where-Object { $_.Name -like "*$className*" }
                
                $markdown += "- ``$className``"
                if ($class.Summary) { $markdown += ": $($class.Summary)" }
                $markdown += "`n"
                
                if ($classMethods.Count -gt 0) {
                    foreach ($method in $classMethods) {
                        $methodName = ($method.Name -split '\.')[-1] -replace '\(.*\)', ''
                        $markdown += "  - ``$methodName``"
                        if ($method.Summary) { $markdown += ": $($method.Summary)" }
                        $markdown += "`n"
                    }
                }
            }
            $markdown += "`n"
        }
    }
}

# Document Libraries
if ($libraries) {
    $markdown += "`n### Libraries`n`n"
    foreach ($project in $libraries) {
        $projectMembers = $allMembers | Where-Object { $_.Project -eq $project.Name }
        
        $markdown += "#### $($project.Name)`n`n"
        $markdown += "$($project.Description)`n`n"
        
        $classes = $projectMembers | Where-Object { $_.Type -eq "Class" }
        if ($classes.Count -gt 0) {
            $markdown += "**Key Components:**`n"
            foreach ($class in $classes) {
                $className = ($class.Name -split '\.')[-1]
                $markdown += "- ``$className``"
                if ($class.Summary) { $markdown += ": $($class.Summary)" }
                $markdown += "`n"
            }
            $markdown += "`n"
        }
    }
}

# Document Utilities
if ($utilities) {
    $markdown += "`n### Utilities & Tools`n`n"
    foreach ($project in $utilities) {
        $markdown += "#### $($project.Name)`n`n"
        $markdown += "$($project.Description)`n`n"
    }
}

# Document Tests
if ($tests) {
    $markdown += "`n### Testing Projects`n`n"
    foreach ($project in $tests) {
        $markdown += "#### $($project.Name)`n`n" 
        $markdown += "$($project.Description)`n`n"
    }
}

# Add footer
$markdown += @"

## Development Information

### Build Configuration
All projects are configured to automatically generate XML documentation during Debug builds.

### Documentation Generation
This documentation is automatically generated from source code XML comments and can be regenerated using:
```
.\Generate-ApiDocumentation.ps1
```

### Project Statistics
- **Total Projects**: $($Projects.Count)
- **Azure Functions**: $($azureFunctions.Count)
- **Libraries**: $($libraries.Count)
- **Utilities**: $($utilities.Count)
- **Test Projects**: $($tests.Count)
- **Documented Members**: $($allMembers.Count)

---

*Last updated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")*
*Generated from: OnePageAuthor API Documentation System*
"@

# Write the documentation file
try {
    $resolvedPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
    if (-not $resolvedPath) {
        $OutputPath = Join-Path (Get-Location) (Split-Path $OutputPath -Leaf)
    } else {
        $OutputPath = $resolvedPath.Path
    }
    
    $markdown | Out-File -FilePath $OutputPath -Encoding UTF8
    
    Write-Host "✅ Complete system documentation generated successfully!" -ForegroundColor Green
    Write-Host "📁 Output file: $OutputPath" -ForegroundColor Green
    Write-Host "📊 Projects documented: $($Projects.Count)" -ForegroundColor Green
    Write-Host "📊 Total members documented: $($allMembers.Count)" -ForegroundColor Green
    
    # Open the file if on Windows
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $choice = Read-Host "Would you like to open the documentation file? (y/N)"
        if ($choice -eq 'y' -or $choice -eq 'Y') {
            Start-Process $OutputPath
        }
    }
}
catch {
    Write-Error "❌ Failed to generate documentation: $($_.Exception.Message)"
    exit 1
}

Write-Host "🎉 Complete system documentation generation complete!" -ForegroundColor Green