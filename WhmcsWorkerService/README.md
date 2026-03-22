# WHMCS Worker Service

A .NET 10 background worker that dequeues domain-registration messages from an Azure Service Bus queue and calls the WHMCS REST API to register the domain and update its name servers.

## Table of Contents

- [Why This Service Exists](#why-this-service-exists)
- [Architecture Overview](#architecture-overview)
- [Prerequisites](#prerequisites)
- [Full Deployment Guide](#full-deployment-guide)
  1. [Provision an Azure VM with a static outbound IP](#1-provision-an-azure-vm-with-a-static-outbound-ip)
  2. [Find and record the static IP address](#2-find-and-record-the-static-ip-address)
  3. [Create the Azure Service Bus queue](#3-create-the-azure-service-bus-queue)
  4. [Prepare the VM](#4-prepare-the-vm)
  5. [Build and publish the service](#5-build-and-publish-the-service)
  6. [Deploy to the VM](#6-deploy-to-the-vm)
  7. [Configure environment variables](#7-configure-environment-variables)
  8. [Install and start the systemd service](#8-install-and-start-the-systemd-service)
  9. [Configure WHMCS API credentials with IP allowlist](#9-configure-whmcs-api-credentials-with-ip-allowlist)
  10. [Verify end-to-end](#10-verify-end-to-end)
- [Configuration Reference](#configuration-reference)
- [Routine Maintenance](#routine-maintenance)
- [Monitoring and Observability](#monitoring-and-observability)
  - [systemd journal (real-time)](#systemd-journal-real-time)
  - [Logging levels and verbose telemetry](#logging-levels-and-verbose-telemetry)
  - [Key log messages to watch for](#key-log-messages-to-watch-for)
  - [Structured EventId reference](#structured-eventid-reference)
  - [Application Insights setup](#application-insights-setup)
  - [KQL queries for worker service activity](#kql-queries-for-worker-service-activity)
- [Troubleshooting](#troubleshooting)
- [Security Notes](#security-notes)
- [Related Components](#related-components)

---

## Why This Service Exists

WHMCS allows you to restrict which IP addresses may call its API. Azure Functions run on shared, dynamic infrastructure whose outbound IP addresses can change at any time — making it impossible to maintain a stable allowlist.

This service is deployed on a **Linux VM that has a static public IP address**. All WHMCS API calls originate from that single, known IP, so:

- WHMCS can allowlist exactly one IP address.
- The Azure Function (`DomainRegistrationTriggerFunction`) never contacts WHMCS directly; instead it enqueues a message to an Azure Service Bus queue.
- This service dequeues messages and makes the WHMCS API calls from the static IP.

```
Azure Function (dynamic IP)
  │  enqueues message
  ▼
Azure Service Bus Queue
  │  dequeues message
  ▼
WhmcsWorkerService on VM (static IP)
  │  calls WHMCS REST API
  ▼
WHMCS → Domain Registrar
```

---

## Architecture Overview

| Component | Location | Purpose |
|-----------|----------|---------|
| `DomainRegistrationTriggerFunction` | Azure Functions (InkStainedWretchFunctions) | Detects new domain registrations in Cosmos DB, enqueues message to Service Bus |
| Azure Service Bus queue | Azure cloud | Durable message buffer between the function and this worker |
| `WhmcsWorkerService` | Linux VM with static IP | Dequeues messages, calls WHMCS API to register domain and set name servers |
| WHMCS | Your WHMCS installation | Manages the domain registration through a registrar module |

**Message processing flow**:

1. Worker receives a `WhmcsDomainRegistrationMessage` (JSON) from the queue.
2. Calls `IWhmcsService.RegisterDomainAsync()` — WHMCS `DomainRegister` API action.
3. If registration succeeds and name servers (2–5) were provided, calls `IWhmcsService.UpdateNameServersAsync()` — WHMCS `DomainUpdateNameservers` API action.
4. On success → message is **completed** (removed from queue).
5. On transient error (API unreachable, WHMCS returned false) → message is **abandoned** (returned to queue for retry up to the queue's max delivery count).
6. On permanent error (malformed JSON, missing domain data) → message is **dead-lettered**.

---

## Prerequisites

| Item | Notes |
|------|-------|
| Azure subscription | Required for VM and Service Bus |
| Azure CLI or Azure Portal access | For resource provisioning |
| .NET 10 SDK | For building the service |
| SSH client | For deploying to the VM |
| WHMCS installation | With admin access to create API credentials |
| WHMCS registrar module | Must be active and funded |

---

## Full Deployment Guide

### 1. Provision an Azure VM with a static outbound IP

The VM requires a **static public IP address** so WHMCS can allowlist it permanently.

#### Option A — Azure Portal

1. Go to **Virtual Machines** → **Create** → **Azure virtual machine**.
2. Choose a small SKU (e.g., `Standard_B1s`) running **Ubuntu 24.04 LTS**.
3. Under **Networking** → **Public IP** click **Create new**:
   - Set **Assignment** to **Static**.
   - Note the name you give it (e.g., `whmcs-worker-pip`).
4. Allow inbound SSH (port 22) from your management IP only.
5. Complete the wizard and create the VM.

#### Option B — Azure CLI

```bash
RESOURCE_GROUP="whmcs-worker-rg"
LOCATION="eastus"
VM_NAME="whmcs-worker-vm"
ADMIN_USER="azureuser"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create static public IP
az network public-ip create \
  --resource-group $RESOURCE_GROUP \
  --name whmcs-worker-pip \
  --allocation-method Static \
  --sku Standard

# Create VM referencing the static IP
az vm create \
  --resource-group $RESOURCE_GROUP \
  --name $VM_NAME \
  --image Ubuntu2404 \
  --size Standard_B1s \
  --admin-username $ADMIN_USER \
  --ssh-key-values ~/.ssh/id_rsa.pub \
  --public-ip-address whmcs-worker-pip \
  --public-ip-sku Standard
```

---

### 2. Find and record the static IP address

You need this IP to configure the WHMCS API allowlist.

#### From the Azure Portal

1. Open **Virtual Machines** → select your VM.
2. In the **Overview** panel, read **Public IP address** — this is the static outbound IP.
3. Alternatively: **Networking** → **Network Interface** → **IP configurations** → `ipconfig1` → **Public IP address**.

#### From the Azure CLI

```bash
az vm show \
  --resource-group whmcs-worker-rg \
  --name whmcs-worker-vm \
  --show-details \
  --query publicIps \
  --output tsv
```

#### Directly from the VM (confirmation)

After SSH-ing into the VM, confirm the IP is correct:

```bash
# Any of these will print the VM's public outbound IP
curl -s https://checkip.amazonaws.com
curl -s https://ifconfig.me
curl -s https://api.ipify.org
```

> **Important**: Record this IP address. You will enter it in WHMCS in [step 9](#9-configure-whmcs-api-credentials-with-ip-allowlist).

---

### 3. Create the Azure Service Bus queue

#### Option A — Azure Portal

1. Create (or open) a **Service Bus namespace** (Standard tier or higher).
2. Under **Queues** → **+ Queue**:
   - **Name**: `whmcs-domain-registrations` (or your preferred name — must match `SERVICE_BUS_WHMCS_QUEUE_NAME`).
   - **Max delivery count**: `10` (messages are abandoned to trigger retries; after 10 attempts they are dead-lettered).
   - Leave other settings at their defaults.
3. Copy the **Primary Connection String** from **Shared access policies** → `RootManageSharedAccessKey` (or create a least-privilege policy with `Send` + `Listen` claims).

#### Option B — Azure CLI

```bash
SB_NAMESPACE="whmcs-worker-sb"
RESOURCE_GROUP="whmcs-worker-rg"
LOCATION="eastus"

az servicebus namespace create \
  --resource-group $RESOURCE_GROUP \
  --name $SB_NAMESPACE \
  --location $LOCATION \
  --sku Standard

az servicebus queue create \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --name whmcs-domain-registrations \
  --max-delivery-count 10

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group $RESOURCE_GROUP \
  --namespace-name $SB_NAMESPACE \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv
```

---

### 4. Prepare the VM

SSH into the VM and install the .NET 10 runtime:

```bash
ssh azureuser@<vm-public-ip>

# Install .NET 10 runtime (no SDK needed on the VM)
# Ubuntu 24.04
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0

# Create service user (no login shell, no home directory)
sudo useradd --system --no-create-home --shell /usr/sbin/nologin whmcsworker

# Create directories
sudo mkdir -p /opt/whmcs-worker
sudo mkdir -p /etc/whmcs-worker
sudo mkdir -p /var/log/whmcs-worker

# Ownership
sudo chown -R whmcsworker:whmcsworker /opt/whmcs-worker
sudo chown -R whmcsworker:whmcsworker /var/log/whmcs-worker
sudo chown root:root /etc/whmcs-worker
sudo chmod 750 /etc/whmcs-worker
```

---

### 5. Build and publish the service

Run on your **development machine** (not the VM):

```bash
# From repository root
dotnet publish WhmcsWorkerService/WhmcsWorkerService.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o /tmp/whmcs-worker-publish
```

> Use `--self-contained true` if you prefer a single deployable folder that does not require .NET on the VM.

---

### 6. Deploy to the VM

```bash
# Copy published files to the VM
scp -r /tmp/whmcs-worker-publish/* azureuser@<vm-public-ip>:/tmp/whmcs-worker-stage/

# SSH in and move files into place
ssh azureuser@<vm-public-ip>
sudo cp -r /tmp/whmcs-worker-stage/* /opt/whmcs-worker/
sudo chown -R whmcsworker:whmcsworker /opt/whmcs-worker/
rm -rf /tmp/whmcs-worker-stage
```

---

### 7. Configure environment variables

Create the environment file that the systemd service loads at startup. The file must be readable by root only (secrets are stored here).

```bash
sudo tee /etc/whmcs-worker/environment > /dev/null <<'EOF'
# Azure Service Bus
SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key
SERVICE_BUS_WHMCS_QUEUE_NAME=whmcs-domain-registrations

# WHMCS API
WHMCS_API_URL=https://your-whmcs-instance.com/includes/api.php
WHMCS_API_IDENTIFIER=your-api-identifier
WHMCS_API_SECRET=your-api-secret

# systemd watchdog defaults (used ONLY if systemd-provided NOTIFY_SOCKET/WATCHDOG_* are missing or blank)
# These values are safe to include in /etc/whmcs-worker/environment because they do NOT override systemd's vars.
WHMCS_SYSTEMD_NOTIFY_SOCKET=/run/systemd/notify
WHMCS_SYSTEMD_WATCHDOG_USEC=30000000
EOF

# Lock down permissions
sudo chmod 600 /etc/whmcs-worker/environment
sudo chown root:root /etc/whmcs-worker/environment
```

**Never commit this file to source control.**

---

### 8. Install and start the systemd service

The `whmcs-worker.service` unit file is already in the repository at `WhmcsWorkerService/whmcs-worker.service`.

```bash
# Copy the unit file
sudo cp /opt/whmcs-worker/whmcs-worker.service /etc/systemd/system/whmcs-worker.service

# Reload systemd, enable on boot, start now
sudo systemctl daemon-reload
sudo systemctl enable whmcs-worker
sudo systemctl start whmcs-worker

# Check status
sudo systemctl status whmcs-worker
```

Expected output:

```
● whmcs-worker.service - WHMCS Domain Registration Worker Service
     Loaded: loaded (/etc/systemd/system/whmcs-worker.service; enabled; preset: enabled)
     Active: active (running) since ...
```

View live logs:

```bash
sudo journalctl -u whmcs-worker -f
```

---

### 9. Configure WHMCS API credentials with IP allowlist

1. Log in to your **WHMCS Admin Panel**.
2. Navigate to **Setup** → **Staff Management** → **API Credentials**.
3. Click **Generate New API Credential** (or edit an existing one).
4. Fill in the details:
   - **Admin User**: select the admin account used for API operations.
   - **IP Restriction(s)**: enter the VM's static public IP address (from [step 2](#2-find-and-record-the-static-ip-address)). Adding the IP here restricts WHMCS so it only accepts API calls from your VM.
   - **API Roles / Permissions**: at minimum, enable `Domain: Register Domain` and `Domain: Update Nameservers`. Enable `Billing: Get TLD Pricing` if you use the `GetTLDPricing` endpoint.
5. Click **Save** and copy:
   - **Identifier** → `WHMCS_API_IDENTIFIER`
   - **Secret** (shown once) → `WHMCS_API_SECRET`
6. Update `/etc/whmcs-worker/environment` on the VM with these values and restart the service:

   ```bash
   sudo systemctl restart whmcs-worker
   ```

> **Tip**: After saving the credential, use the `WhmcsTestHarness` console app (see `WhmcsTestHarness/README.md`) from the VM to confirm the credentials work.

---

### 10. Verify end-to-end

1. Create a test domain registration record in Cosmos DB with `status = Pending`.
2. Watch the `DomainRegistrationTriggerFunction` logs in Application Insights — it should enqueue a message.
3. Watch the worker logs on the VM:

   ```bash
   sudo journalctl -u whmcs-worker -f
   ```

   You should see:

   ```
   Processing WHMCS registration for domain example.com (message ID ...)
   Successfully registered domain example.com via WHMCS API
   Successfully updated name servers for domain example.com in WHMCS
   Completed processing WHMCS registration for domain example.com
   ```

4. Check the WHMCS **Activity Log** (**Utilities** → **Logs** → **Activity Log**) for the registration event.

---

## Configuration Reference

All configuration is loaded from `/etc/whmcs-worker/environment` (and optionally from .NET User Secrets during development).

### Core variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SERVICE_BUS_CONNECTION_STRING` | **Yes** | — | Full Azure Service Bus connection string |
| `SERVICE_BUS_WHMCS_QUEUE_NAME` | No | `whmcs-domain-registrations` | Name of the Service Bus queue |
| `WHMCS_API_URL` | **Yes** | — | Full URL to the WHMCS API (e.g., `https://your-whmcs.com/includes/api.php`) |
| `WHMCS_API_IDENTIFIER` | **Yes** | — | WHMCS API credential identifier |
| `WHMCS_API_SECRET` | **Yes** | — | WHMCS API credential secret |

### Telemetry and logging variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | No | — | Application Insights connection string. When set, all structured logs are exported to Azure Monitor for KQL querying. |
| `WHMCS_WORKER_LOG_LEVEL` | No | `Information` | Minimum log level feature flag. Accepted values: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`. Set to `Debug` or `Trace` to enable verbose telemetry. |

The worker logs the configured/missing status of each variable at startup (values are never logged).

---

## Routine Maintenance

### Deploying updates

```bash
# On development machine: build and publish new version
dotnet publish WhmcsWorkerService/WhmcsWorkerService.csproj \
  -c Release -r linux-x64 --self-contained false \
  -o /tmp/whmcs-worker-publish

# Copy to VM
scp -r /tmp/whmcs-worker-publish/* azureuser@<vm-public-ip>:/tmp/whmcs-worker-stage/

# On VM: stop service, replace files, restart
ssh azureuser@<vm-public-ip>
sudo systemctl stop whmcs-worker
sudo cp -r /tmp/whmcs-worker-stage/* /opt/whmcs-worker/
sudo chown -R whmcsworker:whmcsworker /opt/whmcs-worker/
sudo systemctl start whmcs-worker
sudo systemctl status whmcs-worker
```

### Rotating WHMCS API credentials

1. In WHMCS Admin, go to **Setup** → **Staff Management** → **API Credentials**.
2. Edit the credential → regenerate the secret.
3. Copy the new secret.
4. Update `/etc/whmcs-worker/environment` on the VM:

   ```bash
   sudo nano /etc/whmcs-worker/environment
   # Update WHMCS_API_SECRET
   sudo systemctl restart whmcs-worker
   ```

5. Verify the worker restarts cleanly (`systemctl status whmcs-worker`).

### Rotating Service Bus connection strings

1. In the Azure Portal, navigate to your Service Bus namespace → **Shared access policies** → regenerate a key.
2. Update `SERVICE_BUS_CONNECTION_STRING` in `/etc/whmcs-worker/environment`.
3. Restart the service:

   ```bash
   sudo systemctl restart whmcs-worker
   ```

### VM OS patching

Apply security patches monthly (or via Azure Automatic VM Guest Patching):

```bash
sudo apt-get update && sudo apt-get upgrade -y
sudo reboot   # if kernel was updated
```

After reboot, the service starts automatically (it is enabled in systemd).

### .NET runtime updates

When a new .NET patch version is released:

```bash
sudo apt-get update
sudo apt-get install --only-upgrade aspnetcore-runtime-10.0
sudo systemctl restart whmcs-worker
```

### Dead-letter queue monitoring

Messages that fail repeatedly (invalid JSON, missing data, or exceeded max-delivery-count) land in the dead-letter sub-queue. Check it periodically:

```bash
# Azure CLI — count dead-lettered messages
az servicebus queue show \
  --resource-group whmcs-worker-rg \
  --namespace-name whmcs-worker-sb \
  --name whmcs-domain-registrations \
  --query "countDetails.deadLetterMessageCount"
```

To reprocess or inspect dead-lettered messages, use **Azure Service Bus Explorer** in the Azure Portal (navigate to the queue → **Service Bus Explorer** → select the **Dead-letter** sub-queue).

### Verifying the VM's IP has not changed

Static public IP addresses do not change unless you explicitly delete and recreate the IP resource. Verify at any time:

```bash
# From the VM
curl -s https://checkip.amazonaws.com

# From Azure CLI (off-VM)
az network public-ip show \
  --resource-group whmcs-worker-rg \
  --name whmcs-worker-pip \
  --query ipAddress \
  --output tsv
```

If you ever need to resize or move the VM, reassociate the same public IP resource to the new VM/NIC to keep the IP unchanged.

---

## Monitoring and Observability

### systemd journal (real-time)

```bash
# Follow live
sudo journalctl -u whmcs-worker -f

# Last 100 lines
sudo journalctl -u whmcs-worker -n 100

# Filter to errors only
sudo journalctl -u whmcs-worker -p err

# Since a specific time
sudo journalctl -u whmcs-worker --since "2024-01-01 00:00:00"
```

### Logging levels and verbose telemetry

The worker uses structured logging with five active log levels:

| Level | When emitted | Set `WHMCS_WORKER_LOG_LEVEL` to |
|-------|--------------|---------------------------------|
| **Critical** | Service cannot start (missing config) | — |
| **Error** | Message dead-lettered; exceptions thrown by WHMCS API or Service Bus | `Error` |
| **Warning** | WHMCS returned a non-success result; incorrect name server count; NS update returned false | `Warning` |
| **Information** | Normal operation events (service start/stop, message processing, registration success, NS update result) | `Information` *(default)* |
| **Debug** | Detailed metadata — message body size, enqueue time, processing duration (ms), WHMCS API call durations, result flags | `Debug` |
| **Trace** | Maximum verbosity — message body deserialization details, full name server lists, step-by-step flow | `Trace` |

Setting `WHMCS_WORKER_LOG_LEVEL=Debug` (or `Trace`) in `/etc/whmcs-worker/environment` enables verbose telemetry without redeploying:

```bash
# Edit the environment file on the VM
sudo nano /etc/whmcs-worker/environment

# Add or update:
WHMCS_WORKER_LOG_LEVEL=Debug

# Restart to apply
sudo systemctl restart whmcs-worker
sudo journalctl -u whmcs-worker -f
```

### Key log messages to watch for

| Log message | Level | Meaning |
|-------------|-------|---------|
| `WHMCS Worker Service starting. Listening on queue '{Queue}'` | Info | Service started successfully |
| `WHMCS Worker Service is running and listening for messages` | Info | Processor started; ready for messages |
| `Processing WHMCS registration for domain {Domain}` | Info | A message was dequeued |
| `Successfully registered domain {Domain} via WHMCS API` | Info | Registration call succeeded |
| `Successfully updated name servers for domain {Domain}` | Info | NS update succeeded |
| `WHMCS domain registration returned false for domain {Domain}. Message will be abandoned for retry.` | Warn | WHMCS returned a non-success result; message will retry |
| `Exception while registering domain {Domain} via WHMCS API. Message will be abandoned for retry.` | Error | Network/HTTP error; message will retry |
| `Failed to deserialize WHMCS queue message {MessageId}. Dead-lettering.` | Error | Bad message format — inspect in dead-letter queue |
| `WHMCS queue message {MessageId} has no domain registration data. Dead-lettering.` | Error | Message missing domain — inspect in dead-letter queue |
| `SERVICE_BUS_CONNECTION_STRING is not configured` | Critical | Worker cannot start — check environment file |
| `WHMCS Worker Service is stopping` | Info | Graceful shutdown initiated |

### Structured EventId reference

Every log entry emits a numeric `EventId` and an `EventId.Name`. Use these in KQL queries (see below) to filter by specific events without relying on message text matching.

| EventId | Name | Level | Description |
|---------|------|-------|-------------|
| 1001 | `WhmcsWorkerStarting` | Info/Debug | Service started; queue name and processor options logged |
| 1002 | `WhmcsWorkerRunning` | Info | Processor active; ready to receive messages |
| 1003 | `WhmcsWorkerStopping` | Info | Graceful shutdown begun |
| 2001 | `MessageReceived` | Debug | Service Bus message received (body size, delivery count, sequence number) |
| 2002 | `MessageDeserializeFailed` | Error | JSON deserialization failed; message dead-lettered |
| 2003 | `MessageMissingData` | Error | DomainRegistration or Domain is null; message dead-lettered |
| 2010 | `ProcessingStarted` | Info/Debug/Trace | Domain registration processing started; metadata logged at Debug |
| 2011 | `RegistrationStarted` | Debug | WHMCS `RegisterDomainAsync` call initiated; duration logged on completion |
| 2012 | `RegistrationSucceeded` | Info | WHMCS domain registration returned success |
| 2013 | `RegistrationFailed` | Warning | WHMCS returned false; message will be retried |
| 2014 | `RegistrationException` | Error | Exception thrown during registration; message will be retried |
| 2021 | `NameServerUpdateStarted` | Info/Trace | NS update initiated; full NS list logged at Trace |
| 2022 | `NameServerUpdateSucceeded` | Info | WHMCS NS update returned success |
| 2023 | `NameServerUpdateFailed` | Warning | WHMCS returned false for NS update; message completed anyway |
| 2024 | `NameServerUpdateException` | Error | Exception during NS update; message completed anyway |
| 2025 | `NameServerUpdateSkipped` | Info/Warning | No NS provided (Info) or invalid NS count (Warning) |
| 2030 | `ProcessingCompleted` | Info/Debug | Processing finished; total elapsed ms logged at Debug |
| 3001 | `ServiceBusError` | Error | Service Bus processor error (connection issues, etc.) |

### Application Insights setup

Set `APPLICATIONINSIGHTS_CONNECTION_STRING` in `/etc/whmcs-worker/environment` to export all structured logs to Azure Monitor. The connection string is available in the Azure Portal under your Application Insights resource → **Overview** → **Connection String**.

```bash
# Add to /etc/whmcs-worker/environment
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...;IngestionEndpoint=...
```

Once configured, logs appear in the `traces` table in Log Analytics with:

- `cloud_RoleName` = `unknown_service:WhmcsWorkerService`
- `customDimensions["EventId"]` = numeric EventId (e.g. `2012`)
- `customDimensions["EventId.Name"]` = EventId name (e.g. `RegistrationSucceeded`)
- `customDimensions["Domain"]` = domain name (e.g. `example.com`)
- `customDimensions["MessageId"]` = Service Bus message ID
- `customDimensions["TotalElapsedMs"]` = total processing duration (Debug level only)
- `customDimensions["RegistrationElapsedMs"]` = WHMCS registration API duration (Debug level only)
- `customDimensions["NsElapsedMs"]` = WHMCS NS update API duration (Debug level only)

### KQL queries for worker service activity

All queries use `cloud_RoleName == "unknown_service:WhmcsWorkerService"` to scope results to this service. These queries are also available as `.kql` files in the [`/kql`](../kql/) directory.

#### Service health

```kql
// Worker service lifecycle events (starts, stops, Service Bus errors)
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(24h)
| where customDimensions["EventId.Name"] in (
    "WhmcsWorkerStarting",
    "WhmcsWorkerRunning",
    "WhmcsWorkerStopping",
    "ServiceBusError"
)
| extend
    EventName = tostring(customDimensions["EventId.Name"]),
    Source    = tostring(customDimensions["Source"]),
    Entity    = tostring(customDimensions["Entity"])
| project timestamp, EventName, Source, Entity, severityLevel, message
| order by timestamp desc
```

#### All errors and warnings

```kql
// All warning, error, and critical events
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(24h)
| where severityLevel >= 2  // 2=Warning, 3=Error, 4=Critical
| extend
    EventName = tostring(customDimensions["EventId.Name"]),
    Domain    = tostring(customDimensions["Domain"]),
    MessageId = tostring(customDimensions["MessageId"])
| project timestamp, severityLevel, EventName, Domain, MessageId, message
| order by timestamp desc
```

#### Message processing lifecycle

```kql
// Full lifecycle of each domain registration message
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(24h)
| where customDimensions["EventId.Name"] in (
    "ProcessingStarted",
    "ProcessingCompleted",
    "RegistrationSucceeded",
    "RegistrationFailed",
    "RegistrationException",
    "MessageDeserializeFailed",
    "MessageMissingData"
)
| extend
    EventName = tostring(customDimensions["EventId.Name"]),
    Domain    = tostring(customDimensions["Domain"]),
    MessageId = tostring(customDimensions["MessageId"])
| project timestamp, EventName, Domain, MessageId, severityLevel, message
| order by timestamp desc
```

#### Registration success rate over time

```kql
// Hourly registration success/failure/exception counts with success rate
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(7d)
| where customDimensions["EventId.Name"] in (
    "RegistrationSucceeded",
    "RegistrationFailed",
    "RegistrationException"
)
| extend EventName = tostring(customDimensions["EventId.Name"])
| summarize
    Succeeded     = countif(EventName == "RegistrationSucceeded"),
    Failed        = countif(EventName == "RegistrationFailed"),
    Exceptions    = countif(EventName == "RegistrationException"),
    TotalAttempts = count()
    by bin(timestamp, 1h)
| extend SuccessRate = round(todouble(Succeeded) / todouble(TotalAttempts) * 100, 1)
| order by timestamp desc
| render timechart
```

#### Dead-letter queue analysis

```kql
// All dead-lettered messages (bad JSON or missing domain data)
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(7d)
| where customDimensions["EventId.Name"] in (
    "MessageDeserializeFailed",
    "MessageMissingData"
)
| extend
    EventName = tostring(customDimensions["EventId.Name"]),
    MessageId = tostring(customDimensions["MessageId"]),
    Reason = case(
        customDimensions["EventId.Name"] == "MessageDeserializeFailed", "DeserializationFailure",
        customDimensions["EventId.Name"] == "MessageMissingData",       "MissingDomainData",
        "Unknown"
    )
| project timestamp, Reason, MessageId, message
| order by timestamp desc
```

#### Processing performance (requires `WHMCS_WORKER_LOG_LEVEL=Debug`)

```kql
// Hourly P95, max, avg processing durations
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(24h)
| where customDimensions["EventId.Name"] == "ProcessingCompleted"
| extend
    Domain         = tostring(customDimensions["Domain"]),
    TotalElapsedMs = tolong(customDimensions["TotalElapsedMs"])
| where isnotnull(TotalElapsedMs)
| summarize
    AvgMs  = avg(TotalElapsedMs),
    MaxMs  = max(TotalElapsedMs),
    P95Ms  = percentile(TotalElapsedMs, 95),
    Count  = count()
    by bin(timestamp, 1h)
| order by timestamp desc
| render timechart
```

#### Name server update outcomes

```kql
// Name server update success/failure/skip counts over time
traces
| where cloud_RoleName == "unknown_service:WhmcsWorkerService"
| where timestamp > ago(7d)
| where customDimensions["EventId.Name"] in (
    "NameServerUpdateSucceeded",
    "NameServerUpdateFailed",
    "NameServerUpdateException",
    "NameServerUpdateSkipped"
)
| extend EventName = tostring(customDimensions["EventId.Name"])
| summarize
    Succeeded  = countif(EventName == "NameServerUpdateSucceeded"),
    Failed     = countif(EventName == "NameServerUpdateFailed"),
    Exceptions = countif(EventName == "NameServerUpdateException"),
    Skipped    = countif(EventName == "NameServerUpdateSkipped"),
    TotalOps   = count()
    by bin(timestamp, 1h)
| order by timestamp desc
| render timechart
```

#### Cross-service correlation (Azure Functions + Worker Service)

```kql
// Correlate enqueue events (Function side) with processing events (Worker side)
// Useful for measuring end-to-end latency from enqueue to completion
let enqueuedMessages =
    traces
    | where cloud_RoleName != "unknown_service:WhmcsWorkerService"
    | where message contains "Enqueued WHMCS"
    | extend Domain = extract(@"domain ([^\s,]+)", 1, message)
    | project EnqueuedAt = timestamp, Domain;
let processedMessages =
    traces
    | where cloud_RoleName == "unknown_service:WhmcsWorkerService"
    | where customDimensions["EventId.Name"] == "ProcessingCompleted"
    | extend Domain = tostring(customDimensions["Domain"])
    | project ProcessedAt = timestamp, Domain;
enqueuedMessages
| join kind=leftouter processedMessages on Domain
| extend LatencySeconds = datetime_diff("second", ProcessedAt, EnqueuedAt)
| project EnqueuedAt, ProcessedAt, Domain, LatencySeconds
| order by EnqueuedAt desc
```

```kql
// Messages enqueued by DomainRegistrationTriggerFunction (Azure Functions side)
traces
| where message contains "Enqueued WHMCS"
| project timestamp, message, severityLevel, cloud_RoleName
| order by timestamp desc
```

```kql
// Domain registration trigger errors (Azure Functions side)
traces
| where operation_Name == "DomainRegistrationTrigger"
| where severityLevel >= 3
| project timestamp, message, cloud_RoleName
| order by timestamp desc
```

---

## Troubleshooting

### Service fails to start — "SERVICE_BUS_CONNECTION_STRING is not configured"

- Verify `/etc/whmcs-worker/environment` exists and contains `SERVICE_BUS_CONNECTION_STRING`.
- Check permissions: `sudo ls -la /etc/whmcs-worker/environment` → should be `-rw------- root root`.
- Reload and restart:

  ```bash
  sudo systemctl daemon-reload
  sudo systemctl restart whmcs-worker
  ```

### Service fails to start — "No such file or directory" for WhmcsWorkerService.dll

- The published output was not copied to `/opt/whmcs-worker/`.
- Run [step 6](#6-deploy-to-the-vm) again.

### Messages are abandoned repeatedly and not completing

1. **Check WHMCS is reachable from the VM**:

   ```bash
   curl -v https://your-whmcs-instance.com/includes/api.php
   ```

2. **Check IP allowlist in WHMCS**: the VM's IP must match the IP restriction on the API credential.

3. **Check WHMCS credentials**: use the `WhmcsTestHarness` from the VM to send a test API call.

4. **Check WHMCS Activity Log** (WHMCS Admin → Utilities → Logs → Activity Log) for rejected or errored calls.

5. If the WHMCS registrar module has insufficient balance, domain registration will fail. Top up the account.

### Messages are dead-lettered with "DeserializationFailure"

The message JSON does not match the expected `WhmcsDomainRegistrationMessage` schema. This usually means a different version of `DomainRegistrationTriggerFunction` was deployed. Ensure the function app and worker service are built from the same version of `OnePageAuthorLib`.

### "401 Unauthorized" from WHMCS API

- The API identifier or secret is wrong.
- The API credential is disabled in WHMCS.
- The IP restriction in WHMCS does not include the VM's IP.

### Worker VM's public IP appears to have changed

Static public IP addresses assigned to a VM NIC do not change. However:
- If the VM was deallocated and the IP was not attached to the NIC, it may have been released.
- Reassociate the IP resource, or update the WHMCS IP allowlist with the new IP.

---

## Security Notes

- `/etc/whmcs-worker/environment` contains secrets — keep permissions as `600 root:root`.
- The `whmcsworker` system user has no login shell and no home directory — it cannot be used to log in interactively.
- The systemd unit applies hardening flags: `NoNewPrivileges`, `PrivateTmp`, `ProtectSystem=strict`, `ProtectHome`.
- All WHMCS API calls use HTTPS; never disable TLS certificate validation.
- Never commit credentials to source control; use environment files or Azure Key Vault.
- Rotate WHMCS API credentials and Service Bus keys at least annually.

---

## Related Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `IWhmcsQueueService` / `WhmcsQueueService` | `OnePageAuthorLib/interfaces/`, `OnePageAuthorLib/api/` | Sends domain registration messages to Service Bus |
| `DomainRegistrationTriggerFunction` | `InkStainedWretchFunctions/DomainRegistrationTriggerFunction.cs` | Triggers the queue on new Cosmos DB domain records |
| `IWhmcsService` / `WhmcsService` | `OnePageAuthorLib/interfaces/`, `OnePageAuthorLib/api/` | Makes WHMCS REST API calls |
| `WhmcsTestHarness` | `WhmcsTestHarness/` | Console app for manual testing of WHMCS API calls |
| `docs/WHMCS_INTEGRATION_SUMMARY.md` | `docs/` | Full integration architecture summary |

---

## References

- [WHMCS API — DomainRegister](https://developers.whmcs.com/api-reference/domainregister/)
- [WHMCS API — DomainUpdateNameservers](https://developers.whmcs.com/api-reference/domainupdatenameservers/)
- [WHMCS API Credentials](https://docs.whmcs.com/API_Authentication_Credentials)
- [Azure Service Bus queues](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-queues-topics-subscriptions)
- [Static public IP addresses in Azure](https://learn.microsoft.com/en-us/azure/virtual-network/ip-services/public-ip-addresses#allocation-method)
- [.NET Worker Service with systemd](https://learn.microsoft.com/en-us/dotnet/core/extensions/systemd-service)
