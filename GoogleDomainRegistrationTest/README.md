# Google Cloud Platform Domain Registration Test

A console application for testing Google Cloud Platform domain registration functionality. This test harness validates domain availability checks and registration parameters without performing actual domain registrations (dry run mode).

## Purpose

This test application is designed to:
1. Verify Google Cloud Platform domain registration API integration
2. Test domain availability checking functionality
3. Validate domain registration parameters and contact information
4. Confirm database compatibility for domain registration records
5. Run on Linux laptops or development environments

## Prerequisites

### Software Requirements
- .NET 10.0 SDK or later
- Access to Google Cloud Platform with Domains API enabled
- Azure Cosmos DB instance (for test data validation)
- Linux, macOS, or Windows environment

### Google Cloud Platform Setup
1. Create a Google Cloud Platform project
2. Enable the Cloud Domains API
3. Set up authentication:
   - **Option A (Recommended)**: Use service account with JSON key file
     ```bash
     export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"
     ```
   - **Option B**: Use Application Default Credentials (ADC)
     ```bash
     gcloud auth application-default login
     ```

### Azure Cosmos DB Setup
- Cosmos DB account with the OnePageAuthor database
- DomainRegistrations container configured
- Valid connection credentials

## Configuration

### Using User Secrets (Recommended for Development)

```bash
cd GoogleDomainRegistrationTest
dotnet user-secrets init
dotnet user-secrets set "GOOGLE_CLOUD_PROJECT_ID" "your-project-id"
dotnet user-secrets set "GOOGLE_DOMAINS_LOCATION" "us-central1"
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-primary-key=="
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthor"
```

### Using Environment Variables (Linux)

```bash
export GOOGLE_CLOUD_PROJECT_ID="your-project-id"
export GOOGLE_DOMAINS_LOCATION="us-central1"
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key=="
export COSMOSDB_DATABASE_ID="OnePageAuthor"
```

### Configuration Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `GOOGLE_CLOUD_PROJECT_ID` | Yes | Google Cloud project ID | `my-project-12345` |
| `GOOGLE_DOMAINS_LOCATION` | No | GCP region for domains | `us-central1` (default) |
| `COSMOSDB_ENDPOINT_URI` | Yes | Cosmos DB endpoint | `https://account.documents.azure.com:443/` |
| `COSMOSDB_PRIMARY_KEY` | Yes | Cosmos DB primary key | `your-key==` |
| `COSMOSDB_DATABASE_ID` | Yes | Database name | `OnePageAuthor` |
| `GOOGLE_APPLICATION_CREDENTIALS` | Yes* | Path to service account JSON | `/path/to/key.json` |

*Required if not using Application Default Credentials

## Usage

### Build the Application

```bash
cd GoogleDomainRegistrationTest
dotnet build
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

### Run in Interactive Mode (Database)

```bash
dotnet run -- --interactive
```

Prompts for a User Principal Name (UPN/email) and retrieves all domain registrations for that user from Cosmos DB.

### Test All Domains for a Specific User

```bash
dotnet run -- --upn testuser@example.com
```

Retrieves and tests all domain registrations for the specified user from the database.

### Test a Specific Domain

```bash
dotnet run -- --domain com example
```

Tests a specific domain by TLD and SLD. Looks up the domain in the database first. If not found, creates minimal test data.

### Command Line Options Summary

| Option | Arguments | Description |
|--------|-----------|-------------|
| (none) | - | Use default test file (`data/test-domains.json`) |
| `<file-path>` | JSON file path | Use specified JSON file |
| `--interactive` or `-i` | - | Interactive mode - prompt for UPN |
| `--upn` | email address | Test all domains for a user from database |
| `--domain` | tld sld | Test specific domain (e.g., `com example`) |

## Test Data Format

The test data file should contain an array of domain registration objects:

```json
[
  {
    "upn": "testuser@example.com",
    "domain": {
      "topLevelDomain": "com",
      "secondLevelDomain": "mytestdomain"
    },
    "contactInformation": {
      "firstName": "John",
      "lastName": "Doe",
      "address": "123 Main Street",
      "address2": "Suite 100",
      "city": "Mountain View",
      "state": "CA",
      "country": "United States",
      "zipCode": "94043",
      "emailAddress": "john@example.com",
      "telephoneNumber": "+1-555-123-4567"
    },
    "status": 0
  }
]
```

### Field Descriptions

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `upn` | Yes | string | User Principal Name (email) |
| `domain.topLevelDomain` | Yes | string | TLD (e.g., "com", "org", "net") |
| `domain.secondLevelDomain` | Yes | string | Second-level domain name |
| `contactInformation.firstName` | Yes | string | Registrant's first name |
| `contactInformation.lastName` | Yes | string | Registrant's last name |
| `contactInformation.address` | Yes | string | Street address |
| `contactInformation.city` | Yes | string | City |
| `contactInformation.state` | Yes | string | State/Province code |
| `contactInformation.country` | Yes | string | Country name |
| `contactInformation.zipCode` | Yes | string | Postal/ZIP code |
| `contactInformation.emailAddress` | Yes | string | Contact email |
| `contactInformation.telephoneNumber` | Yes | string | Phone number (E.164 format) |
| `status` | No | int | Registration status (0=Pending) |

### Status Values
- `0` - Pending (default for new registrations)
- `1` - InProgress
- `2` - Completed
- `3` - Failed
- `4` - Cancelled

## Test Scenarios

### What This Test Validates

1. **Domain Availability Check**
   - Queries Google Cloud Domains API to check if domain is available
   - Reports availability status for each domain

2. **Registration Parameters Validation**
   - Validates domain information structure
   - Checks contact information completeness
   - Verifies data format compliance with Google Cloud requirements

3. **Database Compatibility**
   - Checks if domain already exists in Cosmos DB
   - Validates partition key and document structure
   - Confirms data can be stored correctly

### What This Test Does NOT Do

- **Does NOT perform actual domain registrations** (dry run mode)
- **Does NOT charge your Google Cloud account** (no purchase operations)
- **Does NOT modify existing domain registrations**
- **Does NOT create DNS records**

## Sample Output

```
Google Cloud Platform Domain Registration Test
==============================================

