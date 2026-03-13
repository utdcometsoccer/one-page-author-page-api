# WHMCS Domain Registration Integration - Implementation Summary

> **Deployment & Operations guide**: See [`WhmcsWorkerService/README.md`](../WhmcsWorkerService/README.md) for the step-by-step guide on deploying the worker, obtaining the static VM IP, configuring the WHMCS allowlist, and ongoing maintenance.

## Overview

This document details the implementation of WHMCS API integration for automated domain registration in the OnePageAuthor API platform. The integration uses a **queue-based proxy architecture**: an Azure Function enqueues domain registration requests to Azure Service Bus, and a dedicated Linux VM worker (`WhmcsWorkerService`) dequeues them and calls WHMCS from a **static outbound IP address** that can be allowlisted in WHMCS.

## Architecture

```
DomainRegistrationTriggerFunction (Azure Function, dynamic IP)
  │  enqueues WhmcsDomainRegistrationMessage
  ▼
Azure Service Bus Queue  (whmcs-domain-registrations)
  │  dequeues message
  ▼
WhmcsWorkerService  (Linux VM, static IP — see WhmcsWorkerService/README.md)
  │  calls WHMCS REST API
  ▼
WHMCS → Domain Registrar
```

### Why a VM proxy?

WHMCS API credentials can be locked to specific IP addresses. Azure Functions run on shared infrastructure with dynamic IPs, making a stable allowlist impossible. The VM worker solves this by providing a single, permanent outbound IP.

## Implementation Date

February 9, 2026 (initial). Queue-based proxy architecture added subsequently.

## Changes Made

### New Components

#### 1. IWhmcsService Interface
**File**: `OnePageAuthorLib/interfaces/IWhmcsService.cs`

- Defines the contract for WHMCS API interactions
- Single method: `RegisterDomainAsync(DomainRegistration domainRegistration)`
- Returns `Task<bool>` indicating success or failure

#### 2. WhmcsService Implementation
**File**: `OnePageAuthorLib/api/WhmcsService.cs`

**Features**:
- Implements WHMCS DomainRegister API integration
- Uses HttpClient for API calls
- Configurable via environment variables:
  - `WHMCS_API_URL` - WHMCS API endpoint
  - `WHMCS_API_IDENTIFIER` - API authentication identifier
  - `WHMCS_API_SECRET` - API authentication secret
- Maps DomainRegistration entity to WHMCS API parameters
- Handles contact information mapping
- Comprehensive error handling and logging
- Returns structured responses with success/failure indicators

**API Request Format**:
```csharp
// Form-encoded request to WHMCS API
{
    "action": "DomainRegister",
    "identifier": "{api-identifier}",
    "secret": "{api-secret}",
    "domain": "example.com",
    "responsetype": "json",
    // Contact information fields
    "firstname": "John",
    "lastname": "Doe",
    "email": "john@example.com",
    "address1": "123 Main St",
    "city": "City",
    "state": "State",
    "postcode": "12345",
    "country": "Country",
    "phonenumber": "+1-555-1234"
}
```

**Response Handling**:
- Parses JSON response from WHMCS
- Checks `result` field for "success" status
- Logs detailed error messages on failure
- Gracefully handles HTTP errors and JSON parsing errors

#### 3. WhmcsWorkerService (Queue Worker / Proxy)
**File**: `WhmcsWorkerService/Worker.cs`, `WhmcsWorkerService/Program.cs`, `WhmcsWorkerService/whmcs-worker.service`

The worker is a .NET 10 background service (`BackgroundService`) that runs permanently on a Linux VM with a static public IP:

- Listens to the Azure Service Bus queue (`whmcs-domain-registrations`, one message at a time).
- Deserializes each `WhmcsDomainRegistrationMessage` and calls `IWhmcsService.RegisterDomainAsync()`.
- If 2–5 name servers are included, calls `IWhmcsService.UpdateNameServersAsync()`.
- **Completes** the message on success, **abandons** it on transient failure (so Service Bus retries), and **dead-letters** it on permanent failure (bad JSON, missing data).
- Integrates with systemd via `UseSystemd()` for reliable service management and journal logging.

