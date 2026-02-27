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

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `SERVICE_BUS_CONNECTION_STRING` | **Yes** | — | Full Azure Service Bus connection string |
| `SERVICE_BUS_WHMCS_QUEUE_NAME` | No | `whmcs-domain-registrations` | Name of the Service Bus queue |
| `WHMCS_API_URL` | **Yes** | — | Full URL to the WHMCS API (e.g., `https://your-whmcs.com/includes/api.php`) |
| `WHMCS_API_IDENTIFIER` | **Yes** | — | WHMCS API credential identifier |
| `WHMCS_API_SECRET` | **Yes** | — | WHMCS API credential secret |

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
```

### Key log messages to watch for

| Log message | Meaning |
|-------------|---------|
| `WHMCS Worker Service starting. Listening on queue 'whmcs-domain-registrations'` | Service started successfully |
| `Processing WHMCS registration for domain {Domain}` | A message was dequeued |
| `Successfully registered domain {Domain} via WHMCS API` | Registration call succeeded |
| `Successfully updated name servers for domain {Domain}` | Name server update succeeded |
| `WHMCS domain registration returned false for domain {Domain}. Message will be abandoned for retry.` | WHMCS returned a non-success result; message will retry |
| `Exception while registering domain {Domain} via WHMCS API. Message will be abandoned for retry.` | Network/HTTP error; message will retry |
| `Failed to deserialize WHMCS queue message {MessageId}. Dead-lettering.` | Bad message format — inspect in dead-letter queue |
| `SERVICE_BUS_CONNECTION_STRING is not configured` | Worker cannot start — check environment file |

### Application Insights (Azure Functions side)

To correlate with the Azure Functions side, use these KQL queries:

```kql
// Messages enqueued by DomainRegistrationTriggerFunction
traces
| where message contains "Enqueued WHMCS"
| project timestamp, message, severityLevel
| order by timestamp desc

// Domain registration trigger errors
traces
| where operation_Name == "DomainRegistrationTrigger"
| where severityLevel >= 3
| project timestamp, message
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
