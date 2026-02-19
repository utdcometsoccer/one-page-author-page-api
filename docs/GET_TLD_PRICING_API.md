# Get TLD Pricing API Documentation

## Overview

The Get TLD Pricing API retrieves domain name pricing information from the WHMCS backend, including registration, renewal, and transfer costs for all available top-level domains (TLDs). This is useful for displaying domain pricing to users before they register a domain.

## Endpoint

### GET /api/whmcs/tld-pricing

Retrieves TLD pricing for all available domain extensions.

#### Authorization

This endpoint requires a valid JWT Bearer token issued by Microsoft Entra ID.

```
Authorization: Bearer <your-jwt-token>
```

#### Query Parameters

| Parameter    | Type    | Required | Description                                        |
|--------------|---------|----------|----------------------------------------------------|
| `clientId`   | string  | No       | WHMCS client ID to retrieve client-specific pricing |
| `currencyId` | integer | No       | Currency ID for pricing conversion                 |

#### Response Format

**TypeScript Interface:**

```typescript
interface TLDPricing {
  result: string;  // "success" when the request succeeds
  pricing: {
    [tld: string]: {
      registration: { [years: string]: number };
      renewal: { [years: string]: number };
      transfer: { [years: string]: number };
    };
  };
}
```

#### Response Examples

**200 OK – Success:**

```json
{
  "result": "success",
  "pricing": {
    "com": {
      "registration": { "1": 8.95, "2": 17.90 },
      "renewal": { "1": 9.95, "2": 19.90 },
      "transfer": { "1": 8.95 }
    },
    "net": {
      "registration": { "1": 9.95 },
      "renewal": { "1": 10.95 },
      "transfer": { "1": 9.95 }
    },
    "org": {
      "registration": { "1": 10.95 },
      "renewal": { "1": 11.95 },
      "transfer": { "1": 10.95 }
    }
  }
}
```

**401 Unauthorized:**

```json
{
  "error": "User profile validation failed"
}
```

**502 Bad Gateway:**

```json
{
  "error": "WHMCS service is not configured or returned an error message"
}
```

**500 Internal Server Error:**

```json
{
  "error": "An unexpected error occurred"
}
```

---

## HTTP Status Codes

| Status Code | Description                                                    |
|-------------|----------------------------------------------------------------|
| `200 OK`    | Request succeeded; pricing data returned                       |
| `401 Unauthorized` | Invalid or missing JWT token, or user profile validation failed |
| `500 Internal Server Error` | Unexpected server-side error                    |
| `502 Bad Gateway` | WHMCS API error, configuration issue, or HTTP request failure |

---

## JavaScript / TypeScript Integration Guide

### TypeScript Client Function

```typescript
interface TLDPricing {
  result: string;
  pricing: {
    [tld: string]: {
      registration: { [years: string]: number };
      renewal: { [years: string]: number };
      transfer: { [years: string]: number };
    };
  };
}

interface GetTLDPricingOptions {
  clientId?: string;
  currencyId?: number;
}

const getTLDPricing = async (
  token: string,
  options: GetTLDPricingOptions = {}
): Promise<TLDPricing> => {
  const params = new URLSearchParams();
  if (options.clientId) params.append('clientId', options.clientId);
  if (options.currencyId !== undefined) params.append('currencyId', options.currencyId.toString());

  const queryString = params.toString();
  const url = `/api/whmcs/tld-pricing${queryString ? `?${queryString}` : ''}`;

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Unauthorized – invalid or missing token');
    }
    if (response.status === 502) {
      throw new Error('Domain pricing service unavailable');
    }
    throw new Error(`Failed to retrieve TLD pricing (HTTP ${response.status})`);
  }

  return response.json() as Promise<TLDPricing>;
};
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';

interface TLDPricing {
  result: string;
  pricing: {
    [tld: string]: {
      registration: { [years: string]: number };
      renewal: { [years: string]: number };
      transfer: { [years: string]: number };
    };
  };
}

function useTLDPricing(token: string, currencyId?: number) {
  const [pricing, setPricing] = useState<TLDPricing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchPricing = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await getTLDPricing(token, { currencyId });
        setPricing(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    if (token) {
      fetchPricing();
    }
  }, [token, currencyId]);

  return { pricing, loading, error };
}
```

