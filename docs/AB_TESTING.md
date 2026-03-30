# A/B Testing

> This document consolidates `AB_TESTING_API.md`, `AB_TESTING_IMPLEMENTATION_SUMMARY.md`, and `AB_TESTING_QUICK_REFERENCE.md` into a single reference.

## Overview

The A/B Testing Configuration API enables frontend applications to retrieve experiment assignments with consistent variant bucketing. This allows running multiple concurrent A/B tests across different pages with stable user experiences.

## Key Features

- **Consistent Bucketing**: Users/sessions always receive the same variants using deterministic hashing
- **Multiple Concurrent Experiments**: Support for running multiple experiments on the same page
- **Page-Based Experiments**: Experiments are scoped to specific pages (e.g., landing, pricing)
- **Traffic Control**: Configure traffic allocation percentages for each variant
- **Session Tracking**: Generate session IDs for anonymous users or use provided user IDs

---

## Quick Reference

### Endpoint

```
GET /api/experiments?page={page}&userId={userId}
```

### Parameters

- **page** (required): Page identifier (e.g., `"landing"`, `"pricing"`)
- **userId** (optional): User ID for consistent bucketing

### Response

```json
{
  "experiments": [
    {
      "id": "string",
      "name": "string",
      "variant": "string",
      "config": { }
    }
  ],
  "sessionId": "string"
}
```

### Quick Start

#### 1. Seed Sample Data

```bash
cd SeedExperiments
export COSMOSDB_ENDPOINT_URI="your-endpoint"
export COSMOSDB_PRIMARY_KEY="your-key"
dotnet run
```

#### 2. Test API

```bash
# Anonymous user
curl "http://localhost:7071/api/experiments?page=landing"

# Authenticated user
curl "http://localhost:7071/api/experiments?page=pricing&userId=user-123"
```

#### 3. Frontend Integration

```typescript
const response = await fetch(`/api/experiments?page=landing&userId=${userId}`);
const { experiments, sessionId } = await response.json();

const config = experiments.find(e => e.id === 'my-experiment-id')?.config;
```

### Test Coverage

23/23 tests passing ✅

- `ExperimentServiceTests`: 14 tests
- `GetExperimentsTests`: 9 tests

---

## Full API Reference

### GET /api/experiments

Retrieves experiment assignments for a user/session on a specific page.

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `page` | string | Yes | Page identifier (e.g., `'landing'`, `'pricing'`, `'signup'`) |
| `userId` | string | No | User ID for consistent bucketing across sessions. If not provided, a session ID will be generated. |

#### Request Examples

```bash
# Get experiments for landing page (anonymous user)
GET /api/experiments?page=landing

# Get experiments for pricing page with user ID
GET /api/experiments?page=pricing&userId=user-12345

# Get experiments for signup page
GET /api/experiments?page=signup&userId=authenticated-user-789
```

#### Response Format

```typescript
interface GetExperimentsResponse {
  experiments: AssignedExperiment[];
  sessionId: string;
}

interface AssignedExperiment {
  id: string;                      // Experiment identifier
  name: string;                    // Human-readable experiment name
  variant: string;                 // Assigned variant ID (e.g., 'control', 'variant_a')
  config: Record<string, any>;     // Variant-specific configuration
}
```

#### Response Examples

**Landing Page Response (Anonymous User):**

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
    },
    {
      "id": "hero-headline-test",
      "name": "Hero Headline Test",
      "variant": "control",
      "config": {
        "headline": "Create Your Author Page",
        "subheadline": "Share your stories with the world"
      }
    }
  ],
  "sessionId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

**Pricing Page Response (Authenticated User):**

```json
{
  "experiments": [
    {
      "id": "pricing-card-design-test",
      "name": "Pricing Card Design Test",
      "variant": "variant_a",
      "config": {
        "cardStyle": "modern",
        "showBadge": true,
        "highlightColor": "#28a745",
        "badgeText": "Most Popular"
      }
    },
    {
      "id": "pricing-cta-button-test",
      "name": "Pricing CTA Button Text Test",
      "variant": "variant_b",
      "config": {
        "buttonText": "Get Started Today"
      }
    }
  ],
  "sessionId": "user-12345"
}
```

#### Status Codes

| Code | Description |
|------|-------------|
| 200 | Success - Returns experiment assignments |
| 400 | Bad Request - Missing or invalid `page` parameter |
| 500 | Internal Server Error - Server-side error occurred |

#### Error Response Format

```json
{
  "error": "Missing required parameter: 'page'",
  "message": "Please provide a 'page' query parameter (e.g., 'landing', 'pricing')"
}
```

