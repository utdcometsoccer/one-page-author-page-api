#!/bin/bash

# PowerShell script to start Cosmos DB Emulator on Windows
# For use in PowerShell or from VS Code terminal

Write-Host "Starting Azure Cosmos DB Emulator..." -ForegroundColor Green
Write-Host "This may take a few minutes on first run."
Write-Host

# Check if Cosmos DB Emulator is installed
$emulatorPath = "${env:ProgramFiles}\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"
if (-not (Test-Path $emulatorPath)) {
    Write-Host "Azure Cosmos DB Emulator not found!" -ForegroundColor Red
    Write-Host "Please download and install it from:"
    Write-Host "https://aka.ms/cosmosdb-emulator"
    Write-Host
    Read-Host "Press Enter to exit"
    exit 1
}

# Start the emulator with default settings
Write-Host "Starting emulator process..."
Start-Process -FilePath $emulatorPath -ArgumentList "/NoUI", "/NoExplorer"

Write-Host "Waiting for Cosmos DB Emulator to start..."
Write-Host "This may take 1-2 minutes..."
Write-Host

# Wait for the emulator to be ready (check if port 8081 is listening)
$maxWait = 120 # 2 minutes
$waited = 0
do {
    Start-Sleep -Seconds 10
    $waited += 10
    
    $listening = Get-NetTCPConnection -LocalPort 8081 -ErrorAction SilentlyContinue | Where-Object { $_.State -eq "Listen" }
    if ($listening) {
        break
    }
    
    Write-Host "Still starting... ($waited seconds elapsed)"
    
    if ($waited -ge $maxWait) {
        Write-Host "Emulator is taking longer than expected to start." -ForegroundColor Yellow
        Write-Host "Please check the emulator status manually."
        break
    }
} while (-not $listening)

if ($listening) {
    Write-Host "Cosmos DB Emulator is now running!" -ForegroundColor Green
    Write-Host "Web interface available at: https://localhost:8081/_explorer/index.html"
    Write-Host
    Write-Host "You can now run the Data Seeder:"
    Write-Host "dotnet run --project OnePageAuthor.DataSeeder" -ForegroundColor Cyan
} else {
    Write-Host "Could not confirm emulator startup. Please check manually." -ForegroundColor Yellow
}

Write-Host
Read-Host "Press Enter to continue"