See **[`WhmcsWorkerService/README.md`](../WhmcsWorkerService/README.md)** for the complete deployment guide.

#### 4. IWhmcsQueueService / WhmcsQueueService
**File**: `OnePageAuthorLib/interfaces/IWhmcsQueueService.cs`, `OnePageAuthorLib/api/WhmcsQueueService.cs`

Sends domain registration messages to the Service Bus queue from the Azure Function:
- Serializes the `DomainRegistration` entity and name servers into a `WhmcsDomainRegistrationMessage`.
- Sets `MessageId`, `Subject`, `ContentType`, and `EnqueuedAt`.
- Requires `SERVICE_BUS_CONNECTION_STRING` and `SERVICE_BUS_WHMCS_QUEUE_NAME`.

### Modified Components

#### 1. DomainRegistrationTriggerFunction
**File**: `InkStainedWretchFunctions/DomainRegistrationTriggerFunction.cs`

**Changes**:
- Replaced direct `IWhmcsService` call with `IWhmcsQueueService.EnqueueDomainRegistrationAsync()`.
- WHMCS API calls now happen on the VM worker, not inside the Azure Function.

**New Process Flow**:
1. Validate domain registration data (status must be `Pending`).
2. Ensure Azure DNS zone exists; retrieve name servers.
3. **NEW**: Enqueue a `WhmcsDomainRegistrationMessage` to Service Bus (domain + name servers).
4. Add domain to Azure Front Door (continues even if enqueue fails).
5. Log overall completion.

**Graceful Degradation**:
- If the enqueue step fails, the function logs an error but continues to Front Door setup.
- This ensures DNS and CDN infrastructure is provisioned regardless of WHMCS availability.

#### 2. Program.cs
**File**: `InkStainedWretchFunctions/Program.cs`

- Added `.AddWhmcsService()` to service registration chain
- Follows existing service registration patterns
- No breaking changes to existing configuration

### Test Coverage

#### 1. WhmcsServiceTests
**File**: `OnePageAuthor.Test/Services/WhmcsServiceTests.cs`

**Test Coverage** (15 tests):
- Constructor validation (null parameters, missing configuration)
- Configuration validation (missing API URL, identifier, secret)
- Successful domain registration
- Error handling (HTTP errors, JSON parsing errors, network errors)
- Contact information mapping
- Request formatting

**All tests passing**: ✅ 15/15

#### 2. DomainRegistrationTriggerFunctionTests
**File**: `OnePageAuthor.Test/InkStainedWretchFunctions/DomainRegistrationTriggerFunctionTests.cs`

**Updates**:
- Added WHMCS service mock setup
- Updated constructor tests to validate WHMCS service dependency
- Modified existing tests to verify WHMCS calls
- Maintained backward compatibility with Front Door tests

**All tests passing**: ✅ 18/18

### Documentation Updates

#### 1. InkStainedWretchFunctions README
**File**: `InkStainedWretchFunctions/README.md`

**Additions**:
1. **Configuration Section**: Added WHMCS configuration variables with setup instructions
2. **Environment Variables Table**: Added WHMCS_API_URL, WHMCS_API_IDENTIFIER, WHMCS_API_SECRET
3. **WHMCS Integration Details**: New section explaining WHMCS setup, permissions, and requirements
4. **DomainRegistrationTrigger Documentation**: Updated process flow to include WHMCS registration step

## Configuration Requirements

### Required Environment Variables

