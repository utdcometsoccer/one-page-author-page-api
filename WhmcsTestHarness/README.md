# WHMCS Domain Registration Test Harness

A console application for testing WHMCS domain registration functionality. This test harness validates domain registration parameters and tests the WHMCS API integration.

## Purpose

This test application is designed to:

1. Verify WHMCS API integration for domain registration
2. Test domain registration parameters and contact information
3. Validate API request/response handling
4. Confirm database compatibility for domain registration records
5. Run on Linux laptops or development environments

## Prerequisites

### Software Requirements

- .NET 10.0 SDK or later
- Access to a WHMCS instance with API credentials
- Azure Cosmos DB instance (for test data validation)
- Linux, macOS, or Windows environment

### WHMCS Setup

1. Access your WHMCS admin panel
2. Navigate to: Setup → Staff Management → API Credentials
3. Create a new API credential:
   - Select admin user for API operations
   - (Optional) Add IP restrictions for security
   - Enable API checkbox
   - Save and copy identifier/secret

### Azure Cosmos DB Setup

- Cosmos DB account with the OnePageAuthor database
- DomainRegistrations container configured
- Valid connection credentials

## Configuration

### Using User Secrets (Recommended for Development)

```bash
cd WhmcsTestHarness
dotnet user-secrets init
dotnet user-secrets set "WHMCS_API_URL" "https://your-whmcs.com/includes/api.php"
dotnet user-secrets set "WHMCS_API_IDENTIFIER" "your-api-identifier"
dotnet user-secrets set "WHMCS_API_SECRET" "your-api-secret"
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-primary-key=="
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthor"
```

### Using Environment Variables (Linux)

```bash
export WHMCS_API_URL="https://your-whmcs.com/includes/api.php"
export WHMCS_API_IDENTIFIER="your-api-identifier"
export WHMCS_API_SECRET="your-api-secret"
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key=="
export COSMOSDB_DATABASE_ID="OnePageAuthor"
```

### Configuration Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `WHMCS_API_URL` | Yes | WHMCS API endpoint URL | `https://whmcs.example.com/includes/api.php` |
| `WHMCS_API_IDENTIFIER` | Yes | API authentication identifier | `abc123def456` |
| `WHMCS_API_SECRET` | Yes | API authentication secret | `secret123456` |
| `COSMOSDB_ENDPOINT_URI` | Yes | Cosmos DB endpoint | `https://account.documents.azure.com:443/` |
| `COSMOSDB_PRIMARY_KEY` | Yes | Cosmos DB primary key | `your-key==` |
| `COSMOSDB_DATABASE_ID` | Yes | Database name | `OnePageAuthor` |

## Usage

### Build the Application

```bash
cd WhmcsTestHarness
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
      "city": "Los Angeles",
      "state": "CA",
      "country": "United States",
      "zipCode": "90001",
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

1. **Registration Parameters Validation**
   - Validates domain information structure
   - Checks contact information completeness
   - Verifies data format compliance with WHMCS requirements

2. **WHMCS API Integration**
   - Tests actual API calls to WHMCS DomainRegister endpoint
   - Validates API authentication
   - Checks API response handling
   - Reports success or failure of registration attempts

3. **Database Compatibility**
   - Checks if domain already exists in Cosmos DB
   - Validates partition key and document structure
   - Confirms data can be stored correctly

### Important Notes

- **This test makes ACTUAL API calls to WHMCS** - Unlike the Google Domains test harness which runs in dry-run mode, this harness will attempt real domain registrations if WHMCS is configured properly
- **Billing implications** - Successful registrations may result in charges from your domain registrar (via WHMCS)
- **Use test/sandbox environment** - It is highly recommended to test against a WHMCS sandbox/test environment first
- **Test domain availability** - Ensure test domains are available or use domains you intend to register

## Sample Output

```
WHMCS Domain Registration Test Harness
======================================

WHMCS API URL: https://****example.com
WHMCS API Identifier: abc1****ef56
Cosmos DB Endpoint: https://****com:443/
Cosmos DB Database: OnePageAuthor

Processing test file: test-domains.json
------------------------------------------------------------
Loaded 3 test registration(s)

Test 1: whmcstest001.com
------------------------------------------------------------
  Step 1: Validating registration parameters for whmcstest001.com...
  Registrant: WHMCS TestUser
  Email: testuser@example.com
  Location: Los Angeles, CA
  ✓ Registration parameters validated
  Step 2: Testing WHMCS registration for whmcstest001.com...
  ✅ WHMCS API registration call succeeded
  Step 3: Verifying database compatibility...
  ✓ Domain is new and ready for database insertion
  ✅ TEST PASSED: whmcstest001.com registration completed successfully

Test 2: whmcstest002.org
------------------------------------------------------------
  Step 1: Validating registration parameters for whmcstest002.org...
  Registrant: WHMCS TestUser
  Email: testuser@example.com
  Location: Los Angeles, CA
  ✓ Registration parameters validated
  Step 2: Testing WHMCS registration for whmcstest002.org...
  ⚠️  WHMCS API registration call returned false (check logs for details)
  Step 3: Verifying database compatibility...
  ✓ Domain is new and ready for database insertion
  ⚠️  TEST COMPLETED WITH WARNINGS: whmcstest002.org registration returned false

