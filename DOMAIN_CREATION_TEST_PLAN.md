# Domain Creation Test Plan

Comprehensive test plan for validating the Domain Creation Process in the OnePageAuthor API platform. This document describes the testing strategy, test applications, and procedures for testing both Google Cloud Platform domain registration and Azure Front Door infrastructure integration.

## Overview

The OnePageAuthor platform provides automated domain registration through Google Cloud Platform and automated infrastructure provisioning through Azure Front Door. This test plan ensures both integrations work correctly and can be validated on Linux development environments.

## Architecture Overview

```
User Request
    ↓
Domain Registration API (Azure Function)
    ↓
Cosmos DB (DomainRegistrations container)
    ↓ (Cosmos DB Trigger)
    ├─→ Google Domain Registration Function → Google Cloud Domains API
    └─→ Domain Registration Trigger Function → Azure Front Door Service
```

### Components Under Test

1. **Google Cloud Platform Integration**
   - Domain availability checking via Google Domains API
   - Domain registration parameter validation
   - Contact information formatting
   - Long-running operation handling

2. **Azure Front Door Integration**
   - Custom domain addition to Front Door profiles
   - DNS zone management and validation
   - TLS certificate provisioning (Azure-managed)
   - Domain naming and sanitization

3. **Data Layer**
   - Cosmos DB domain registration storage
   - Repository pattern validation
   - Partition key handling (UPN-based)

## Test Applications

### 1. GoogleDomainRegistrationTest

**Location**: `/GoogleDomainRegistrationTest/`

**Purpose**: Validates Google Cloud Platform domain registration API integration without performing actual registrations (dry run mode).

**Key Features**:
- Domain availability checking
- Registration parameter validation
- Contact information verification
- Database compatibility testing
- Safe for repeated execution (no charges)

**Documentation**: See `GoogleDomainRegistrationTest/README.md`

### 2. AzureFrontDoorTest

**Location**: `/AzureFrontDoorTest/`

**Purpose**: Validates Azure Front Door custom domain integration without making infrastructure changes (dry run mode).

**Key Features**:
- DNS zone existence checking
- Front Door domain conflict detection
- Domain format validation
- TLS configuration verification
- Safe for repeated execution (no infrastructure changes)

**Documentation**: See `AzureFrontDoorTest/README.md`

### 3. DomainRegistrationTestHarness (Existing)

**Location**: `/DomainRegistrationTestHarness/`

**Purpose**: Seeds domain registration data into Cosmos DB to trigger the actual processing functions.

**Note**: This performs actual writes to Cosmos DB and can trigger real domain registrations. Use with caution.

## Test Environment Requirements

### Software Prerequisites

- **.NET 10.0 SDK** or later
- **Azure CLI** (for Azure authentication)
- **Google Cloud SDK** (optional, for gcloud authentication)
- **Linux, macOS, or Windows** environment

### Cloud Resources Required

#### Google Cloud Platform
- Active GCP project with billing enabled
- Cloud Domains API enabled
- Service account with Domains API permissions
- Authentication configured (service account key or ADC)

#### Microsoft Azure
- Active Azure subscription
- Azure Front Door Premium or Standard profile
- Azure DNS zones configured
- Contributor permissions on Front Door and DNS resources
- Azure Cosmos DB instance with OnePageAuthor database

### Authentication Setup

#### Google Cloud Platform

**Option 1: Service Account (Recommended for CI/CD)**
```bash
# Download service account key from GCP Console
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"
```

**Option 2: Application Default Credentials (Local Development)**
```bash
gcloud auth application-default login
```

#### Microsoft Azure

**Option 1: Azure CLI (Recommended for Local Development)**
```bash
az login
az account set --subscription "your-subscription-id"
```

**Option 2: Service Principal (CI/CD)**
```bash
export AZURE_TENANT_ID="your-tenant-id"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_CLIENT_SECRET="your-client-secret"
```

**Option 3: Managed Identity (Azure-hosted)**
- Automatically configured when running in Azure
- No additional setup required

## Configuration

### Environment Variables

All test applications support configuration via environment variables or .NET user secrets.

#### Google Domain Registration Test

```bash
export GOOGLE_CLOUD_PROJECT_ID="your-project-id"
export GOOGLE_DOMAINS_LOCATION="us-central1"
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key=="
export COSMOSDB_DATABASE_ID="OnePageAuthor"
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/key.json"
```

#### Azure Front Door Test

```bash
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_RESOURCE_GROUP_NAME="your-frontdoor-rg"
export AZURE_FRONTDOOR_PROFILE_NAME="your-frontdoor-profile"
export AZURE_DNS_RESOURCE_GROUP="your-dns-rg"
```

### User Secrets (Alternative to Environment Variables)

