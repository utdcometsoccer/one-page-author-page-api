# A/B Testing API - Quick Reference

## Endpoint
```
GET /api/experiments?page={page}&userId={userId}
```

## Parameters
- **page** (required): Page identifier (e.g., "landing", "pricing")
- **userId** (optional): User ID for consistent bucketing

## Response
```json
{
  "experiments": [
    {
      "id": "string",
      "name": "string", 
      "variant": "string",
      "config": { /* variant-specific config */ }
    }
  ],
  "sessionId": "string"
}
```

## Quick Start

### 1. Seed Sample Data
```bash
cd SeedExperiments
export COSMOSDB_ENDPOINT_URI="your-endpoint"
export COSMOSDB_PRIMARY_KEY="your-key"
dotnet run
```

### 2. Test API
```bash
# Anonymous user
curl "http://localhost:7071/api/experiments?page=landing"

# Authenticated user
curl "http://localhost:7071/api/experiments?page=pricing&userId=user-123"
```

### 3. Frontend Integration
```typescript
const response = await fetch(`/api/experiments?page=landing&userId=${userId}`);
const { experiments, sessionId } = await response.json();

const config = experiments.find(e => e.id === 'my-experiment-id')?.config;
```

## Key Features
- ✅ Consistent bucketing (same user = same variant)
- ✅ Multiple experiments per page
- ✅ Traffic percentage control
- ✅ Session tracking for analytics

## Documentation
- Full API Docs: `docs/AB_TESTING_API.md`
- Implementation: `AB_TESTING_IMPLEMENTATION_SUMMARY.md`
- Seeder Guide: `SeedExperiments/README.md`

## Test Coverage
23/23 tests passing ✅
- ExperimentServiceTests: 14 tests
- GetExperimentsTests: 9 tests
