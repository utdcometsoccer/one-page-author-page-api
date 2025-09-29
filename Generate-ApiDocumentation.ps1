#!/usr/bin/env pwsh

<#
.SYNOPSIS
Generates comprehensive API documentation from XML documentation files.

.DESCRIPTION
This script compiles XML documentation from all Azure Function projects into a unified 
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

Write-Host "🚀 Generating API Documentation..." -ForegroundColor Green

# Define projects and their XML documentation paths
$Projects = @(
    @{
        Name = "ImageAPI"
        Path = "./ImageAPI"
        XmlPath = "./ImageAPI/bin/Debug/net9.0/ImageAPI.xml"
        Description = "Image management API for uploading, retrieving, and deleting user images"
        Type = "AzureFunction"
    },
    @{
        Name = "InkStainedWretchFunctions"
        Path = "./InkStainedWretchFunctions"
        XmlPath = "./InkStainedWretchFunctions/bin/Debug/net9.0/InkStainedWretchFunctions.xml"
        Description = "Core application functions for domain registration and external API integration"
        Type = "AzureFunction"
    },
    @{
        Name = "InkStainedWretchStripe"
        Path = "./InkStainedWretchStripe"
        XmlPath = "./InkStainedWretchStripe/bin/Debug/net9.0/InkStainedWretchStripe.xml"
        Description = "Stripe payment processing functions for subscription management and billing"
        Type = "AzureFunction"
    },
    @{
        Name = "function-app"
        Path = "./function-app"
        XmlPath = "./function-app/bin/Debug/net9.0/function-app.xml"
        Description = "Main application functions for author data and localization services"
        Type = "AzureFunction"
    },
    @{
        Name = "OnePageAuthorLib"
        Path = "./OnePageAuthorLib"
        XmlPath = "./OnePageAuthorLib/bin/Debug/net9.0/OnePageAuthorLib.xml"
        Description = "Core library containing business logic, entities, and data services"
        Type = "Library"
    },
    @{
        Name = "OnePageAuthor.Test"
        Path = "./OnePageAuthor.Test"
        XmlPath = "./OnePageAuthor.Test/bin/Debug/net9.0/OnePageAuthor.Test.xml"
        Description = "Unit and integration tests for the OnePageAuthor application"
        Type = "TestProject"
    },
    @{
        Name = "SeedAPIData"
        Path = "./SeedAPIData"
        XmlPath = "./SeedAPIData/bin/Debug/net9.0/SeedAPIData.xml"
        Description = "Data seeding utility for populating API with initial data"
        Type = "Utility"
    },
    @{
        Name = "SeedLocales"
        Path = "./SeedLocales"
        XmlPath = "./SeedLocales/bin/Debug/net9.0/SeedLocales.xml"
        Description = "Localization data seeding utility for multi-language support"
        Type = "Utility"
    },
    @{
        Name = "SeedImageStorageTiers"
        Path = "./SeedImageStorageTiers"
        XmlPath = "./SeedImageStorageTiers/bin/Debug/net9.0/SeedImageStorageTiers.xml"
        Description = "Utility for seeding image storage tier configurations"
        Type = "Utility"
    },
    @{
        Name = "SeedInkStainedWretchesLocale"
        Path = "./SeedInkStainedWretchesLocale"
        XmlPath = "./SeedInkStainedWretchesLocale/bin/Debug/net9.0/SeedInkStainedWretchesLocale.xml"
        Description = "Localization seeding utility for Ink Stained Wretches specific content"
        Type = "Utility"
    },
    @{
        Name = "IntegrationTestAuthorDataService"
        Path = "./IntegrationTestAuthorDataService"
        XmlPath = "./IntegrationTestAuthorDataService/bin/Debug/net9.0/IntegrationTestAuthorDataService.xml"
        Description = "Integration testing utility for author data service validation"
        Type = "TestUtility"
    }
)

# Function to ensure XML documentation is enabled for a project
function Ensure-XmlDocumentation {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
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

# Build all projects to generate XML documentation
Write-Host "📦 Ensuring XML documentation is enabled and building projects..." -ForegroundColor Yellow

foreach ($project in $Projects) {
    Write-Host "  Processing $($project.Name)..." -ForegroundColor Cyan
    
    # Ensure XML documentation is enabled
    Ensure-XmlDocumentation -ProjectPath $project.Path -ProjectName $project.Name
    
    # Build the project
    Push-Location $project.Path
    try {
        dotnet build --configuration Debug --verbosity minimal
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Build failed for $($project.Name) - continuing with other projects"
        }
    }
    finally {
        Pop-Location
    }
}