```bash
# Google Domain Registration Test
cd GoogleDomainRegistrationTest
dotnet user-secrets init
dotnet user-secrets set "GOOGLE_CLOUD_PROJECT_ID" "your-project-id"
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-primary-key=="
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthor"

# Azure Front Door Test
cd ../AzureFrontDoorTest
dotnet user-secrets init
dotnet user-secrets set "AZURE_SUBSCRIPTION_ID" "your-subscription-id"
dotnet user-secrets set "AZURE_RESOURCE_GROUP_NAME" "your-frontdoor-rg"
dotnet user-secrets set "AZURE_FRONTDOOR_PROFILE_NAME" "your-frontdoor-profile"
dotnet user-secrets set "AZURE_DNS_RESOURCE_GROUP" "your-dns-rg"
```

## Test Data

### Sample Data Locations

1. **Google Domain Registration Test**
   - Default: `GoogleDomainRegistrationTest/data/test-domains.json`
   - Contains sample domain registration requests with contact information

2. **Azure Front Door Test**
   - Default: `AzureFrontDoorTest/data/test-domains.json`
   - Contains sample domain names for Front Door testing

3. **Existing Database Data**
   - Query Cosmos DB `DomainRegistrations` container for existing test data
   - Use existing UPNs from database for realistic testing

### Creating Custom Test Data

#### Google Domain Registration Format

```json
[
  {
    "upn": "testuser@example.com",
    "domain": {
      "topLevelDomain": "com",
      "secondLevelDomain": "uniquedomainname"
    },
    "contactInformation": {
      "firstName": "Test",
      "lastName": "User",
      "address": "123 Test Street",
      "city": "Test City",
      "state": "CA",
      "country": "United States",
      "zipCode": "94043",
      "emailAddress": "test@example.com",
      "telephoneNumber": "+1-555-123-4567"
    },
    "status": 0
  }
]
```

#### Azure Front Door Format

```json
[
  {
    "domainName": "example.com",
    "upn": "testuser@example.com",
    "description": "Test domain for Azure Front Door integration"
  }
]
```

## Test Execution

### Quick Start (Linux)

```bash
# Clone repository
cd /path/to/one-page-author-page-api

# Install dependencies
dotnet restore

# Authenticate with cloud providers
az login
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"

# Configure environment (or use user secrets)
export GOOGLE_CLOUD_PROJECT_ID="your-project-id"
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
# ... (see Configuration section for complete list)

# Run Google Domain Registration Test
cd GoogleDomainRegistrationTest
dotnet build
dotnet run

# Run Azure Front Door Test
cd ../AzureFrontDoorTest
dotnet build
dotnet run
```

### Running Individual Tests

#### Test 1: Google Cloud Platform Domain Registration

```bash
cd GoogleDomainRegistrationTest

# Build
dotnet build

# Run with default test data
dotnet run

# Run with custom test data
dotnet run -- /path/to/custom-test-data.json
```

**Expected Output**:
- Domain availability checks for each domain
- Registration parameter validation
- Database compatibility verification
- Pass/Skip status for each test case

**Success Criteria**:
- ✅ All available domains pass validation
- ⚠️ Unavailable domains are skipped (expected)
- ✅ No authentication or API errors
- ✅ Contact information is properly formatted

#### Test 2: Azure Front Door Integration

```bash
cd AzureFrontDoorTest

# Build
dotnet build

# Run with default test data
dotnet run

# Run with custom test data
dotnet run -- /path/to/custom-test-data.json
```

**Expected Output**:
- DNS zone existence checks
- Front Door domain conflict detection
- Domain format validation
- Integration readiness confirmation

**Success Criteria**:
- ✅ DNS zone status correctly reported
- ✅ Existing domains are detected and skipped
- ✅ New domains pass format validation
- ✅ No authentication or API errors

### Running All Tests

```bash
#!/bin/bash
# run-all-domain-tests.sh

echo "Running Domain Creation Tests"
echo "=============================="

# Set working directory
cd /path/to/one-page-author-page-api

# Test 1: Google Domain Registration
echo ""
echo "Test 1: Google Cloud Platform Domain Registration"
echo "--------------------------------------------------"
cd GoogleDomainRegistrationTest
dotnet build --configuration Release
dotnet run --configuration Release
GOOGLE_TEST_RESULT=$?

# Test 2: Azure Front Door
echo ""
echo "Test 2: Azure Front Door Integration"
echo "-------------------------------------"
cd ../AzureFrontDoorTest
dotnet build --configuration Release
dotnet run --configuration Release
AZURE_TEST_RESULT=$?

# Summary
echo ""
echo "Test Execution Summary"
echo "======================"
echo "Google Domain Test: $([ $GOOGLE_TEST_RESULT -eq 0 ] && echo 'PASSED ✅' || echo 'FAILED ❌')"
echo "Azure Front Door Test: $([ $AZURE_TEST_RESULT -eq 0 ] && echo 'PASSED ✅' || echo 'FAILED ❌')"

# Exit with failure if any test failed
[ $GOOGLE_TEST_RESULT -eq 0 ] && [ $AZURE_TEST_RESULT -eq 0 ]
```