> For the VM worker configuration, see [`WhmcsWorkerService/README.md` — Configuration Reference](../WhmcsWorkerService/README.md#configuration-reference).

#### Azure Functions (`InkStainedWretchFunctions`)

| Variable | Description | Example |
|----------|-------------|---------|
| `WHMCS_API_URL` | WHMCS API endpoint URL (used by `GetTLDPricingFunction`) | `https://whmcs.example.com/includes/api.php` |
| `WHMCS_API_IDENTIFIER` | API authentication identifier (used by `GetTLDPricingFunction`) | `abc123def456` |
| `WHMCS_API_SECRET` | API authentication secret (used by `GetTLDPricingFunction`) | `secret123456` |
| `SERVICE_BUS_CONNECTION_STRING` | Azure Service Bus connection string | `Endpoint=sb://...` |
| `SERVICE_BUS_WHMCS_QUEUE_NAME` | Service Bus queue name | `whmcs-domain-registrations` |

#### WhmcsWorkerService (VM)

| Variable | Description | Example |
|----------|-------------|---------|
| `SERVICE_BUS_CONNECTION_STRING` | Azure Service Bus connection string | `Endpoint=sb://...` |
| `SERVICE_BUS_WHMCS_QUEUE_NAME` | Service Bus queue name | `whmcs-domain-registrations` |
| `WHMCS_API_URL` | WHMCS API endpoint URL | `https://whmcs.example.com/includes/api.php` |
| `WHMCS_API_IDENTIFIER` | API authentication identifier | `abc123def456` |
| `WHMCS_API_SECRET` | API authentication secret | `secret123456` |

### WHMCS Setup Steps

> Full step-by-step instructions are in [`WhmcsWorkerService/README.md`](../WhmcsWorkerService/README.md#9-configure-whmcs-api-credentials-with-ip-allowlist).

1. **Access WHMCS Admin Panel**
   - Log in with administrator credentials

2. **Create API Credentials**
   - Navigate to: Setup → Staff Management → API Credentials
   - Click "Generate New API Credential"

3. **Configure API Credential**
   - Select admin user for API operations
   - **Add the VM's static IP address to IP Restrictions** — this is the key security control; WHMCS will reject calls from any other IP
   - Enable the required API roles (`Domain: Register Domain`, `Domain: Update Nameservers`)
   - Save and copy identifier/secret

4. **Verify Registrar Module**
   - Ensure at least one domain registrar module is active
   - Verify registrar supports desired TLDs
   - Check registrar account balance

### User Secrets Configuration (Development — Function App)

```bash
cd InkStainedWretchFunctions
dotnet user-secrets set "WHMCS_API_URL" "https://whmcs.example.com/includes/api.php"
dotnet user-secrets set "WHMCS_API_IDENTIFIER" "your-api-identifier"
dotnet user-secrets set "WHMCS_API_SECRET" "your-api-secret"
dotnet user-secrets set "SERVICE_BUS_CONNECTION_STRING" "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=..."
dotnet user-secrets set "SERVICE_BUS_WHMCS_QUEUE_NAME" "whmcs-domain-registrations"
```

### User Secrets Configuration (Development — WhmcsWorkerService)

```bash
cd WhmcsWorkerService
dotnet user-secrets set "SERVICE_BUS_CONNECTION_STRING" "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=..."
dotnet user-secrets set "SERVICE_BUS_WHMCS_QUEUE_NAME" "whmcs-domain-registrations"
dotnet user-secrets set "WHMCS_API_URL" "https://whmcs.example.com/includes/api.php"
dotnet user-secrets set "WHMCS_API_IDENTIFIER" "your-api-identifier"
dotnet user-secrets set "WHMCS_API_SECRET" "your-api-secret"
```

### Azure Configuration (Production — Function App)

```bash
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --settings \
    WHMCS_API_URL="https://whmcs.example.com/includes/api.php" \
    WHMCS_API_IDENTIFIER="your-api-identifier" \
    WHMCS_API_SECRET="your-api-secret" \
    SERVICE_BUS_CONNECTION_STRING="Endpoint=sb://..." \
    SERVICE_BUS_WHMCS_QUEUE_NAME="whmcs-domain-registrations"
```

For the VM worker (production), see [`WhmcsWorkerService/README.md` — Configure environment variables](../WhmcsWorkerService/README.md#7-configure-environment-variables).

## Architecture Decisions

### 1. Graceful Degradation

**Decision**: Continue to Front Door integration even if WHMCS registration fails

**Rationale**:
- Domain might already be registered externally
- Manual registration might be needed for some TLDs
- Infrastructure setup (DNS, Front Door) can proceed independently
- Reduces brittleness and improves resilience

### 2. HttpClient-Based Implementation

**Decision**: Use HttpClient instead of dedicated WHMCS SDK

**Rationale**:
- WHMCS API is REST-based and simple
- Avoids external dependency management
- Direct control over request/response handling
- Easier to test with mocked HttpMessageHandler

### 3. Configuration via Environment Variables

**Decision**: Store WHMCS credentials in environment variables/user secrets

**Rationale**:
- Follows existing patterns in the codebase
- Supports Azure Key Vault integration
- Compatible with CI/CD pipelines
- Secure credential management

### 4. Form-Encoded Requests

**Decision**: Use `FormUrlEncodedContent` for API requests

**Rationale**:
- WHMCS API expects form-encoded data
- Standard format for RESTful APIs
- Simple key-value parameter mapping
- Native .NET support

## Security Considerations

### 1. Credential Management
- **Never commit** API credentials to source control
- Use Azure Key Vault in production
- Use .NET User Secrets for development
- Rotate credentials regularly

### 2. IP Restrictions
- Configure WHMCS API credential IP restrictions to the VM's static public IP
- The VM proxy exists specifically so this allowlist entry is stable
- See [`WhmcsWorkerService/README.md`](../WhmcsWorkerService/README.md) for how to find the IP and configure WHMCS

### 3. HTTPS Only
- All WHMCS API calls use HTTPS
- Certificate validation enabled
- No fallback to HTTP

### 4. Logging
- Mask sensitive values in logs
- Log request/response for debugging
- Do not log API secrets or credentials

## Testing Strategy

### Unit Tests
- **WhmcsService**: Test all API interactions with mocked HttpClient
- **DomainRegistrationTriggerFunction**: Test integration with mocked services
- **Coverage**: Constructor validation, success cases, error handling

### Integration Tests
- Manual testing with WHMCS sandbox/test environment
- Validate API parameter mapping
- Test error scenarios (invalid credentials, network issues)

### End-to-End Tests
- Create domain registration in Cosmos DB
- Verify trigger function execution
- Check WHMCS admin panel for registration
- Validate Front Door domain addition

## Deployment Checklist

> For the complete step-by-step deployment guide see [`WhmcsWorkerService/README.md`](../WhmcsWorkerService/README.md#full-deployment-guide).

### Azure Function App
- [ ] Configure Service Bus connection string and queue name in Azure Key Vault / App Settings
- [ ] Configure WHMCS API credentials in Azure Key Vault (used by `GetTLDPricingFunction`)
- [ ] Verify logging includes enqueue success/failure status
- [ ] Monitor Application Insights for enqueue errors

### Azure Service Bus
- [ ] Create Service Bus namespace (Standard tier or above)
- [ ] Create queue `whmcs-domain-registrations` with max delivery count of 10
- [ ] Create least-privilege shared access policy with `Send` + `Listen` claims

### WhmcsWorkerService VM
- [ ] Provision Linux VM with a **static** public IP address
- [ ] Record the static IP address
- [ ] Install .NET 10 runtime on the VM
- [ ] Create `whmcsworker` system user and `/opt/whmcs-worker` directory
- [ ] Build and publish `WhmcsWorkerService` in Release mode
- [ ] Copy published output to `/opt/whmcs-worker/`
- [ ] Ensure `/etc/whmcs-worker/environment` exists with all required variables (permissions: `600 root:root`)
   - Manual deployment: create it on the VM
   - CI/CD deployment: `infra/vm.bicep` writes/updates it via a VM extension
- [ ] Install and enable `whmcs-worker.service` via systemd
- [ ] Verify service starts cleanly (`systemctl status whmcs-worker`)

### WHMCS
- [ ] Create WHMCS API credential for the worker
- [ ] Add the VM's static IP to the credential's IP restriction list
- [ ] Enable required API roles (Domain Register, Domain Update Nameservers)
- [ ] Verify WHMCS registrar module is configured and active
- [ ] Verify registrar account has sufficient balance
- [ ] Test with `WhmcsTestHarness` from the VM

### Post-deployment Verification
- [ ] Trigger a test domain registration end-to-end
- [ ] Confirm message is dequeued and processed by the worker
- [ ] Confirm domain appears in WHMCS and is registered with the registrar
- [ ] Set up monitoring alerts on dead-letter queue count and worker service health

## Monitoring and Observability

### Key Metrics
- WHMCS API call success rate
- Domain registration latency
- API error rates by type
- Front Door integration success rate

### Log Queries (Application Insights)

```kql
// Find WHMCS registration attempts
traces
| where message contains "WHMCS API"
| project timestamp, message, severityLevel
| order by timestamp desc

// Find WHMCS errors
traces
| where severityLevel == 3 // Error
| where message contains "WHMCS"
| project timestamp, message
| order by timestamp desc

// Domain registration flow
traces
| where operation_Name == "DomainRegistrationTrigger"
| where message contains "example.com"
| project timestamp, message, severityLevel
| order by timestamp asc
```

### Alerts
- Alert on WHMCS API error rate > 10%
- Alert on repeated WHMCS authentication failures
- Alert on domain registration failures

## Known Limitations

1. **Synchronous WHMCS registration**: The worker calls WHMCS synchronously and retries on failure, but there is no automatic status write-back to Cosmos DB when registration completes.
2. **No registration status updates**: The worker cannot update the domain status in Cosmos DB (no authenticated identity in the worker). An admin must use the `POST /api/management/domain-registrations/{registrationId}/complete` endpoint to mark registrations as complete.
3. **Single registrar routing**: WHMCS must route to the appropriate registrar module; there is no per-TLD registrar selection in the worker.
4. **TLD support**: Limited by the WHMCS registrar module configuration.
5. **VM as single point of failure**: If the VM is unavailable, messages queue up in Service Bus (up to the queue's message TTL). The service resumes processing on VM restart.

## Future Enhancements

1. **Async Registration Status Updates**
   - Implement separate function to update registration status
   - Use durable functions for long-running operations

2. **Multi-Registrar Support**
   - Allow configuration of multiple WHMCS instances
   - Route domains to specific registrars based on TLD

3. **Enhanced Error Recovery**
   - Automatic retry with exponential backoff
   - Dead letter queue for failed registrations

4. **Registration Webhooks**
   - Configure WHMCS webhooks for status updates
   - Create webhook receiver function

5. **Cost Tracking**
   - Log domain registration costs
   - Track spending per customer/domain

## References

- WHMCS API Documentation: https://developers.whmcs.com/api-reference/domainregister/
- WHMCS Module Parameters: https://developers.whmcs.com/domain-registrars/module-parameters/
- Azure Functions Isolated Worker: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
- .NET HttpClient: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient

## Contributors

- AI Assistant (@copilot)
- Project Owner (@utdcometsoccer)

## Version History

- **1.0.0** (2026-02-09): Initial implementation with direct WHMCS API calls from Azure Function
- **2.0.0** (2026-02-27): Refactored to queue-based proxy architecture; added `WhmcsWorkerService` VM worker and `IWhmcsQueueService`; `DomainRegistrationTriggerFunction` now enqueues instead of calling WHMCS directly
