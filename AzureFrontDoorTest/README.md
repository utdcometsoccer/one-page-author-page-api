# Azure Front Door Domain Integration Test

A console application for testing Azure Front Door custom domain integration. This test harness validates DNS zone configuration, Front Door domain setup, and TLS certificate management without performing actual infrastructure changes (dry run mode).

## Purpose

This test application is designed to:
1. Verify Azure Front Door service integration
2. Test DNS zone existence and configuration
3. Validate custom domain addition to Front Door profiles
4. Confirm domain naming and format compliance
5. Run on Linux laptops or development environments

## Prerequisites

### Software Requirements
- .NET 10.0 SDK or later
- Azure subscription with Front Door Premium tier
- Azure DNS zone configured
- Linux, macOS, or Windows environment

### Azure Setup

1. **Azure Front Door Profile**
   - Premium or Standard tier
   - Active and running
   - Note the profile name and resource group

2. **Azure DNS**
   - DNS zones for your domains
   - Resource group configured

3. **Azure Authentication**
   - **Option A (Recommended)**: Managed Identity (when running in Azure)
   - **Option B**: Azure CLI authentication
     ```bash
     az login
     ```
   - **Option C**: Service Principal with environment variables
     ```bash
     export AZURE_CLIENT_ID="your-client-id"
     export AZURE_CLIENT_SECRET="your-client-secret"
     export AZURE_TENANT_ID="your-tenant-id"
     ```

### Permissions Required
- **Front Door**: Contributor or higher on Front Door profile
- **DNS**: Contributor on DNS zones
- **Resource Group**: Reader or higher

## Configuration

### Using User Secrets (Recommended for Development)

```bash
cd AzureFrontDoorTest
dotnet user-secrets init
dotnet user-secrets set "AZURE_SUBSCRIPTION_ID" "your-subscription-id"
dotnet user-secrets set "AZURE_RESOURCE_GROUP_NAME" "your-resource-group"
dotnet user-secrets set "AZURE_FRONTDOOR_PROFILE_NAME" "your-frontdoor-profile"
dotnet user-secrets set "AZURE_DNS_RESOURCE_GROUP" "your-dns-resource-group"
```

### Using Environment Variables (Linux)

```bash
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_RESOURCE_GROUP_NAME="your-resource-group"
export AZURE_FRONTDOOR_PROFILE_NAME="your-frontdoor-profile"
export AZURE_DNS_RESOURCE_GROUP="your-dns-resource-group"
```

