# PowerShell script to update, restore, build, and run Azure                    Write-Host "dotnet-update tool is available" -ForegroundColor Green   Write-Host "dotnet-update tool not found. Installing..." -ForegroundColor YellowFunctions projects
# This script updates all NuGet packages, restores dependencies, builds the solution,
# and runs the Azure Functions projects

param(
    [switch]$SkipUpdate,
    [switch]$SkipBuild,
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\UpdateAndRun.ps1 [-SkipUpdate] [-SkipBuild] [-Help]" -ForegroundColor Green
    Write-Host ""
    Write-Host "Parameters:" -ForegroundColor Yellow
    Write-Host "  -SkipUpdate   Skip the dotnet-update step"
    Write-Host "  -SkipBuild    Skip the build step (restore will still run)"
    Write-Host "  -Help         Show this help message"
    Write-Host ""
    Write-Host "This script will:" -ForegroundColor Cyan
    Write-Host "  1. Run dotnet-update -u across the entire solution"
    Write-Host "  2. Run dotnet restore"
    Write-Host "  3. Run dotnet build"
    Write-Host "  4. Start Azure Functions projects: ImageAPI, InkStainedWretchFunctions, InkStainedWretchStripe"
    Write-Host "     - ImageAPI on port 7000"
    Write-Host "     - InkStainedWretchFunctions on port 7001" 
    Write-Host "     - InkStainedWretchStripe on port 7002"
    exit 0
}

# Function to write colored output
function Write-Step {
    param([string]$Message, [string]$Color = "Green")
    Write-Host "`n=== $Message ===" -ForegroundColor $Color
}

function Write-Error-Step {
    param([string]$Message)
    Write-Host "`n❌ ERROR: $Message" -ForegroundColor Red
}

function Write-Success-Step {
    param([string]$Message)
    Write-Host "`n✅ SUCCESS: $Message" -ForegroundColor Green
}

# Get the script directory (solution root)
$SolutionRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $SolutionRoot

Write-Host "🚀 Starting Solution Update and Run Process" -ForegroundColor Magenta
Write-Host "Solution Root: $SolutionRoot" -ForegroundColor Gray

# Step 1: Check prerequisites
Write-Step "Checking Prerequisites" "Yellow"

# Check if dotnet-update is installed
if (-not $SkipUpdate) {
    try {
        $updateVersion = dotnet tool list --global | Select-String "dotnet-outdated"
        if (-not $updateVersion) {
            Write-Host "❌ dotnet-update tool not found. Installing..." -ForegroundColor Yellow
            dotnet tool install --global dotnet-update
            if ($LASTEXITCODE -ne 0) {
                Write-Error-Step "Failed to install dotnet-update tool"
                exit 1
            }
            Write-Success-Step "dotnet-update tool installed"
        } else {
            Write-Host "✅ dotnet-update tool is available" -ForegroundColor Green
        }
    } catch {
        Write-Error-Step "Error checking for dotnet-outdated tool: $($_.Exception.Message)"
        exit 1
    }
}

# Check if Azure Functions Core Tools are installed
try {
    $funcVersion = func --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Azure Functions Core Tools v$funcVersion is installed" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Azure Functions Core Tools not found. Functions may not run properly." -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️  Could not verify Azure Functions Core Tools installation." -ForegroundColor Yellow
}

# Step 2: Update packages (if not skipped)
if (-not $SkipUpdate) {
    Write-Step "Updating NuGet Packages" "Cyan"
    try {
        Write-Host "Running dotnet-update -u across solution..." -ForegroundColor Yellow
        dotnet-outdated --upgrade
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success-Step "Package update completed"
        } else {
            Write-Error-Step "Package update failed with exit code $LASTEXITCODE"
            exit 1
        }
    } catch {
        Write-Error-Step "Error during package update: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Host "⏭️  Skipping package update as requested" -ForegroundColor Yellow
}

# Step 3: Restore packages
Write-Step "Restoring NuGet Packages" "Cyan"
try {
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Success-Step "Package restore completed"
    } else {
        Write-Error-Step "Package restore failed with exit code $LASTEXITCODE"
        exit 1
    }
} catch {
    Write-Error-Step "Error during package restore: $($_.Exception.Message)"
    exit 1
}