Test 3: whmcstest003.net
------------------------------------------------------------
  Step 1: Validating registration parameters for whmcstest003.net...
  Registrant: WHMCS TestUser2
  Email: testuser2@example.com
  Location: San Diego, CA
  ✓ Registration parameters validated
  Step 2: Testing WHMCS registration for whmcstest003.net...
  ✅ WHMCS API registration call succeeded
  Step 3: Verifying database compatibility...
  ✓ Domain is new and ready for database insertion
  ✅ TEST PASSED: whmcstest003.net registration completed successfully

============================================================
Test Summary
============================================================
Total Tests: 3
```

## Interpreting Results

### Success Indicators (✅ ✓)

- All registration parameters are valid
- WHMCS API call succeeded
- Database compatibility confirmed
- Test passed

### Warning Indicators (⚠️)

- WHMCS API returned false (check WHMCS logs)
- Domain may not be available
- Registration may have failed due to WHMCS configuration
- Domain already exists in database

### Failure Indicators (❌)

- Missing required fields
- Invalid data format
- API connection errors
- Authentication failures

## Troubleshooting

### Authentication Errors

**Problem**: "WHMCS API returned error status 401"

**Solution**:
1. Verify API identifier and secret are correct
2. Check that API credential is enabled in WHMCS
3. Ensure IP restrictions (if any) allow your IP address

### Configuration Errors

**Problem**: "WHMCS_API_URL is required"

**Solution**: Ensure all required configuration variables are set via user secrets or environment variables.

### API Errors

**Problem**: "WHMCS API returned non-success result"

**Solution**:
1. Check WHMCS logs for detailed error messages
2. Verify registrar module is configured and active
3. Ensure registrar account has sufficient balance
4. Check domain availability
5. Verify TLD is supported by your registrar

### Cosmos DB Connection Errors

**Problem**: "Unable to connect to Cosmos DB"

**Solution**:
1. Verify endpoint URI is correct
2. Confirm primary key is valid
3. Check network connectivity to Azure
4. Ensure database and container exist

### Domain Registration Failures

**Problem**: Registration returns false but no error

**Solution**:
1. Check WHMCS Activity Log
2. Verify registrar module configuration
3. Ensure domain is available
4. Check registrar account balance
5. Verify contact information meets registrar requirements

## Running on Linux

This application is fully compatible with Linux environments:

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Fedora/RHEL
sudo dnf install dotnet-sdk-10.0

# Build and run
cd WhmcsTestHarness
dotnet build
dotnet run
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run WHMCS Domain Registration Tests
  env:
    WHMCS_API_URL: ${{ secrets.WHMCS_API_URL }}
    WHMCS_API_IDENTIFIER: ${{ secrets.WHMCS_API_IDENTIFIER }}
    WHMCS_API_SECRET: ${{ secrets.WHMCS_API_SECRET }}
    COSMOSDB_ENDPOINT_URI: ${{ secrets.COSMOSDB_ENDPOINT_URI }}
    COSMOSDB_PRIMARY_KEY: ${{ secrets.COSMOSDB_PRIMARY_KEY }}
    COSMOSDB_DATABASE_ID: "OnePageAuthor"
  run: |
    cd WhmcsTestHarness
    dotnet run
```

## Related Components

- **WhmcsService** - Implementation of WHMCS API integration
- **DomainRegistrationRepository** - Cosmos DB repository for domain registrations
- **DomainRegistration** - Entity model for domain registrations
- **DomainRegistrationTriggerFunction** - Azure Function that processes registrations

## Security Considerations

- **Never commit credentials** to source control
- Use user secrets or environment variables for sensitive data
- API credentials should have minimal required permissions
- Cosmos DB keys should be rotated regularly
- Test data should not contain real personal information
- Use HTTPS for all WHMCS API calls
- Consider IP restrictions on WHMCS API credentials

## Comparison with Google Domains Test Harness

| Feature | Google Domains Test | WHMCS Test |
|---------|---------------------|------------|
| API Calls | Dry run mode (availability check only) | Live API calls (actual registration) |
| Billing Impact | None | Possible domain registration charges |
| Registration | Simulated | Actual (if WHMCS configured) |
| Use Case | Pre-production validation | Integration testing |
| Safety | Safe for production credentials | Requires test environment |

## Best Practices

1. **Test Environment First**: Always test against a WHMCS sandbox/development environment
2. **Use Test Domains**: Use obviously test domains (e.g., whmcstest001.com) that you intend to register
3. **Check Availability**: Manually verify domain availability before running tests
4. **Monitor Costs**: Keep track of domain registration costs during testing
5. **Review Logs**: Check WHMCS Activity Log after each test run
6. **Clean Up**: Delete or cancel test domain registrations after testing
7. **Secure Credentials**: Never commit WHMCS credentials to source control

## Support

For issues or questions:

1. Check the troubleshooting section above
2. Review WHMCS API documentation: https://developers.whmcs.com/api-reference/domainregister/
3. Consult the WHMCS Integration Summary: `docs/WHMCS_INTEGRATION_SUMMARY.md`
4. Check the main project README.md
5. Verify Azure Cosmos DB connection status

## See Also

- **GoogleDomainRegistrationTest** - Similar test harness for Google Cloud Domains (dry-run mode)
- **DomainRegistrationTestHarness** - General domain registration test harness
- **WHMCS_INTEGRATION_SUMMARY.md** - Comprehensive WHMCS integration documentation