### Consistent Bucketing

The API uses SHA256 hashing to ensure consistent variant assignment:

1. A bucketing key is created: `experimentId + ":" + (userId || sessionId)`
2. SHA256 hash is computed from the bucketing key
3. Hash is converted to a number between 0–99 (percentage)
4. Variant is selected based on traffic allocation ranges

**Example** — for a 50/50 traffic split:

- Control: 0–49 (50%)
- Variant A: 50–99 (50%)

A user with hash value 23 → Control  
A user with hash value 67 → Variant A

**Key Property**: Same `userId` + `experimentId` always produces the same hash, ensuring consistent variant assignment.

### Frontend Integration

#### React Example

```typescript
import { useEffect, useState } from 'react';

interface ExperimentConfig {
  [key: string]: any;
}

interface Experiment {
  id: string;
  name: string;
  variant: string;
  config: ExperimentConfig;
}

function useExperiments(page: string, userId?: string) {
  const [experiments, setExperiments] = useState<Experiment[]>([]);
  const [sessionId, setSessionId] = useState<string>('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchExperiments = async () => {
      try {
        const params = new URLSearchParams({ page });
        if (userId) params.append('userId', userId);

        const response = await fetch(`/api/experiments?${params}`);
        const data = await response.json();

        setExperiments(data.experiments);
        setSessionId(data.sessionId);
      } catch (error) {
        console.error('Failed to fetch experiments:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchExperiments();
  }, [page, userId]);

  const getExperimentConfig = (experimentId: string): ExperimentConfig | null => {
    const experiment = experiments.find(e => e.id === experimentId);
    return experiment?.config || null;
  };

  return { experiments, sessionId, loading, getExperimentConfig };
}

// Usage in a component
function LandingPage() {
  const { experiments, getExperimentConfig, loading } = useExperiments('landing');

  if (loading) return <div>Loading...</div>;

  const buttonConfig = getExperimentConfig('hero-button-color-test');
  const buttonColor = buttonConfig?.buttonColor || '#007bff';
  const buttonText = buttonConfig?.buttonText || 'Get Started';

  const headlineConfig = getExperimentConfig('hero-headline-test');
  const headline = headlineConfig?.headline || 'Default Headline';

  return (
    <div>
      <h1>{headline}</h1>
      <button style={{ backgroundColor: buttonColor }}>
        {buttonText}
      </button>
    </div>
  );
}
```

#### Vue.js Example

```vue
<template>
  <div>
    <h1>{{ headline }}</h1>
    <button :style="{ backgroundColor: buttonColor }">
      {{ buttonText }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';

const props = defineProps<{
  page: string;
  userId?: string;
}>();

const experiments = ref([]);
const sessionId = ref('');

onMounted(async () => {
  const params = new URLSearchParams({ page: props.page });
  if (props.userId) params.append('userId', props.userId);

  const response = await fetch(`/api/experiments?${params}`);
  const data = await response.json();

  experiments.value = data.experiments;
  sessionId.value = data.sessionId;
});

const getConfig = (experimentId: string) => {
  const exp = experiments.value.find(e => e.id === experimentId);
  return exp?.config || {};
};

const buttonColor = computed(() =>
  getConfig('hero-button-color-test').buttonColor || '#007bff'
);

const buttonText = computed(() =>
  getConfig('hero-button-color-test').buttonText || 'Get Started'
);

const headline = computed(() =>
  getConfig('hero-headline-test').headline || 'Default Headline'
);
</script>
```

### Analytics Integration

#### Tracking Experiment Exposure

```typescript
function trackExperimentExposure(experiment: Experiment, sessionId: string) {
  gtag('event', 'experiment_exposure', {
    experiment_id: experiment.id,
    experiment_name: experiment.name,
    variant: experiment.variant,
    session_id: sessionId,
    page: window.location.pathname
  });
}
```

#### Tracking Conversions

```typescript
function trackConversion(experimentId: string, variant: string, action: string) {
  analytics.track('Experiment Conversion', {
    experimentId,
    variant,
    action, // e.g., 'signup', 'purchase', 'click_cta'
    timestamp: new Date().toISOString()
  });
}
```

### Best Practices

1. **Store Session ID** in local storage for anonymous users:
   ```typescript
   const { sessionId } = await fetchExperiments('landing');
   localStorage.setItem('experimentSessionId', sessionId);
   ```
