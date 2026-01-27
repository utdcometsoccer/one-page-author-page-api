# A/B Testing Configuration API - Implementation Summary

## Overview

This implementation provides a complete A/B testing configuration API for the OnePageAuthor platform, enabling frontend applications to retrieve experiment assignments with consistent variant bucketing.

## Implemented Components

### 1. Entity Models (`OnePageAuthorLib/entities/Experiment.cs`)

- **Experiment**: Main experiment entity with id, name, page, active status, and variants
- **ExperimentVariant**: Variant configuration with traffic percentage and config dictionary
- **GetExperimentsRequest**: Request model with optional userId and required page
- **GetExperimentsResponse**: Response model with assigned experiments and sessionId
- **AssignedExperiment**: Simplified experiment assignment for API responses

### 2. Data Layer

#### Container Manager (`OnePageAuthorLib/nosql/ExperimentsContainerManager.cs`)

- Creates and manages the "Experiments" container in Cosmos DB
- Partition key: `/Page` for efficient queries by page
- Initial throughput: 400 RU/s

#### Repository Interface (`OnePageAuthorLib/interfaces/IExperimentRepository.cs`)

- `GetActiveExperimentsByPageAsync(string page)` - Get active experiments for a page
- `GetByIdAsync(string id, string page)` - Get a specific experiment
- `CreateAsync(Experiment experiment)` - Create a new experiment
- `UpdateAsync(Experiment experiment)` - Update an existing experiment
- `DeleteAsync(string id, string page)` - Delete an experiment

#### Repository Implementation (`OnePageAuthorLib/nosql/ExperimentRepository.cs`)

- Full CRUD operations with proper error handling
- Logging support for troubleshooting
- Partition key-aware queries for optimal performance

### 3. Business Logic

#### Service Interface (`OnePageAuthorLib/interfaces/IExperimentService.cs`)

- `GetExperimentsAsync(GetExperimentsRequest)` - Get experiment assignments
- `AssignVariant(Experiment, string bucketingKey)` - Deterministic variant assignment

#### Service Implementation (`OnePageAuthorLib/services/ExperimentService.cs`)

- **Consistent Bucketing**: Uses SHA256 hashing for deterministic variant assignment
- **Traffic Allocation**: Respects traffic percentage settings for each variant
- **Session Management**: Generates session IDs for anonymous users
- **Validation**: Validates traffic percentages and variant configurations

### 4. API Endpoint (`InkStainedWretchFunctions/GetExperiments.cs`)

- **Route**: `GET /api/experiments`
- **Query Parameters**: `page` (required), `userId` (optional)
- **Authorization**: Anonymous (no authentication required)
- **Error Handling**: Returns appropriate HTTP status codes (200, 400, 500)
- **Logging**: Comprehensive logging for monitoring and debugging

### 5. Dependency Injection (`OnePageAuthorLib/ServiceFactory.cs`)

- `AddExperimentRepository()` - Registers repository and container manager
- `AddExperimentServices()` - Registers experiment service
- Integrated into `InkStainedWretchFunctions/Program.cs`

### 6. Testing

#### Service Tests (`OnePageAuthor.Test/Services/ExperimentServiceTests.cs`)

- 14 unit tests covering:
  - Constructor validation
  - Input validation
  - Consistent bucketing behavior
  - Traffic allocation accuracy
  - Edge cases (100% traffic, multiple variants, etc.)

#### Function Tests (`OnePageAuthor.Test/InkStainedWretchFunctions/GetExperimentsTests.cs`)

- 9 unit tests covering:
  - Constructor validation
  - Query parameter validation
  - Success scenarios
  - Error handling
  - Multiple experiments support

**All 23 tests passing ✅**

### 7. Sample Data Seeder (`SeedExperiments/`)

- Console application to seed sample experiments
- 4 example experiments:
  1. **Landing Page - Hero Button Color Test** (50/50 split)
  2. **Landing Page - Hero Headline Test** (33/33/34 split)
  3. **Pricing Page - Card Design Test** (50/50 split)
  4. **Pricing Page - CTA Button Text Test** (40/30/30 split)
- Includes README with usage instructions

### 8. Documentation (`docs/AB_TESTING_API.md`)

- Complete API reference
- Request/response examples
- Frontend integration examples (React, Vue.js)
- Analytics integration guidance
- Best practices and recommendations

## Key Features

### ✅ Consistent Bucketing

Users always receive the same variants using SHA256 hashing of `experimentId:bucketingKey`. This ensures:

- Same user sees same variants across sessions
- A/B test results are not biased by variant switching
- User experience remains consistent

### ✅ Multiple Concurrent Experiments

Support for running multiple experiments on the same page:

- Each experiment is independently bucketed
- No cross-contamination between experiments
- Easy to analyze individual experiment impact

### ✅ Traffic Control

Fine-grained control over traffic allocation:

- Percentage-based allocation per variant
- Supports 2-way, 3-way, or N-way splits
- Validation ensures percentages sum to 100

### ✅ Page-Based Experiments

Experiments are scoped to specific pages:

- Landing page experiments
- Pricing page experiments
- Signup page experiments
- Any custom page identifier

### ✅ Session Tracking

Automatic session ID generation:

- Generated for anonymous users
- Uses provided userId for authenticated users
- Enables cross-session tracking

## Bucketing Algorithm

The deterministic bucketing algorithm ensures consistent variant assignment:

1. **Create Bucketing Key**: `experimentId + ":" + (userId || sessionId)`
2. **Compute Hash**: SHA256 hash of bucketing key
3. **Convert to Percentage**: First 4 bytes of hash → integer → modulo 100
4. **Assign Variant**: Map to variant based on cumulative traffic percentages

