@echo off
echo Starting Azure Cosmos DB Emulator...
echo This may take a few minutes on first run.
echo.

REM Check if Cosmos DB Emulator is installed
if not exist "%ProgramFiles%\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" (
    echo Azure Cosmos DB Emulator not found!
    echo Please download and install it from:
    echo https://aka.ms/cosmosdb-emulator
    echo.
    pause
    exit /b 1
)

REM Start the emulator with default settings
start "" "%ProgramFiles%\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /NoUI /NoExplorer

echo Waiting for Cosmos DB Emulator to start...
echo This may take 1-2 minutes...
echo.

REM Wait for the emulator to be ready (check if port 8081 is listening)
:WAIT_LOOP
timeout /t 10 /nobreak > nul
netstat -an | find "8081" | find "LISTENING" > nul
if errorlevel 1 (
    echo Still starting...
    goto WAIT_LOOP
)

echo Cosmos DB Emulator is now running!
echo Web interface available at: https://localhost:8081/_explorer/index.html
echo.
echo You can now run the Data Seeder:
echo dotnet run --project OnePageAuthor.DataSeeder
echo.
pause