2. **Use User ID When Available** for authenticated users to ensure consistent cross-device experiences.
3. **Handle Loading States** to avoid layout shifts.
4. **Provide Fallbacks** in case experiments fail to load:
   ```typescript
   const buttonColor = getConfig('button-test')?.color || '#007bff';
   ```
5. **Track Exposures Early** — as soon as the variant is shown, not just on user interaction.

### Creating New Experiments

Experiments are stored in Cosmos DB. To create a new experiment programmatically:

```csharp
var experiment = new Experiment
{
    id = "unique-experiment-id",
    Name = "Descriptive Experiment Name",
    Page = "landing",
    IsActive = true,
    Variants = new List<ExperimentVariant>
    {
        new ExperimentVariant
        {
            Id = "control",
            Name = "Control Group",
            TrafficPercentage = 50,
            Config = new Dictionary<string, object> { { "featureEnabled", false } }
        },
        new ExperimentVariant
        {
            Id = "variant_a",
            Name = "Treatment Group",
            TrafficPercentage = 50,
            Config = new Dictionary<string, object>
            {
                { "featureEnabled", true },
                { "featureColor", "blue" }
            }
        }
    }
};

await experimentRepository.CreateAsync(experiment);
```

**Traffic Allocation Rules**: Percentages must sum to 100; use whole numbers only.

### Rate Limiting

Currently there is no rate limiting on the experiments endpoint. Consider caching on the frontend:

```typescript
const CACHE_DURATION = 60 * 60 * 1000; // 1 hour

function getCachedExperiments(page: string) {
  const cached = localStorage.getItem(`experiments_${page}`);
  if (!cached) return null;
  const { data, timestamp } = JSON.parse(cached);
  if (Date.now() - timestamp > CACHE_DURATION) return null;
  return data;
}
```

---

## Implementation Details

### Architecture Overview

The implementation follows the standard repository/service pattern used across the codebase:

| Layer | File | Responsibility |
|-------|------|----------------|
| Entity | `OnePageAuthorLib/entities/Experiment.cs` | Data models |
| Container | `OnePageAuthorLib/nosql/ExperimentsContainerManager.cs` | Cosmos DB container (`/Page` partition key, 400 RU/s) |
| Repository | `OnePageAuthorLib/nosql/ExperimentRepository.cs` | CRUD operations |
| Repository Interface | `OnePageAuthorLib/interfaces/IExperimentRepository.cs` | Contract |
| Service | `OnePageAuthorLib/services/ExperimentService.cs` | Bucketing logic |
| Service Interface | `OnePageAuthorLib/interfaces/IExperimentService.cs` | Contract |
| API Endpoint | `InkStainedWretchFunctions/GetExperiments.cs` | HTTP function |
| DI Registration | `OnePageAuthorLib/ServiceFactory.cs` | `AddExperimentRepository()`, `AddExperimentServices()` |

### Bucketing Algorithm

1. **Create Bucketing Key**: `experimentId + ":" + (userId || sessionId)`
2. **Compute Hash**: SHA256 hash of bucketing key
3. **Convert to Percentage**: First 4 bytes of hash → integer → modulo 100
4. **Assign Variant**: Map to variant based on cumulative traffic percentages

### Testing

Run experiment-specific tests:

```bash
# Test experiment service
dotnet test --filter "FullyQualifiedName~ExperimentServiceTests"

# Test API function
dotnet test --filter "FullyQualifiedName~GetExperimentsTests"

# Test all experiment-related code
dotnet test --filter "FullyQualifiedName~Experiment"
```

### Sample Data Seeder

`SeedExperiments/` contains a console application with 4 example experiments:

1. **Landing Page - Hero Button Color Test** (50/50 split)
2. **Landing Page - Hero Headline Test** (33/33/34 split)
3. **Pricing Page - Card Design Test** (50/50 split)
4. **Pricing Page - CTA Button Text Test** (40/30/30 split)

```bash
cd SeedExperiments
export COSMOSDB_ENDPOINT_URI="your-endpoint"
export COSMOSDB_PRIMARY_KEY="your-key"
export COSMOSDB_DATABASE_ID="OnePageAuthor"
dotnet run
```

### Deployment Checklist

1. Ensure the "Experiments" container is created (seeder handles this automatically; partition key `/Page`)
2. Deploy `InkStainedWretchFunctions` with the `GetExperiments` endpoint
3. Run `SeedExperiments` to create initial experiments
4. Integrate frontend to call `/api/experiments?page=<page>` on page load
5. Track experiment exposures via analytics

## Version History

- **v1.0.0** (2024-01-20): Initial release with consistent bucketing and multi-experiment support
