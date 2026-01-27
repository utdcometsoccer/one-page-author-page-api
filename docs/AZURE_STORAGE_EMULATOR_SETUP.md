# Azure Storage Emulator Launch Configuration

## Overview

Added a new VS Code launch configuration to easily start the Azure Storage Emulator (Azurite) for local development.

## Prerequisites

### Install Azurite

You need to have Azurite installed globally:

```bash
npm install -g azurite

```

Or if you prefer using it locally in the project:

```bash
npm install --save-dev azurite

```

## Launch Configurations Added

### 1. Launch Azure Storage Emulator

**Name**: `Launch Azure Storage Emulator`

- **Purpose**: Starts Azurite (Azure Storage Emulator) for local development
- **Location**: Stores data in `${workspaceFolder}/.azurite`
- **Debug Log**: Saves debug information to `${workspaceFolder}/.azurite/debug.log`
- **Mode**: Silent mode (reduced console output)

### 2. Launch All Services (with Storage)

**Name**: `Launch All Services (with Storage)`

- **Purpose**: Compound configuration that starts:

  1. Azure Storage Emulator
  2. ImageAPI Functions
  3. InkStainedWretchFunctions
  4. InkStainedWretchStripe Functions

## How to Use

### Option 1: Launch Storage Emulator Only

1. Open VS Code
2. Go to Run and Debug (Ctrl+Shift+D)
3. Select "Launch Azure Storage Emulator"
4. Click the green play button

### Option 2: Launch All Services Including Storage

1. Open VS Code
2. Go to Run and Debug (Ctrl+Shift+D)
3. Select "Launch All Services (with Storage)"
4. Click the green play button

## Storage Emulator Details

### Default Endpoints

When Azurite starts, it provides these endpoints:

- **Blob Service**: `http://127.0.0.1:10000/{account}`
- **Queue Service**: `http://127.0.0.1:10001/{account}`
- **Table Service**: `http://127.0.0.1:10002/{account}`

### Connection String

Use this connection string in your applications:

```text
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;

```

### Data Persistence

- **Storage Location**: `${workspaceFolder}/.azurite/`
- **Debug Logs**: `${workspaceFolder}/.azurite/debug.log`
- **Data Files**: Azurite creates `.blob`, `.queue`, and `.table` files

## Configuration Details

```json
{
    "name": "Launch Azure Storage Emulator",
    "type": "node",
    "request": "launch",
    "program": "azurite",
    "args": [
        "--silent",
        "--location", "${workspaceFolder}/.azurite",
        "--debug", "${workspaceFolder}/.azurite/debug.log"
    ],
    "cwd": "${workspaceFolder}",
    "console": "integratedTerminal"
}

```

### Arguments Explained

- `--silent`: Reduces console output for cleaner logs
- `--location`: Specifies where to store emulator data
- `--debug`: Enables debug logging to specified file

## Troubleshooting

### Port Already in Use

If you get port conflicts, you can customize the ports:

1. Edit the launch configuration
2. Add port arguments:

   ```json

   "args": [
       "--blobPort", "10000",
       "--queuePort", "10001",
       "--tablePort", "10002",
       "--silent",
       "--location", "${workspaceFolder}/.azurite",
       "--debug", "${workspaceFolder}/.azurite/debug.log"
   ]

   ```

### Azurite Not Found

If you get "azurite command not found":

1. Install globally:

pm install -g azurite`

1. Or update the program path to local installation:

   ```json

   "program": "${workspaceFolder}/node_modules/.bin/azurite"

   ```

### Clear Storage Data

To reset the emulator data:

1. Stop the emulator
2. Delete the `.azurite` folder
3. Restart the emulator

## Integration with Functions

Your Azure Functions can connect to the local storage emulator using:

### In local.settings.json

```json
{
  "Values": {
    "AzureWebJobsStorage": "your-connection-string",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}

```

### Or with explicit connection string

```json
{
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
  }
}

```

## Benefits

1. **Easy Development**: One-click storage emulator startup
2. **Integrated Workflow**: Combined with Function Apps debugging
3. **Persistent Data**: Data survives emulator restarts
4. **Debug Logs**: Easy troubleshooting with debug output
5. **Team Consistency**: Standardized development environment

This configuration makes local Azure Storage development much more convenient and integrated with your VS Code workflow!