Google Cloud Project ID: my-project-12345
Google Domains Location: us-central1
Cosmos DB Endpoint: https://*****.documents.azure.com:443/
Cosmos DB Database: OnePageAuthor

Processing test file: test-domains.json
------------------------------------------------------------
Loaded 3 test registration(s)

Test 1: gcptest001.com
------------------------------------------------------------
  Step 1: Checking availability for gcptest001.com...
  Result: ✓ Available
  Step 2: Testing registration parameters for gcptest001.com...
  Registrant: GCP TestUser
  Email: testuser@example.com
  Location: Mountain View, CA
  ✓ Registration parameters validated
  ⚠️  NOTE: Actual registration not performed (dry run mode)
  Step 3: Verifying database compatibility...
  ✓ Domain is new and ready for database insertion
  ✅ TEST PASSED: gcptest001.com is ready for Google Cloud registration

Test 2: gcptest002.org
------------------------------------------------------------
  Step 1: Checking availability for gcptest002.org...
  Result: ✗ Not Available
  ⚠️  SKIPPED: Domain gcptest002.org is not available for registration

Test 3: gcptest003.net
------------------------------------------------------------
  Step 1: Checking availability for gcptest003.net...
  Result: ✓ Available
  Step 2: Testing registration parameters for gcptest003.net...
  Registrant: GCP TestUser2
  Email: testuser2@example.com
  Location: San Francisco, CA
  ✓ Registration parameters validated
  ⚠️  NOTE: Actual registration not performed (dry run mode)
  Step 3: Verifying database compatibility...
  ✓ Domain is new and ready for database insertion
  ✅ TEST PASSED: gcptest003.net is ready for Google Cloud registration

============================================================
Test Summary
============================================================
Total Tests: 3
```

## Interpreting Results

### Success Indicators (✅ ✓)
- Domain is available for registration
- All registration parameters are valid
- Database compatibility confirmed
- Test passed

### Warning Indicators (⚠️)
- Domain not available (skipped)
- Actual registration not performed (expected in dry run mode)
- Domain already exists in database

### Failure Indicators (❌ ✗)
- Missing required fields
- Invalid data format
- API connection errors
- Authentication failures

## Troubleshooting

### Authentication Errors

**Problem**: "The Application Default Credentials are not available"

**Solution**:
```bash
# Option 1: Set service account credentials
export GOOGLE_APPLICATION_CREDENTIALS="/path/to/service-account-key.json"

# Option 2: Login with gcloud
gcloud auth application-default login
```

### Configuration Errors

**Problem**: "GOOGLE_CLOUD_PROJECT_ID is required"

**Solution**: Ensure all required configuration variables are set via user secrets or environment variables.

### API Errors

**Problem**: "Cloud Domains API has not been used in project"

**Solution**:
1. Navigate to Google Cloud Console
2. Enable the Cloud Domains API for your project
3. Wait a few minutes for propagation

### Cosmos DB Connection Errors

**Problem**: "Unable to connect to Cosmos DB"

**Solution**:
1. Verify endpoint URI is correct
2. Confirm primary key is valid
3. Check network connectivity to Azure
4. Ensure database and container exist

### Domain Availability Issues

**Problem**: All domains show as "Not Available"

**Solution**:
- Use unique domain names for testing
- Some TLDs have restrictions (use .com, .org, .net for testing)
- Check if you have proper permissions in Google Cloud

## Running on Linux

This application is fully compatible with Linux environments:

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Fedora/RHEL
sudo dnf install dotnet-sdk-10.0

# Build and run
cd GoogleDomainRegistrationTest
dotnet build
dotnet run
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run Google Domain Registration Tests
  env:
    GOOGLE_CLOUD_PROJECT_ID: ${{ secrets.GOOGLE_CLOUD_PROJECT_ID }}
    GOOGLE_APPLICATION_CREDENTIALS: ${{ secrets.GOOGLE_APPLICATION_CREDENTIALS }}
    COSMOSDB_ENDPOINT_URI: ${{ secrets.COSMOSDB_ENDPOINT_URI }}
    COSMOSDB_PRIMARY_KEY: ${{ secrets.COSMOSDB_PRIMARY_KEY }}
    COSMOSDB_DATABASE_ID: "OnePageAuthor"
  run: |
    cd GoogleDomainRegistrationTest
    dotnet run
```

## Related Components

- **GoogleDomainsService** - Implementation of Google Cloud Domains API integration
- **DomainRegistrationRepository** - Cosmos DB repository for domain registrations
- **DomainRegistration** - Entity model for domain registrations
- **GoogleDomainRegistrationFunction** - Azure Function that processes registrations

## Security Considerations

- **Never commit credentials** to source control
- Use user secrets or environment variables for sensitive data
- Service account keys should have minimal required permissions
- Cosmos DB keys should be rotated regularly
- Test data should not contain real personal information

## Production Deployment Notes

To perform actual domain registrations (not covered by this test):
1. Remove dry run mode comments in production code
2. Ensure billing is enabled in Google Cloud
3. Verify domain pricing and availability
4. Implement proper error handling and retry logic
5. Add monitoring and alerting for registration status
6. Implement webhook handlers for domain registration events

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review Google Cloud Domains API documentation
3. Consult the main project README.md
4. Check Azure Cosmos DB connection status