# Function to parse XML documentation
function Parse-XmlDocumentation {
    param(
        [string]$XmlPath,
        [string]$ProjectName
    )
    
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
            if ($member.name -like "T:*" -and $member.name -like "*Function*") {
                # This is a class/type - likely an Azure Function class
                $className = $member.name -replace "T:", ""
                $summary = $member.summary -replace '\s+', ' ' | ForEach-Object { $_.Trim() }
                $remarks = $member.remarks -replace '\s+', ' ' | ForEach-Object { $_.Trim() }
                
                $parsedMembers += @{
                    Type = "Class"
                    Name = $className
                    Summary = $summary
                    Remarks = $remarks
                    Examples = $member.example
                    Project = $ProjectName
                }
            }
            elseif ($member.name -like "M:*" -and $member.name -like "*Function*") {
                # This is a method - likely an Azure Function method
                $methodName = $member.name -replace "M:", ""
                $summary = $member.summary -replace '\s+', ' ' | ForEach-Object { $_.Trim() }
                $returns = $member.returns -replace '\s+', ' ' | ForEach-Object { $_.Trim() }
                $remarks = $member.remarks -replace '\s+', ' ' | ForEach-Object { $_.Trim() }
                
                # Parse parameters
                $parameters = @()
                if ($member.param) {
                    foreach ($param in $member.param) {
                        $parameters += @{
                            Name = $param.name
                            Description = $param.InnerText -replace '\s+', ' ' | ForEach-Object { $_.Trim() }
                        }
                    }
                }
                
                $parsedMembers += @{
                    Type = "Method"
                    Name = $methodName
                    Summary = $summary
                    Returns = $returns
                    Remarks = $remarks
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

# Generate markdown documentation
function Generate-MarkdownContent {
    param(
        [array]$AllMembers
    )
    
    $markdown = @"
# OnePageAuthor API Documentation

*Generated on $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")*

This comprehensive API documentation covers all Azure Functions and endpoints available in the OnePageAuthor system.

## Table of Contents

- [Authentication](#authentication)
- [Image API](#image-api)
- [Domain Registration API](#domain-registration-api)
- [External Integration API](#external-integration-api)
- [Error Handling](#error-handling)
- [TypeScript Examples](#typescript-examples)

## Authentication

All API endpoints require authentication using JWT Bearer tokens. Include the token in the Authorization header:

``````http
Authorization: Bearer <your-jwt-token>
``````

### TypeScript Authentication Helper

``````typescript
class ApiClient {
  private baseUrl: string;
  private token: string;

  constructor(baseUrl: string, token: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
    this.token = token;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${'$'}{this.baseUrl}${'$'}{endpoint}`;
    
    const response = await fetch(url, {
      ...options,
      headers: {
        'Authorization': `Bearer ${'$'}{this.token}`,
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    if (response.status === 401) {
      throw new Error('Authentication failed - token may be expired');
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Unknown error' }));
      throw new Error(error.error || `HTTP ${'$'}{response.status}: ${'$'}{response.statusText}`);
    }

    return response.json();
  }

  public get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  public post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  public delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}
``````

"@

    # Group members by project type and then by project
    $projectGroups = $AllMembers | Group-Object -Property Project
    
    # Group projects by type for better organization
    $azureFunctionProjects = @()
    $libraryProjects = @()
    $utilityProjects = @()
    $testProjects = @()
    
    foreach ($group in $projectGroups) {
        $projectName = $group.Name
        $projectInfo = $Projects | Where-Object { $_.Name -eq $projectName }
        
        switch ($projectInfo.Type) {
            "AzureFunction" { $azureFunctionProjects += $group }
            "Library" { $libraryProjects += $group }
            "Utility" { $utilityProjects += $group }
            "TestProject" { $testProjects += $group }
            "TestUtility" { $testProjects += $group }
            default { $libraryProjects += $group }
        }
    }
    
    # Generate Azure Functions documentation
    if ($azureFunctionProjects.Count -gt 0) {
        $markdown += "`n`n## Azure Functions API`n`n"
        $markdown += "The following Azure Functions provide the core API endpoints for the OnePageAuthor system:`n`n"
        
        foreach ($group in $azureFunctionProjects) {
            $projectName = $group.Name
            $projectMembers = $group.Group
            $projectDescription = ($Projects | Where-Object { $_.Name -eq $projectName }).Description
            
            $markdown += "### $projectName`n`n"
            if ($projectDescription) {
                $markdown += "$projectDescription`n`n"
            }
            
            # Process Azure Function classes and methods
            $classes = $projectMembers | Where-Object { $_.Type -eq "Class" }
            $methods = $projectMembers | Where-Object { $_.Type -eq "Method" }
            
            foreach ($class in $classes) {
                $className = ($class.Name -split '\.')[-1]
                $markdown += "#### $className`n`n"
                
                if ($class.Summary) {
                    $markdown += "$($class.Summary)`n`n"
                }
                
                # Find methods for this class
                $classMethods = $methods | Where-Object { $_.Name -like "*$className*" }
                
                foreach ($method in $classMethods) {
                    $methodName = ($method.Name -split '\.')[-1] -replace '\(.*\)', ''
                    $markdown += "##### $methodName`n`n"
                    
                    if ($method.Summary) {
                        $markdown += "**Description:** $($method.Summary)`n`n"
                    }
                    
                    if ($method.Parameters -and $method.Parameters.Count -gt 0) {
                        $markdown += "**Parameters:**`n"
                        foreach ($param in $method.Parameters) {
                            $markdown += "- ``$($param.Name)``: $($param.Description)`n"
                        }
                        $markdown += "`n"
                    }
                    
                    if ($method.Returns) {
                        $markdown += "**Returns:** $($method.Returns)`n`n"
                    }
                    
                    if ($method.Remarks) {
                        $markdown += "$($method.Remarks)`n`n"
                    }
                    
                    if ($method.Examples) {
                        $markdown += "$($method.Examples)`n`n"
                    }
                    
                    $markdown += "---`n`n"
                }
            }
        }
    }
    
    # Generate Libraries documentation
    if ($libraryProjects.Count -gt 0) {
        $markdown += "`n`n## Libraries & Core Components`n`n"
        $markdown += "The following libraries provide the foundational components and business logic:`n`n"
        
        foreach ($group in $libraryProjects) {
            $projectName = $group.Name
            $projectMembers = $group.Group
            $projectDescription = ($Projects | Where-Object { $_.Name -eq $projectName }).Description
            
            $markdown += "### $projectName`n`n"
            if ($projectDescription) {
                $markdown += "$projectDescription`n`n"
            }
            
            # Process library classes and methods more concisely
            $classes = $projectMembers | Where-Object { $_.Type -eq "Class" }
            if ($classes.Count -gt 0) {
                $markdown += "**Key Classes:**`n"
                foreach ($class in $classes) {
                    $className = ($class.Name -split '\.')[-1]
                    $summary = if ($class.Summary) { " - $($class.Summary)" } else { "" }
                    $markdown += "- ``$className``$summary`n"
                }
                $markdown += "`n"
            }
        }
    }
    
    # Generate Utilities documentation  
    if ($utilityProjects.Count -gt 0) {
        $markdown += "`n`n## Utilities & Tools`n`n"
        $markdown += "The following utilities provide data seeding and maintenance functionality:`n`n"
        
        foreach ($group in $utilityProjects) {
            $projectName = $group.Name
            $projectDescription = ($Projects | Where-Object { $_.Name -eq $projectName }).Description
            
            $markdown += "### $projectName`n`n"
            if ($projectDescription) {
                $markdown += "$projectDescription`n`n"
            }
        }
    }
    
    # Generate Test documentation
    if ($testProjects.Count -gt 0) {
        $markdown += "`n`n## Testing & Validation`n`n"
        $markdown += "The following projects provide comprehensive testing coverage:`n`n"
        
        foreach ($group in $testProjects) {
            $projectName = $group.Name
            $projectDescription = ($Projects | Where-Object { $_.Name -eq $projectName }).Description
            
            $markdown += "### $projectName`n`n"
            if ($projectDescription) {
                $markdown += "$projectDescription`n`n"
            }
        }
    }
        
        # Group by class first, then methods
        $classes = $projectMembers | Where-Object { $_.Type -eq "Class" }
        $methods = $projectMembers | Where-Object { $_.Type -eq "Method" }
        
        foreach ($class in $classes) {
            $className = ($class.Name -split '\.')[-1]  # Get just the class name
            $markdown += "### $className`n`n"
            
            if ($class.Summary) {
                $markdown += "$($class.Summary)`n`n"
            }
            
            if ($class.Remarks) {
                $markdown += "$($class.Remarks)`n`n"
            }
            
            # Find methods for this class
            $classMethods = $methods | Where-Object { $_.Name -like "*$className*" }
            
            foreach ($method in $classMethods) {
                $methodName = ($method.Name -split '\.')[-1] -replace '\(.*\)', ''
                $markdown += "#### $methodName`n`n"
                
                if ($method.Summary) {
                    $markdown += "**Description:** $($method.Summary)`n`n"
                }
                
                if ($method.Parameters -and $method.Parameters.Count -gt 0) {
                    $markdown += "**Parameters:**`n"
                    foreach ($param in $method.Parameters) {
                        $markdown += "- ``$($param.Name)``: $($param.Description)`n"
                    }
                    $markdown += "`n"
                }
                
                if ($method.Returns) {
                    $markdown += "**Returns:** $($method.Returns)`n`n"
                }
                
                if ($method.Remarks) {
                    $markdown += "$($method.Remarks)`n`n"
                }
                
                if ($method.Examples) {
                    $markdown += "$($method.Examples)`n`n"
                }
                
                $markdown += "---`n`n"
            }
        }
    }
    
    # Add common error handling section
    $markdown += @"

## Error Handling

All API endpoints return consistent error responses:

``````json
{
  "error": "Descriptive error message"
}
``````

### Common HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Invalid or missing token |
| 402 | Payment Required - Subscription limit exceeded |
| 403 | Forbidden - Access denied |
| 404 | Not Found - Resource not found |
| 409 | Conflict - Resource already exists |
| 500 | Internal Server Error |
| 507 | Insufficient Storage - Storage quota exceeded |

### TypeScript Error Handling

``````typescript
interface ApiError {
  error: string;
  details?: any;
}

class ApiException extends Error {
  public statusCode: number;
  public apiError: ApiError;

  constructor(statusCode: number, apiError: ApiError) {
    super(apiError.error);
    this.statusCode = statusCode;
    this.apiError = apiError;
  }
}

// Usage in async functions
try {
  const result = await apiClient.get('/api/images/user');
} catch (error) {
  if (error instanceof ApiException) {
    switch (error.statusCode) {
      case 401:
        // Redirect to login
        window.location.href = '/login';
        break;
      case 403:
        // Show upgrade prompt
        showUpgradePrompt();
        break;
      default:
        // Show general error
        showErrorMessage(error.message);
    }
  }
}
``````

## Rate Limiting

API endpoints may be rate-limited based on subscription tier:

- **Starter**: 100 requests/minute
- **Pro**: 1000 requests/minute  
- **Elite**: 10000 requests/minute

Rate limit headers are included in responses:

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1640995200
```

---

*This documentation is automatically generated from source code XML comments. Last updated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")*
"@

    return $markdown
}

# Main execution
try {
    Write-Host "📄 Parsing XML documentation files..." -ForegroundColor Yellow
    
    $allMembers = @()
    
    foreach ($project in $Projects) {
        $members = Parse-XmlDocumentation -XmlPath $project.XmlPath -ProjectName $project.Name
        $allMembers += $members
    }
    
    Write-Host "✏️  Generating markdown documentation..." -ForegroundColor Yellow
    
    $markdownContent = Generate-MarkdownContent -AllMembers $allMembers
    
    # Write the documentation file
    $resolvedPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
    if (-not $resolvedPath) {
        $OutputPath = Join-Path (Get-Location) (Split-Path $OutputPath -Leaf)
    } else {
        $OutputPath = $resolvedPath.Path
    }
    
    $markdownContent | Out-File -FilePath $OutputPath -Encoding UTF8
    
    Write-Host "✅ API documentation generated successfully!" -ForegroundColor Green
    Write-Host "📁 Output file: $OutputPath" -ForegroundColor Green
    Write-Host "📊 Documented members: $($allMembers.Count)" -ForegroundColor Green
    
    # Open the file if on Windows
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        $choice = Read-Host "Would you like to open the documentation file? (y/N)"
        if ($choice -eq 'y' -or $choice -eq 'Y') {
            Start-Process $OutputPath
        }
    }
}
catch {
    Write-Error "❌ Failed to generate API documentation: $($_.Exception.Message)"
    exit 1
}

Write-Host "🎉 Documentation generation complete!" -ForegroundColor Green