## Test Scenarios

### Scenario 1: New Domain Registration (Happy Path)

**Objective**: Validate complete domain registration flow for a brand new domain.

**Steps**:
1. Choose a unique, available domain name
2. Run Google Domain Registration Test
   - Verify domain is available
   - Confirm registration parameters are valid
   - Check database compatibility
3. Run Azure Front Door Test
   - Verify DNS zone status
   - Confirm domain can be added to Front Door
   - Validate TLS configuration readiness

**Expected Results**:
- ✅ Google test: Domain available and ready
- ✅ Azure test: Domain ready for Front Door
- ✅ No errors or warnings (except dry run mode notices)

### Scenario 2: Existing Domain Detection

**Objective**: Verify the system correctly detects and handles already-registered domains.

**Steps**:
1. Use a domain name already in your Front Door profile
2. Run Azure Front Door Test
3. Verify the test detects the existing configuration

**Expected Results**:
- ℹ️ Domain detected as already existing
- ⚠️ Test skipped (idempotent behavior)
- ✅ No errors

### Scenario 3: Invalid Domain Handling

**Objective**: Test error handling for invalid or unavailable domains.

**Steps**:
1. Create test data with well-known unavailable domain (e.g., "google.com")
2. Run Google Domain Registration Test
3. Verify graceful handling

**Expected Results**:
- ⚠️ Domain reported as unavailable
- ⚠️ Test skipped
- ✅ No crashes or unhandled exceptions

### Scenario 4: Missing Configuration

**Objective**: Validate error messages when configuration is incomplete.

**Steps**:
1. Unset a required environment variable
2. Run either test application
3. Verify clear error message

**Expected Results**:
- ❌ Clear error message indicating missing configuration
- ❌ Application exits gracefully
- ℹ️ Error message indicates which variable is missing

### Scenario 5: Database Integration

**Objective**: Verify domain data can be stored in Cosmos DB correctly.

**Steps**:
1. Run Google Domain Registration Test
2. Check Cosmos DB for test data compatibility
3. Query `DomainRegistrations` container

**Expected Results**:
- ✅ Database compatibility confirmed
- ✅ Partition key (UPN) handled correctly
- ✅ Data structure matches entity model

## Continuous Integration

### GitHub Actions Workflow

```yaml
name: Domain Creation Tests

on:
  pull_request:
    paths:
      - 'OnePageAuthorLib/api/GoogleDomainsService.cs'
      - 'OnePageAuthorLib/api/FrontDoorService.cs'
      - 'GoogleDomainRegistrationTest/**'
      - 'AzureFrontDoorTest/**'
  workflow_dispatch:

jobs:
  test-domain-creation:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Authenticate to Google Cloud
        uses: google-github-actions/auth@v2
        with:
          credentials_json: ${{ secrets.GCP_SERVICE_ACCOUNT_KEY }}
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Run Google Domain Registration Test
        env:
          GOOGLE_CLOUD_PROJECT_ID: ${{ secrets.GOOGLE_CLOUD_PROJECT_ID }}
          COSMOSDB_ENDPOINT_URI: ${{ secrets.COSMOSDB_ENDPOINT_URI }}
          COSMOSDB_PRIMARY_KEY: ${{ secrets.COSMOSDB_PRIMARY_KEY }}
          COSMOSDB_DATABASE_ID: "OnePageAuthor"
        run: |
          cd GoogleDomainRegistrationTest
          dotnet build
          dotnet run
      
      - name: Run Azure Front Door Test
        env:
          AZURE_SUBSCRIPTION_ID: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
          AZURE_RESOURCE_GROUP_NAME: ${{ secrets.AZURE_FRONTDOOR_RG }}
          AZURE_FRONTDOOR_PROFILE_NAME: ${{ secrets.AZURE_FRONTDOOR_PROFILE }}
          AZURE_DNS_RESOURCE_GROUP: ${{ secrets.AZURE_DNS_RG }}
        run: |
          cd AzureFrontDoorTest
          dotnet build
          dotnet run
```

## Troubleshooting

### Common Issues

#### Google Cloud Platform Issues

| Issue | Solution |
|-------|----------|
| "Cloud Domains API has not been used" | Enable the API in GCP Console |
| "Default credentials not available" | Set `GOOGLE_APPLICATION_CREDENTIALS` or run `gcloud auth application-default login` |
| "Permission denied" | Ensure service account has Domains API permissions |
| All domains show unavailable | Use unique domain names; check GCP project status |

#### Azure Issues