### Configuration Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `AZURE_SUBSCRIPTION_ID` | Yes | Azure subscription GUID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_RESOURCE_GROUP_NAME` | Yes | Front Door resource group | `myapp-frontdoor-rg` |
| `AZURE_FRONTDOOR_PROFILE_NAME` | Yes | Front Door profile name | `myapp-frontdoor` |
| `AZURE_DNS_RESOURCE_GROUP` | Yes | DNS zones resource group | `myapp-dns-rg` |
| `AZURE_TENANT_ID` | No* | Azure AD tenant ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_CLIENT_ID` | No* | Service principal client ID | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |
| `AZURE_CLIENT_SECRET` | No* | Service principal secret | `your-secret` |

*Required only when using service principal authentication

## Usage

### Build the Application

```bash
cd AzureFrontDoorTest
dotnet build
```

### Authenticate with Azure (if using Azure CLI)

```bash
az login
az account set --subscription "your-subscription-id"
```

### Run with Default Test Data

```bash
dotnet run
```

This uses the default test file: `data/test-domains.json`

### Run with Custom Test Data

```bash
dotnet run -- /path/to/your-test-data.json
```

## Test Data Format

The test data file should contain an array of test domain objects:

```json
[
  {
    "domainName": "example.com",
    "upn": "user@example.com",
    "description": "Test domain for Azure Front Door integration"
  },
  {
    "domainName": "another-domain.org",
    "upn": "user@example.com",
    "description": "Another test domain with org TLD"
  }
]
```

### Field Descriptions

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `domainName` | Yes | string | Fully qualified domain name (e.g., "example.com") |
| `upn` | Yes | string | User Principal Name (email) for ownership tracking |
| `description` | No | string | Human-readable description of test case |

### Domain Naming Rules

Azure Front Door custom domains must follow these rules:
- Valid fully qualified domain name (FQDN)
- No wildcard domains (e.g., *.example.com) in this test
- Total length less than 250 characters
- Valid DNS characters only

## Test Scenarios

### What This Test Validates

1. **DNS Zone Configuration**
   - Checks if DNS zone exists in Azure DNS
   - Reports zone status for each domain
   - Identifies zones that need to be created

2. **Front Door Domain Existence**
   - Queries Front Door profile for existing custom domains
   - Identifies domains already configured
   - Detects naming conflicts

3. **Domain Format Validation**
   - Validates domain name format
   - Checks Azure resource naming compliance
   - Generates sanitized resource names (dots → hyphens)

4. **Integration Readiness**
   - Confirms domain is ready for Front Door integration
   - Reports TLS configuration requirements
   - Validates certificate management settings

### What This Test Does NOT Do

- **Does NOT create DNS zones** (dry run mode)
- **Does NOT add domains to Front Door** (dry run mode)
- **Does NOT configure TLS certificates** (dry run mode)
- **Does NOT modify existing Front Door configuration**
- **Does NOT create DNS records**
- **Does NOT incur Azure charges** (read-only operations)

### Front Door Configuration (Production Mode)

In production, the actual integration would configure:
- **Custom Domain**: Added to Front Door profile
- **TLS Version**: Minimum TLS 1.2
- **Certificate Type**: Azure-managed certificate
- **DNS Validation**: CNAME or TXT record validation
- **HTTPS Redirect**: Enabled (optional)

## Sample Output

```
Azure Front Door Domain Integration Test
========================================

Azure Subscription ID: xxxxxxxx-****-****-****-xxxxxxxxxxxx
Resource Group: myapp-frontdoor-rg
Front Door Profile: myapp-frontdoor

Processing test file: test-domains.json
------------------------------------------------------------
Loaded 4 test domain(s)

Test 1: afdtest001.com
Description: Test domain for Azure Front Door integration - basic setup
------------------------------------------------------------
  Step 1: Checking DNS Zone for afdtest001.com...
  Result: DNS Zone does not exist
  ⚠️  NOTE: DNS Zone would need to be created
  ⚠️  Skipping DNS Zone creation (dry run mode)
  Step 2: Checking Front Door configuration for afdtest001.com...
  Result: Domain does not exist in Front Door
  Step 3: Validating domain format for Azure Front Door...
  Sanitized resource name: afdtest001-com
  ✓ Domain format validated
  Step 4: Verifying readiness for Front Door integration...
  ⚠️  NOTE: Actual Front Door configuration not performed (dry run mode)
  ⚠️  Production would configure:
    - Custom domain: afdtest001.com
    - TLS Version: Minimum TLS 1.2
    - Managed Certificate: Yes
    - DNS Validation: Required
  ✅ TEST PASSED: afdtest001.com is ready for Azure Front Door integration

Test 2: afdtest002.org
Description: Test domain for Azure Front Door integration - org TLD
------------------------------------------------------------
  Step 1: Checking DNS Zone for afdtest002.org...
  Result: DNS Zone exists
  Step 2: Checking Front Door configuration for afdtest002.org...
  Result: Domain already exists in Front Door
  ℹ️  Domain is already configured in Azure Front Door
  ⚠️  SKIPPED: Domain already exists