### React Component Example

```tsx
const DomainPricingTable: React.FC<{ token: string }> = ({ token }) => {
  const { pricing, loading, error } = useTLDPricing(token);

  if (loading) return <p>Loading domain pricing...</p>;
  if (error) return <p>Error: {error}</p>;
  if (!pricing) return null;

  return (
    <table>
      <thead>
        <tr>
          <th>Extension</th>
          <th>Registration (1 yr)</th>
          <th>Renewal (1 yr)</th>
          <th>Transfer</th>
        </tr>
      </thead>
      <tbody>
        {Object.entries(pricing.pricing).map(([tld, prices]) => (
          <tr key={tld}>
            <td>.{tld}</td>
            <td>${prices.registration['1']?.toFixed(2) ?? '—'}</td>
            <td>${prices.renewal['1']?.toFixed(2) ?? '—'}</td>
            <td>${prices.transfer['1']?.toFixed(2) ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
```

### Plain JavaScript Example

```javascript
async function getTLDPricing(token, options = {}) {
  const params = new URLSearchParams();
  if (options.clientId) params.append('clientId', options.clientId);
  if (options.currencyId !== undefined) params.append('currencyId', String(options.currencyId));

  const queryString = params.toString();
  const url = `/api/whmcs/tld-pricing${queryString ? `?${queryString}` : ''}`;

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Failed to retrieve TLD pricing (HTTP ${response.status})`);
  }

  return response.json();
}

// Usage
getTLDPricing(userToken)
  .then((data) => {
    const comPrices = data.pricing['com'];
    if (comPrices) {
      console.log('.com registration (1 yr):', comPrices.registration['1']);
      console.log('.com renewal (1 yr):', comPrices.renewal['1']);
    }
  })
  .catch((err) => console.error('Error:', err.message));
```

---

## cURL Example

```bash
curl -X GET "https://<your-function-app>.azurewebsites.net/api/whmcs/tld-pricing" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json"
```

With optional query parameters:

```bash
curl -X GET "https://<your-function-app>.azurewebsites.net/api/whmcs/tld-pricing?currencyId=1" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -H "Content-Type: application/json"
```

---

## Configuration

The endpoint relies on WHMCS API credentials configured as environment variables. Contact your system administrator if the endpoint returns `502 Bad Gateway`.

| Environment Variable   | Description                                      |
|------------------------|--------------------------------------------------|
| `WHMCS_API_URL`        | WHMCS API endpoint URL                           |
| `WHMCS_API_IDENTIFIER` | WHMCS API authentication identifier              |
| `WHMCS_API_SECRET`     | WHMCS API authentication secret                  |

See [WHMCS_INTEGRATION_SUMMARY](WHMCS_INTEGRATION_SUMMARY.md) for full configuration and setup instructions.

---

## Troubleshooting

### 401 Unauthorized

- Ensure the `Authorization` header includes a valid `Bearer` token.
- Verify the token has not expired. Tokens issued by Microsoft Entra ID are time-limited.
- Confirm the user account has a valid profile in the system.
- See [JWT_INVALID_TOKEN_TROUBLESHOOTING](authentication/JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for detailed guidance.

### 502 Bad Gateway

- The WHMCS service may not be configured. Verify `WHMCS_API_URL`, `WHMCS_API_IDENTIFIER`, and `WHMCS_API_SECRET` environment variables are set.
- The WHMCS API may be temporarily unavailable. Retry the request after a short delay.
- Check Application Insights for detailed error logs.

### Empty or Missing TLDs

- WHMCS only returns pricing for TLDs supported by the configured registrar modules.
- Contact your WHMCS administrator to add support for additional TLDs.

---

## Related Documentation

- [WHMCS_INTEGRATION_SUMMARY](WHMCS_INTEGRATION_SUMMARY.md) – WHMCS integration architecture and configuration
- [API-Documentation](API-Documentation.md) – Complete API reference
- [Authentication README](authentication/README.md) – Authentication setup and JWT token guide
- [JWT_INVALID_TOKEN_TROUBLESHOOTING](authentication/JWT_INVALID_TOKEN_TROUBLESHOOTING.md) – Token troubleshooting
