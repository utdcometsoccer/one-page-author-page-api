# Domain Registration Test Harness

A console application that triggers Azure Cosmos DB functions by updating the DomainRegistrations container with data from JSON files.

## Purpose

This test harness is used to trigger the `DomainRegistrationTriggerFunction` Azure Function, which is configured with a Cosmos DB trigger on the DomainRegistrations container. When domain registrations are created or updated in Cosmos DB, the trigger function processes them and adds domains to Azure Front Door.

## Prerequisites

- .NET 10.0 SDK
- Azure Cosmos DB connection details
- User secrets or environment variables configured with:
  - `COSMOSDB_ENDPOINT_URI` - Cosmos DB account endpoint
  - `COSMOSDB_PRIMARY_KEY` - Cosmos DB primary key
  - `COSMOSDB_DATABASE_ID` - Database name (e.g., "OnePageAuthor")

## Configuration

### Using User Secrets (Recommended for Development)

```bash
cd DomainRegistrationTestHarness
dotnet user-secrets init
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY" "your-primary-key=="
dotnet user-secrets set "COSMOSDB_DATABASE_ID" "OnePageAuthor"
```

### Using Environment Variables

```bash
export COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/"
export COSMOSDB_PRIMARY_KEY="your-primary-key=="
export COSMOSDB_DATABASE_ID="OnePageAuthor"
```

## Usage

### Build

```bash
cd DomainRegistrationTestHarness
dotnet build
```

### Run with Default Data Files

Place JSON files in the `data/` folder with the naming pattern `domain-registrations-*.json`:

```bash
dotnet run
```

### Run with Specific JSON File

```bash
dotnet run -- /path/to/your-registrations.json
```

## JSON File Format

The JSON file should contain an array of domain registration objects:

```json
[
  {
    "upn": "user@example.com",
    "domain": {
      "topLevelDomain": "com",
      "secondLevelDomain": "mydomain"
    },
    "contactInformation": {
      "firstName": "John",
      "lastName": "Doe",
      "address": "123 Main Street",
      "address2": "Suite 100",
      "city": "Austin",
      "state": "TX",
      "country": "United States",
      "zipCode": "78701",
      "emailAddress": "john@example.com",
      "telephoneNumber": "+1-555-123-4567"
    },
    "status": 0
  }
]
```

### Field Descriptions

| Field | Required | Description |
|-------|----------|-------------|
| `upn` | Yes | User Principal Name (partition key) |
| `id` | No | Document ID (auto-generated if not provided) |
| `domain.topLevelDomain` | Yes | TLD (e.g., "com", "org", "net") |
| `domain.secondLevelDomain` | Yes | Second-level domain name |
| `contactInformation` | Yes | Contact details for registration |
| `status` | No | Registration status (0=Pending, 1=InProgress, 2=Completed, 3=Failed, 4=Cancelled) |
| `createdAt` | No | Auto-populated with current UTC time |

### Status Values

- `0` - Pending (triggers processing)
- `1` - InProgress
- `2` - Completed
- `3` - Failed
- `4` - Cancelled

## Sample Data

A sample data file is included at `data/domain-registrations-sample.json`. You can use this as a template for your test data.

## Related Components

- **DomainRegistrationTriggerFunction** (`InkStainedWretchFunctions/DomainRegistrationTriggerFunction.cs`) - The Cosmos DB triggered function that processes domain registrations
- **DomainRegistrationRepository** (`OnePageAuthorLib/nosql/DomainRegistrationRepository.cs`) - Repository for domain registration CRUD operations
- **DomainRegistration** (`OnePageAuthorLib/entities/DomainRegistration.cs`) - Entity model for domain registrations

## Troubleshooting

### Configuration Errors

If you see "COSMOSDB_ENDPOINT_URI is required", ensure your user secrets or environment variables are properly configured.

### Connection Errors

Verify that:

1. The Cosmos DB endpoint is accessible
2. The primary key is correct
3. The database exists
4. Your network allows connections to Azure Cosmos DB

### JSON Parsing Errors

Ensure your JSON file:

1. Is valid JSON (use a JSON validator)
2. Contains an array of objects
3. Has required fields (upn, domain.topLevelDomain, domain.secondLevelDomain)