# Step 4: Build solution (if not skipped)
if (-not $SkipBuild) {
    Write-Step "Building Solution" "Cyan"
    try {
        dotnet build --no-restore
        if ($LASTEXITCODE -eq 0) {
            Write-Success-Step "Solution build completed"
        } else {
            Write-Error-Step "Solution build failed with exit code $LASTEXITCODE"
            exit 1
        }
    } catch {
        Write-Error-Step "Error during solution build: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Host "⏭️  Skipping build as requested" -ForegroundColor Yellow
}

# Step 5: Start Azure Functions projects
Write-Step "Starting Azure Functions Projects" "Magenta"

# Define the projects to run
$Projects = @(
    @{ Name = "ImageAPI"; Path = "ImageAPI" },
    @{ Name = "InkStainedWretchFunctions"; Path = "InkStainedWretchFunctions" },
    @{ Name = "InkStainedWretchStripe"; Path = "InkStainedWretchStripe" }
)

# Array to store running jobs
$RunningJobs = @()
$PortCounter = 0

foreach ($Project in $Projects) {
    $ProjectPath = Join-Path $SolutionRoot $Project.Path
    
    if (Test-Path $ProjectPath) {
        Write-Host "🚀 Starting $($Project.Name)..." -ForegroundColor Green
        
        $Port = 7000 + $PortCounter
        
        # Start the function app in a new PowerShell job
        $Job = Start-Job -ScriptBlock {
            param($ProjectPath, $ProjectName, $Port)
            Set-Location $ProjectPath
            Write-Host "Starting $ProjectName in $ProjectPath on port $Port"
            func start --port $Port --cors * 
        } -ArgumentList $ProjectPath, $Project.Name, $Port -Name $Project.Name
        
        $RunningJobs += @{
            Job = $Job
            Name = $Project.Name
            Path = $ProjectPath
            Port = $Port
        }
        
        Write-Host "✅ $($Project.Name) started as background job (ID: $($Job.Id)) on port $Port" -ForegroundColor Green
        $PortCounter++
        Start-Sleep -Seconds 2  # Brief pause between starts
    } else {
        Write-Host "⚠️  Project directory not found: $ProjectPath" -ForegroundColor Yellow
    }
}

# Display running jobs information
if ($RunningJobs.Count -gt 0) {
    Write-Step "Running Azure Functions Summary" "Green"
    Write-Host "The following Azure Functions projects are now running:" -ForegroundColor Cyan
    
    foreach ($RunningJob in $RunningJobs) {
        Write-Host "  • $($RunningJob.Name) (Job ID: $($RunningJob.Job.Id))" -ForegroundColor White
    }
    
    Write-Host "`n📋 Management Commands:" -ForegroundColor Yellow
    Write-Host "  • View all jobs:           Get-Job" -ForegroundColor Gray
    Write-Host "  • View job output:         Receive-Job -Id <JobId> -Keep" -ForegroundColor Gray
    Write-Host "  • Stop a specific job:     Stop-Job -Id <JobId>" -ForegroundColor Gray
    Write-Host "  • Stop all function jobs:  Get-Job | Where-Object { @('ImageAPI', 'InkStainedWretchFunctions', 'InkStainedWretchStripe') -contains `$_.Name } | Stop-Job" -ForegroundColor Gray
    Write-Host "  • Remove completed jobs:   Get-Job | Remove-Job" -ForegroundColor Gray
    
    Write-Host "`n🌐 Expected Endpoints (once started):" -ForegroundColor Yellow
    Write-Host "  • ImageAPI:                  http://localhost:7000" -ForegroundColor Cyan
    Write-Host "  • InkStainedWretchFunctions: http://localhost:7001" -ForegroundColor Cyan  
    Write-Host "  • InkStainedWretchStripe:    http://localhost:7002" -ForegroundColor Cyan
    
    Write-Host "`n⏱️  Functions are starting up... This may take 30-60 seconds." -ForegroundColor Yellow
    Write-Host "Use 'Receive-Job -Id <JobId> -Keep' to monitor startup progress." -ForegroundColor Gray
} else {
    Write-Host "❌ No Azure Functions projects were started" -ForegroundColor Red
}

Write-Host "`n🎉 Script execution completed!" -ForegroundColor Magenta
Write-Host "All requested operations have finished. Azure Functions are running in background jobs." -ForegroundColor Green