| Issue | Solution |
|-------|----------|
| "DefaultAzureCredential failed" | Run `az login` or set service principal environment variables |
| "Front Door profile not found" | Verify resource group and profile name configuration |
| "Insufficient permissions" | Ensure Contributor role on Front Door and DNS resources |
| "DNS zone not found" | Verify DNS resource group configuration |

#### General Issues

| Issue | Solution |
|-------|----------|
| "Configuration variable is required" | Set all required environment variables or user secrets |
| Build errors | Run `dotnet restore` and ensure .NET 10.0 SDK is installed |
| Network connectivity | Check firewall and proxy settings |
| Cosmos DB connection failed | Verify endpoint URI and primary key; check network access |

## Success Metrics

### Test Execution Metrics

- **100% Pass Rate**: All tests pass (excluding expected skips)
- **< 30 seconds**: Google Domain Registration Test execution time
- **< 30 seconds**: Azure Front Door Test execution time
- **Zero Errors**: No unhandled exceptions or API errors
- **Clear Output**: All test results clearly labeled with ✅, ⚠️, or ❌

### Quality Metrics

- **Code Coverage**: Test applications cover all major integration points
- **Documentation**: Complete README files with examples
- **Linux Compatibility**: All tests run successfully on Ubuntu 22.04+
- **Idempotency**: Tests can be run repeatedly without side effects
- **Safety**: Dry run mode prevents accidental charges or infrastructure changes

## Production Deployment Checklist

Before deploying domain creation functionality to production:

- [ ] All tests pass in CI/CD pipeline
- [ ] Google Cloud Platform billing is enabled
- [ ] Azure Front Door profile is provisioned
- [ ] DNS zones are configured correctly
- [ ] Monitoring and alerting configured
- [ ] Error handling and retry logic implemented
- [ ] Webhook handlers for async operations tested
- [ ] Security review completed
- [ ] Cost estimates reviewed and approved
- [ ] Rollback plan documented
- [ ] User documentation updated
- [ ] Support team trained on domain creation process

## Related Documentation

- **Main README**: `/README.md` - Project overview
- **API Documentation**: `/API-Documentation.md` - API reference
- **Implementation Summary**: `/IMPLEMENTATION_SUMMARY.md` - Domain creation implementation details
- **Google Domain Test**: `/GoogleDomainRegistrationTest/README.md` - Detailed Google test documentation
- **Azure Front Door Test**: `/AzureFrontDoorTest/README.md` - Detailed Azure test documentation
- **Domain Registration Harness**: `/DomainRegistrationTestHarness/README.md` - Database seeding tool

## Support and Feedback

For issues, questions, or suggestions regarding this test plan:

1. Review troubleshooting sections in individual test application READMEs
2. Check cloud provider documentation (Google Cloud Domains, Azure Front Door)
3. Verify all prerequisites and configuration requirements are met
4. Review existing issues in the project repository
5. Consult with the development team

## Appendix A: Complete Configuration Reference

### All Environment Variables

```bash
# Google Cloud Platform
GOOGLE_CLOUD_PROJECT_ID="your-project-id"
GOOGLE_DOMAINS_LOCATION="us-central1"  # Optional, defaults to us-central1
GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"

# Azure
AZURE_SUBSCRIPTION_ID="your-subscription-id"
AZURE_RESOURCE_GROUP_NAME="your-frontdoor-rg"
AZURE_FRONTDOOR_PROFILE_NAME="your-frontdoor-profile"
AZURE_DNS_RESOURCE_GROUP="your-dns-rg"

# Azure Authentication (Service Principal - Optional)
AZURE_TENANT_ID="your-tenant-id"
AZURE_CLIENT_ID="your-client-id"
AZURE_CLIENT_SECRET="your-client-secret"

# Cosmos DB
COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
COSMOSDB_PRIMARY_KEY="your-primary-key=="
COSMOSDB_DATABASE_ID="OnePageAuthor"
```

## Appendix B: Test Data Templates

### Minimal Google Domain Registration Test Data

```json
[
  {
    "upn": "test@example.com",
    "domain": {
      "topLevelDomain": "com",
      "secondLevelDomain": "testdomain001"
    },
    "contactInformation": {
      "firstName": "Test",
      "lastName": "User",
      "address": "123 Main St",
      "city": "Anytown",
      "state": "CA",
      "country": "United States",
      "zipCode": "12345",
      "emailAddress": "test@example.com",
      "telephoneNumber": "+1-555-123-4567"
    },
    "status": 0
  }
]
```

### Minimal Azure Front Door Test Data

```json
[
  {
    "domainName": "testdomain001.com",
    "upn": "test@example.com",
    "description": "Basic test domain"
  }
]
```

## Version History

- **Version 1.0** (2026-01-22): Initial test plan created
  - Google Domain Registration Test application
  - Azure Front Door Test application
  - Comprehensive documentation
  - CI/CD integration examples