Example with 50/50 split:

- Hash value 0-49 → Control (50%)
- Hash value 50-99 → Variant A (50%)

## API Usage Examples

### Get Experiments for Landing Page

```bash
GET /api/experiments?page=landing
```

Response:

```json
{
  "experiments": [
    {
      "id": "hero-button-color-test",
      "name": "Hero Button Color Test",
      "variant": "variant_a",
      "config": {
        "buttonColor": "#28a745",
        "buttonText": "Get Started"
      }
    }
  ],
  "sessionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

### Get Experiments with User ID

```bash
GET /api/experiments?page=pricing&userId=user-12345
```

Response:

```json
{
  "experiments": [
    {
      "id": "pricing-card-design-test",
      "name": "Pricing Card Design Test",
      "variant": "control",
      "config": {
        "cardStyle": "traditional",
        "showBadge": false,
        "highlightColor": "#007bff"
      }
    }
  ],
  "sessionId": "user-12345"
}
```

## Frontend Integration Example

```typescript
// Fetch experiments
const response = await fetch(`/api/experiments?page=landing&userId=${userId}`);
const { experiments, sessionId } = await response.json();

// Get specific experiment config
const buttonExperiment = experiments.find(e => e.id === 'hero-button-color-test');
const buttonColor = buttonExperiment?.config?.buttonColor || '#007bff';

// Apply configuration
<button style={{ backgroundColor: buttonColor }}>
  {buttonExperiment?.config?.buttonText || 'Get Started'}
</button>

// Track exposure
analytics.track('Experiment Exposure', {
  experimentId: buttonExperiment.id,
  variant: buttonExperiment.variant,
  sessionId
});
```

## Testing the Implementation

### Run Unit Tests

```bash
# Test experiment service
dotnet test --filter "FullyQualifiedName~ExperimentServiceTests"

# Test API function
dotnet test --filter "FullyQualifiedName~GetExperimentsTests"

# Test all experiment-related code
dotnet test --filter "FullyQualifiedName~Experiment"
```

### Seed Sample Data

```bash
cd SeedExperiments
export COSMOSDB_ENDPOINT_URI="your-endpoint"
export COSMOSDB_PRIMARY_KEY="your-key"
export COSMOSDB_DATABASE_ID="OnePageAuthor"
dotnet run
```

### Test API Endpoint

```bash
# Start the function app
cd InkStainedWretchFunctions
func start

# Call the API
curl "http://localhost:7071/api/experiments?page=landing"
curl "http://localhost:7071/api/experiments?page=pricing&userId=test-user-123"
```

## Files Created/Modified

### New Files (15)

1. `OnePageAuthorLib/entities/Experiment.cs` - Entity models
2. `OnePageAuthorLib/nosql/ExperimentsContainerManager.cs` - Container manager
3. `OnePageAuthorLib/nosql/ExperimentRepository.cs` - Repository implementation
4. `OnePageAuthorLib/interfaces/IExperimentRepository.cs` - Repository interface
5. `OnePageAuthorLib/interfaces/IExperimentService.cs` - Service interface
6. `OnePageAuthorLib/services/ExperimentService.cs` - Service implementation
7. `InkStainedWretchFunctions/GetExperiments.cs` - API endpoint
8. `OnePageAuthor.Test/Services/ExperimentServiceTests.cs` - Service tests
9. `OnePageAuthor.Test/InkStainedWretchFunctions/GetExperimentsTests.cs` - Function tests
10. `SeedExperiments/Program.cs` - Seeder implementation
11. `SeedExperiments/SeedExperiments.csproj` - Seeder project file
12. `SeedExperiments/README.md` - Seeder documentation
13. `docs/AB_TESTING_API.md` - API documentation

### Modified Files (2)

1. `OnePageAuthorLib/ServiceFactory.cs` - Added DI extensions
2. `InkStainedWretchFunctions/Program.cs` - Registered services

## Next Steps

To deploy this feature to production:

1. **Set up Cosmos DB**
   - Ensure the "Experiments" container is created (seeder will do this automatically)
   - Verify partition key is set to `/Page`

2. **Deploy Azure Function**
   - Deploy InkStainedWretchFunctions with the new GetExperiments endpoint
   - Verify endpoint is accessible: `GET /api/experiments?page=landing`

3. **Seed Initial Experiments**
   - Run SeedExperiments to create sample experiments
   - Or create experiments programmatically via repository

4. **Frontend Integration**
   - Add API calls to fetch experiments on page load
   - Apply configurations from experiment variants
   - Track experiment exposures and conversions

5. **Monitor and Analyze**
   - Monitor API performance and error rates
   - Analyze experiment results through analytics platform
   - Iterate on winning variants

## Success Metrics

The implementation meets all requirements from the issue:

✅ **Endpoint**: `GET /api/experiments` with required parameters
✅ **Consistent Variant Assignment**: SHA256-based deterministic bucketing
✅ **Multiple Concurrent Experiments**: Supported per page
✅ **Analytics Integration**: Session ID provided for tracking
✅ **Test Coverage**: 23 unit tests, all passing
✅ **Documentation**: Complete API reference with examples
✅ **Sample Data**: Seeder with 4 realistic experiments

## Conclusion

The A/B Testing Configuration API is production-ready with:

- Robust bucketing algorithm ensuring consistent user experiences
- Comprehensive test coverage validating core functionality
- Detailed documentation for frontend teams
- Sample experiments for quick testing and validation
- Scalable architecture supporting future enhancements

The implementation follows best practices from the codebase:

- Repository pattern for data access
- Service layer for business logic
- Dependency injection for loose coupling
- Comprehensive error handling and logging
- Extensive unit testing
