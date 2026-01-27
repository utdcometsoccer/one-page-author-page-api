# A/B Testing Configuration API Documentation

## Overview

The A/B Testing Configuration API enables frontend applications to retrieve experiment assignments with consistent variant bucketing. This allows running multiple concurrent A/B tests across different pages with stable user experiences.

## Key Features

- **Consistent Bucketing**: Users/sessions always receive the same variants using deterministic hashing
- **Multiple Concurrent Experiments**: Support for running multiple experiments on the same page
- **Page-Based Experiments**: Experiments are scoped to specific pages (e.g., landing, pricing)
- **Traffic Control**: Configure traffic allocation percentages for each variant
- **Session Tracking**: Generate session IDs for anonymous users or use provided user IDs

## API Endpoint

### GET /api/experiments

Retrieves experiment assignments for a user/session on a specific page.

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `page` | string | Yes | Page identifier (e.g., 'landing', 'pricing', 'signup') |
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

## Consistent Bucketing

The API uses SHA256 hashing to ensure consistent variant assignment:

1. A bucketing key is created: `experimentId + ":" + (userId || sessionId)`
2. SHA256 hash is computed from the bucketing key
3. Hash is converted to a number between 0-99 (percentage)
4. Variant is selected based on traffic allocation ranges

### Example

For an experiment with 50/50 traffic split:

- Control: 0-49 (50%)
- Variant A: 50-99 (50%)

A user with hash value 23 → Control
A user with hash value 67 → Variant A

**Key Property**: Same `userId` + `experimentId` always produces the same hash, ensuring consistent variant assignment.

## Frontend Integration

### React Example

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

  // Helper to get config for a specific experiment
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

  // Get button color from experiment
  const buttonConfig = getExperimentConfig('hero-button-color-test');
  const buttonColor = buttonConfig?.buttonColor || '#007bff';
  const buttonText = buttonConfig?.buttonText || 'Get Started';

  // Get headline from experiment
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

### Vue.js Example

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

## Analytics Integration

### Tracking Experiment Exposure

When an experiment is shown to a user, send an event to your analytics system:

```typescript
function trackExperimentExposure(experiment: Experiment, sessionId: string) {
  // Google Analytics example
  gtag('event', 'experiment_exposure', {
    experiment_id: experiment.id,
    experiment_name: experiment.name,
    variant: experiment.variant,
    session_id: sessionId,
    page: window.location.pathname
  });

  // Custom analytics example
  analytics.track('Experiment Exposure', {
    experimentId: experiment.id,
    experimentName: experiment.name,
    variant: experiment.variant,
    sessionId: sessionId,
    timestamp: new Date().toISOString()
  });
}
```

### Tracking Conversions

Track when users complete key actions:

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

## Best Practices

### 1. Store Session ID

Store the returned `sessionId` in local storage or session storage for anonymous users:

```typescript
const { sessionId } = await fetchExperiments('landing');
localStorage.setItem('experimentSessionId', sessionId);
```

### 2. Use User ID When Available

Always pass the `userId` parameter for authenticated users to ensure consistent experiences across devices:

```typescript
const userId = getCurrentUser()?.id;
const experiments = await fetchExperiments('landing', userId);
```

### 3. Handle Loading States

Show appropriate loading states while fetching experiments to avoid layout shifts:

```typescript
if (loading) {
  return <DefaultLayout />; // Show default without experimental changes
}
```

### 4. Provide Fallbacks

Always provide fallback values in case experiments fail to load:

```typescript
const buttonColor = getConfig('button-test')?.color || '#007bff';
```

### 5. Track Exposures Early

Track experiment exposures as soon as the variant is shown, not just on user interaction:

```typescript
useEffect(() => {
  if (experiments.length > 0) {
    experiments.forEach(exp => trackExperimentExposure(exp, sessionId));
  }
}, [experiments]);
```

## Creating New Experiments

Experiments are stored in Cosmos DB. To create a new experiment:

```csharp
var experiment = new Experiment
{
    id = "unique-experiment-id",
    Name = "Descriptive Experiment Name",
    Page = "landing", // or "pricing", "signup", etc.
    IsActive = true,
    Variants = new List<ExperimentVariant>
    {
        new ExperimentVariant
        {
            Id = "control",
            Name = "Control Group",
            TrafficPercentage = 50,
            Config = new Dictionary<string, object>
            {
                { "featureEnabled", false }
            }
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

### Traffic Allocation Rules

- Traffic percentages must sum to 100
- Minimum percentage: 1
- Maximum percentage: 100
- Use whole numbers only

## Rate Limiting

Currently, there is no rate limiting on the experiments endpoint. Consider implementing caching on the frontend to reduce API calls:

```typescript
const CACHE_DURATION = 60 * 60 * 1000; // 1 hour

function getCachedExperiments(page: string) {
  const cached = localStorage.getItem(`experiments_${page}`);
  if (!cached) return null;
  
  const { data, timestamp } = JSON.parse(cached);
  if (Date.now() - timestamp > CACHE_DURATION) return null;
  
  return data;
}

function setCachedExperiments(page: string, data: any) {
  localStorage.setItem(`experiments_${page}`, JSON.stringify({
    data,
    timestamp: Date.now()
  }));
}
```

## Support

For issues or questions:

- Check the README in `/SeedExperiments` for seeding sample data
- Review test files in `/OnePageAuthor.Test/Services/ExperimentServiceTests.cs`
- Contact the development team

## Version History

- **v1.0.0** (2024-01-20): Initial release with consistent bucketing and multi-experiment support
