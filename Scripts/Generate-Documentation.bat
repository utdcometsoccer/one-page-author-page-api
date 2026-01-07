@echo off
setlocal enabledelayedexpansion

echo.
echo ===================================================
echo   OnePageAuthor API Documentation Generator
echo ===================================================
echo.

REM Check if PowerShell is available
where pwsh.exe >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell Core (pwsh.exe) not found
    echo Please install PowerShell Core from: https://github.com/PowerShell/PowerShell
    pause
    exit /b 1
)

REM Build all projects first
echo [1/3] Building projects to generate XML documentation...
echo.

echo Building ImageAPI...
cd /d "%~dp0ImageAPI"
dotnet build --configuration Debug --verbosity minimal
if %errorlevel% neq 0 (
    echo ERROR: Failed to build ImageAPI
    pause
    exit /b 1
)

echo Building InkStainedWretchFunctions...
cd /d "%~dp0InkStainedWretchFunctions"  
dotnet build --configuration Debug --verbosity minimal
if %errorlevel% neq 0 (
    echo ERROR: Failed to build InkStainedWretchFunctions
    pause
    exit /b 1
)

cd /d "%~dp0"

echo.
echo [2/3] Generating API documentation...
echo.

REM Run the PowerShell documentation generator
pwsh.exe -ExecutionPolicy Bypass -File "%~dp0Generate-ApiDocumentation.ps1" -OutputPath "%~dp0API-Documentation.md"
if %errorlevel% neq 0 (
    echo ERROR: Failed to generate API documentation
    pause
    exit /b 1
)

echo.
echo [3/3] Documentation generation complete!
echo.
echo Output file: %~dp0API-Documentation.md
echo.

REM Ask if user wants to open the documentation
set /p "openfile=Would you like to open the documentation file? (Y/N): "
if /i "!openfile!"=="Y" (
    start "" "%~dp0API-Documentation.md"
)

echo.
echo Press any key to exit...
pause >nul