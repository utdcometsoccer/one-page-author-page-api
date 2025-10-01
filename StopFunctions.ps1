# PowerShell script to stop all running Azure Functions projects
# This script stops the background jobs started by UpdateAndRun.ps1

param(
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\StopFunctions.ps1 [-Help]" -ForegroundColor Green
    Write-Host ""
    Write-Host "This script will stop all running Azure Functions background jobs:" -ForegroundColor Cyan
    Write-Host "  • ImageAPI"
    Write-Host "  • InkStainedWretchFunctions"
    Write-Host "  • InkStainedWretchStripe"
    exit 0
}

function Write-Step {
    param([string]$Message, [string]$Color = "Green")
    Write-Host "`n=== $Message ===" -ForegroundColor $Color
}

Write-Host "🛑 Stopping Azure Functions Projects" -ForegroundColor Red

# Define the function project names
$FunctionProjects = @("ImageAPI", "InkStainedWretchFunctions", "InkStainedWretchStripe")

# Get all running jobs
$AllJobs = Get-Job

if ($AllJobs.Count -eq 0) {
    Write-Host "ℹ️  No background jobs found." -ForegroundColor Yellow
    exit 0
}

Write-Step "Current Background Jobs" "Cyan"
$AllJobs | Format-Table Id, Name, State, HasMoreData

# Find and stop function jobs
$FunctionJobs = $AllJobs | Where-Object { $FunctionProjects -contains $_.Name }

if ($FunctionJobs.Count -eq 0) {
    Write-Host "ℹ️  No Azure Functions jobs found to stop." -ForegroundColor Yellow
} else {
    Write-Step "Stopping Azure Functions Jobs" "Yellow"
    
    foreach ($Job in $FunctionJobs) {
        Write-Host "🛑 Stopping $($Job.Name) (Job ID: $($Job.Id))..." -ForegroundColor Yellow
        
        try {
            Stop-Job -Job $Job -ErrorAction Stop
            Write-Host "✅ Stopped $($Job.Name)" -ForegroundColor Green
        } catch {
            Write-Host "❌ Failed to stop $($Job.Name): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # Remove the stopped jobs
    Write-Step "Cleaning Up Stopped Jobs" "Cyan"
    
    foreach ($Job in $FunctionJobs) {
        try {
            Remove-Job -Job $Job -Force -ErrorAction Stop
            Write-Host "🗑️  Removed job $($Job.Name)" -ForegroundColor Gray
        } catch {
            Write-Host "❌ Failed to remove job $($Job.Name): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Show remaining jobs
$RemainingJobs = Get-Job
if ($RemainingJobs.Count -gt 0) {
    Write-Step "Remaining Background Jobs" "Gray"
    $RemainingJobs | Format-Table Id, Name, State, HasMoreData
    Write-Host "💡 Use 'Get-Job | Remove-Job' to clean up remaining jobs if needed." -ForegroundColor Cyan
} else {
    Write-Host "✅ All jobs have been stopped and removed." -ForegroundColor Green
}

Write-Host "`n🎉 Azure Functions cleanup completed!" -ForegroundColor Green