Test 3: afdtest003.net
Description: Test domain for Azure Front Door integration - different user
------------------------------------------------------------
  Step 1: Checking DNS Zone for afdtest003.net...
  Result: DNS Zone exists
  Step 2: Checking Front Door configuration for afdtest003.net...
  Result: Domain does not exist in Front Door
  Step 3: Validating domain format for Azure Front Door...
  Sanitized resource name: afdtest003-net
  ✓ Domain format validated
  Step 4: Verifying readiness for Front Door integration...
  ⚠️  NOTE: Actual Front Door configuration not performed (dry run mode)
  ⚠️  Production would configure:
    - Custom domain: afdtest003.net
    - TLS Version: Minimum TLS 1.2
    - Managed Certificate: Yes
    - DNS Validation: Required
  ✅ TEST PASSED: afdtest003.net is ready for Azure Front Door integration

Test 4: afdtest004.io
Description: Test domain for Azure Front Door integration - io TLD
------------------------------------------------------------
  Step 1: Checking DNS Zone for afdtest004.io...
  Result: DNS Zone does not exist
  ⚠️  NOTE: DNS Zone would need to be created
  ⚠️  Skipping DNS Zone creation (dry run mode)
  Step 2: Checking Front Door configuration for afdtest004.io...
  Result: Domain does not exist in Front Door
  Step 3: Validating domain format for Azure Front Door...
  Sanitized resource name: afdtest004-io
  ✓ Domain format validated
  Step 4: Verifying readiness for Front Door integration...
  ⚠️  NOTE: Actual Front Door configuration not performed (dry run mode)
  ⚠️  Production would configure:
    - Custom domain: afdtest004.io
    - TLS Version: Minimum TLS 1.2
    - Managed Certificate: Yes
    - DNS Validation: Required
  ✅ TEST PASSED: afdtest004.io is ready for Azure Front Door integration

============================================================
Test Summary
============================================================
Total Tests: 4
Passed: 3
Failed: 0
Skipped: 1
```

## Interpreting Results

### Success Indicators (✅ ✓)
- Domain format validated successfully
- DNS zone status confirmed (exists or needs creation)
- Domain is ready for Front Door integration
- Test passed

### Warning Indicators (⚠️)
- DNS zone needs to be created (expected for new domains)
- Actual configuration not performed (expected in dry run mode)
- Domain already configured in Front Door (idempotent check)

### Failure Indicators (❌)
- Invalid domain name format
- Authentication errors
- Missing configuration variables
- Azure API errors

### Skip Indicators (ℹ️)
- Domain already exists in Front Door
- No action needed for this domain

## Troubleshooting

### Authentication Errors

**Problem**: "DefaultAzureCredential failed to retrieve a token"

**Solution**:
```bash
# Option 1: Azure CLI login
az login
az account set --subscription "your-subscription-id"

# Option 2: Set service principal credentials
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
```

### Configuration Errors

**Problem**: "AZURE_SUBSCRIPTION_ID is required"

**Solution**: Ensure all required configuration variables are set via user secrets or environment variables.

### Permission Errors

**Problem**: "The client does not have authorization to perform action"

**Solution**:
1. Verify you have Contributor role on the Front Door profile
2. Check DNS zone permissions
3. Confirm subscription access
4. Wait a few minutes for role assignments to propagate

### Resource Not Found Errors

**Problem**: "Front Door profile not found"

**Solution**:
1. Verify the Front Door profile name is correct
2. Confirm the resource group name matches
3. Check the subscription ID
4. Ensure the Front Door resource exists in Azure Portal

### DNS Zone Errors

**Problem**: "Unable to check DNS zone"

**Solution**:
1. Verify DNS resource group name is correct
2. Confirm you have read permissions on DNS zones
3. Check if DNS zone resource provider is registered
4. Ensure network connectivity to Azure

## Running on Linux

This application is fully compatible with Linux environments:

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 azure-cli

# Fedora/RHEL
sudo dnf install dotnet-sdk-10.0 azure-cli

# Authenticate
az login

# Build and run
cd AzureFrontDoorTest
dotnet build
dotnet run
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Azure Login
  uses: azure/login@v1
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}

- name: Run Azure Front Door Tests
  env:
    AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    AZURE_RESOURCE_GROUP_NAME: "myapp-frontdoor-rg"
    AZURE_FRONTDOOR_PROFILE_NAME: "myapp-frontdoor"
    AZURE_DNS_RESOURCE_GROUP: "myapp-dns-rg"
  run: |
    cd AzureFrontDoorTest
    dotnet run
```

