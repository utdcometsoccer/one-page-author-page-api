@echo off
setlocal enabledelayedexpansion

echo.
echo ========================================================
echo   OnePageAuthor Complete System Documentation Generator
echo ========================================================
echo.

REM Check if PowerShell is available
where pwsh.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: PowerShell Core ^(pwsh.exe^) not found
    echo Please install PowerShell Core from: https://github.com/PowerShell/PowerShell
    pause
    exit /b 1
)

echo [1/1] Generating complete system documentation for all 11 projects...
echo.

REM Run the PowerShell documentation generator
pwsh.exe -ExecutionPolicy Bypass -File "%~dp0Generate-Complete-Documentation.ps1"
if errorlevel 1 (
    echo ERROR: Failed to generate system documentation
    pause
    exit /b 1
)

echo.
echo ========================================================
echo   Documentation generation complete!
echo ========================================================
echo.
echo Generated files:
echo   - Complete-System-Documentation.md (all 11 projects)
echo   - API-Documentation.md (Azure Functions only)
echo.
echo Statistics:
echo   - Total Projects: 11
echo   - Azure Functions: 4
echo   - Libraries: 1  
echo   - Utilities: 4
echo   - Test Projects: 2
echo   - Documented Members: ~300
echo.

REM Ask if user wants to open the documentation
set /p "openfile=Would you like to open the complete documentation file? (Y/N): "
if /i "!openfile!"=="Y" (
    start "" "%~dp0..\Complete-System-Documentation.md"
)

echo.
echo Press any key to exit...
pause >nul