### Azure DevOps Example

```yaml
- task: AzureCLI@2
  displayName: 'Run Azure Front Door Tests'
  inputs:
    azureSubscription: 'MyAzureConnection'
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      cd AzureFrontDoorTest
      dotnet run
  env:
    AZURE_SUBSCRIPTION_ID: $(AzureSubscriptionId)
    AZURE_RESOURCE_GROUP_NAME: 'myapp-frontdoor-rg'
    AZURE_FRONTDOOR_PROFILE_NAME: 'myapp-frontdoor'
    AZURE_DNS_RESOURCE_GROUP: 'myapp-dns-rg'
```

## Azure Front Door Architecture

### Components Tested

1. **Custom Domain Configuration**
   - Domain name validation
   - Resource naming (sanitization)
   - Existence checks

2. **DNS Integration**
   - DNS zone validation
   - Zone existence checks
   - Readiness assessment

3. **TLS/SSL Configuration** (Production)
   - Managed certificates (Azure-managed)
   - Minimum TLS version (1.2)
   - HTTPS enforcement

4. **DNS Validation** (Production)
   - CNAME record requirement
   - Domain ownership verification
   - Certificate provisioning

### Production Workflow

```
Test Application (Dry Run)
    ↓
DNS Zone Check → Create if needed
    ↓
Front Door Domain Check → Add if new
    ↓
Certificate Provisioning → Azure-managed
    ↓
DNS Validation → CNAME verification
    ↓
Domain Active → HTTPS enabled
```

## Related Components

- **FrontDoorService** - Implementation of Azure Front Door integration
- **DnsZoneService** - Azure DNS zone management
- **DomainRegistrationTriggerFunction** - Cosmos DB trigger for Front Door integration
- **DomainRegistration** - Entity model for domain registrations

## Security Considerations

- **Never commit credentials** to source control
- Use managed identities when running in Azure
- Service principals should have minimal required permissions
- Rotate credentials regularly
- Test data should not contain real domain names you plan to use
- Review Azure RBAC assignments periodically

## Cost Considerations

### This Test (Dry Run)
- **No charges** - Read-only operations only
- No infrastructure changes
- No certificate provisioning

### Production Operations
- Azure Front Door Premium/Standard tier charges
- DNS zone hosting charges
- Certificate management (included with managed certificates)
- Data transfer costs (egress)

## Production Deployment Notes

To perform actual Front Door configuration (not covered by this test):
1. Remove dry run mode comments in production code
2. Implement proper error handling and retry logic
3. Add certificate validation monitoring
4. Configure health probes and routing rules
5. Set up custom routing policies
6. Implement WAF policies (Web Application Firewall)
7. Configure caching rules
8. Add monitoring and alerting
9. Document DNS validation steps for domain owners

## Advanced Testing Scenarios

### Testing with Existing Infrastructure

If you have a real Front Door profile and want to test against it:
1. Use actual domain names you control
2. Have DNS zones already created
3. Set appropriate environment variables
4. Review test output for configuration drift

### Testing DNS Zone Creation

To test DNS zone creation readiness:
1. Use domain names that don't have zones yet
2. Verify the test reports they need creation
3. Manually create zones to validate the test detects them

### Testing Domain Conflicts

To test duplicate domain detection:
1. Add domains to Front Door manually
2. Run the test with those domains
3. Verify the test skips already-configured domains

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review Azure Front Door documentation
3. Consult Azure DNS documentation
4. Check the main project README.md
5. Verify Azure subscription status and quotas
6. Review Azure service health status
