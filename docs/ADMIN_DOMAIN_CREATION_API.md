# Admin Domain Registration APIs — JavaScript / TypeScript Client Guide

> **Audience:** Frontend developers building admin dashboards or tooling in JavaScript or TypeScript.

---

## Overview

The **Admin Domain Registration** API provides two endpoints for users with the `Admin` role:

1. **`GET /api/admin/domain-registrations`** — lists all incomplete domain registrations (Pending, InProgress, Failed) across all users.
2. **`POST /api/admin/domain-registrations/{registrationId}/complete`** — runs the full domain-provisioning workflow for a specific registration, which includes:
   1. WHMCS domain registration
   2. Azure DNS zone creation and name-server retrieval
   3. WHMCS name-server update
   4. Azure Front Door custom-domain setup

---

## Required Configuration

Both admin endpoints require **Cosmos DB** and **JWT authentication** to work. The complete endpoint additionally requires Azure infrastructure and WHMCS credentials.

### Minimum configuration — GET endpoint

| Variable | Required | Description | How to obtain |
|----------|----------|-------------|---------------|
| `COSMOSDB_ENDPOINT_URI` | ✅ Yes | Cosmos DB account endpoint | Azure Portal → Cosmos DB → Keys → URI |
| `COSMOSDB_PRIMARY_KEY` | ✅ Yes | Cosmos DB primary access key | Azure Portal → Cosmos DB → Keys → Primary Key |
| `COSMOSDB_DATABASE_ID` | ✅ Yes | Database name (e.g. `OnePageAuthorDb`) | Your Cosmos DB database name |
| `CosmosDBConnection` | ✅ Yes | Full connection string for triggers | `AccountEndpoint={URI};AccountKey={KEY};` |
| `AAD_TENANT_ID` | ✅ Yes | Azure AD tenant ID for JWT validation | Azure Portal → Microsoft Entra ID → Overview → Tenant ID |
| `AAD_AUDIENCE` | ✅ Yes | API application client ID | Azure Portal → Microsoft Entra ID → App registrations → Your App → Application (client) ID |

### Additional configuration — POST complete endpoint

The variables below enable the provisioning steps. Each integration degrades gracefully if its variables are absent (the step is skipped, logged, and the endpoint continues).

| Variable | Step enabled | Description | How to obtain |
|----------|-------------|-------------|---------------|
| `AZURE_SUBSCRIPTION_ID` | DNS + Front Door | Azure subscription ID | Azure Portal → Subscriptions → Subscription ID |
| `AZURE_DNS_RESOURCE_GROUP` | DNS zone creation | Resource group for DNS zones | Azure Portal → Resource Groups → name |
| `AZURE_RESOURCE_GROUP_NAME` | Front Door | Resource group that contains the Front Door profile | Azure Portal → Resource Groups → name |
| `AZURE_FRONTDOOR_PROFILE_NAME` | Front Door | Azure Front Door profile name | Azure Portal → Front Door → Profile name |
| `WHMCS_API_URL` | WHMCS registration | WHMCS API endpoint URL | `https://<your-whmcs-host>/includes/api.php` |
| `WHMCS_API_IDENTIFIER` | WHMCS registration | WHMCS API identifier | WHMCS Admin → Setup → API Credentials |
| `WHMCS_API_SECRET` | WHMCS registration | WHMCS API secret | WHMCS Admin → Setup → API Credentials |

> **Important:** `AZURE_SUBSCRIPTION_ID`, `AZURE_DNS_RESOURCE_GROUP`, `AZURE_RESOURCE_GROUP_NAME`, and `AZURE_FRONTDOOR_PROFILE_NAME` must all be present together for their respective steps to run. Providing only some of them results in the step being skipped.

### Azure RBAC permissions

The Azure Functions managed identity (or service principal) requires the following RBAC roles to operate on Azure resources:

| Role | Scope | Required for |
|------|-------|-------------|
| `DNS Zone Contributor` | DNS resource group | Creating and reading DNS zones |
| `CDN Profile Contributor` | Front Door profile | Adding custom domains to Front Door |

Assign these roles with the Azure CLI:

```bash
# DNS Zone Contributor
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "DNS Zone Contributor" \
  --resource-group <AZURE_DNS_RESOURCE_GROUP>

# CDN Profile Contributor (for Front Door)
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "CDN Profile Contributor" \
  --scope /subscriptions/<AZURE_SUBSCRIPTION_ID>/resourceGroups/<AZURE_RESOURCE_GROUP_NAME>/providers/Microsoft.Cdn/profiles/<AZURE_FRONTDOOR_PROFILE_NAME>
```

### Development setup (user secrets)

```bash
cd InkStainedWretchFunctions

# Core — required for the GET endpoint
dotnet user-secrets set "COSMOSDB_ENDPOINT_URI"    "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "COSMOSDB_PRIMARY_KEY"     "your-primary-key=="
dotnet user-secrets set "COSMOSDB_DATABASE_ID"     "OnePageAuthorDb"
dotnet user-secrets set "CosmosDBConnection"       "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-primary-key==;"
dotnet user-secrets set "AAD_TENANT_ID"            "your-tenant-id"
dotnet user-secrets set "AAD_AUDIENCE"             "your-api-client-id"

# Azure infrastructure — required for provisioning steps in POST endpoint
dotnet user-secrets set "AZURE_SUBSCRIPTION_ID"       "your-subscription-id"
dotnet user-secrets set "AZURE_DNS_RESOURCE_GROUP"    "your-dns-resource-group"
dotnet user-secrets set "AZURE_RESOURCE_GROUP_NAME"   "your-frontdoor-resource-group"
dotnet user-secrets set "AZURE_FRONTDOOR_PROFILE_NAME" "your-frontdoor-profile-name"

# WHMCS — required for domain registration in POST endpoint
dotnet user-secrets set "WHMCS_API_URL"        "https://your-whmcs-host/includes/api.php"
dotnet user-secrets set "WHMCS_API_IDENTIFIER" "your-api-identifier"
dotnet user-secrets set "WHMCS_API_SECRET"     "your-api-secret"
```

### Production setup (Azure CLI)

```bash
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <rg-name> \
  --settings \
    COSMOSDB_ENDPOINT_URI="https://your-account.documents.azure.com:443/" \
    COSMOSDB_PRIMARY_KEY="your-primary-key==" \
    COSMOSDB_DATABASE_ID="OnePageAuthorDb" \
    CosmosDBConnection="AccountEndpoint=...;AccountKey=...;" \
    AAD_TENANT_ID="your-tenant-id" \
    AAD_AUDIENCE="your-api-client-id" \
    AZURE_SUBSCRIPTION_ID="your-subscription-id" \
    AZURE_DNS_RESOURCE_GROUP="your-dns-resource-group" \
    AZURE_RESOURCE_GROUP_NAME="your-frontdoor-resource-group" \
    AZURE_FRONTDOOR_PROFILE_NAME="your-frontdoor-profile-name" \
    WHMCS_API_URL="https://your-whmcs-host/includes/api.php" \
    WHMCS_API_IDENTIFIER="your-api-identifier" \
    WHMCS_API_SECRET="your-api-secret"
```

---

## Endpoints

### GET /api/admin/domain-registrations

Lists all incomplete domain registrations (status Pending, InProgress, or Failed) across all users. Contact information is redacted from the response.

| Detail | Value |
|--------|-------|
| Method | `GET` |
| Auth | Bearer JWT with `Admin` role claim |
| Query param | `maxResults` (optional, integer) — caps the number of results returned |
| Request body | *(none)* |

#### Response codes

| Status | Meaning |
|--------|---------|
| `200 OK` | Returns array of registration responses (may be empty) |
| `401 Unauthorized` | Missing or invalid JWT |
| `403 Forbidden` | Authenticated user does not have the `Admin` role |
| `500 Internal Server Error` | Unexpected server error |

#### Response body

```typescript
type DomainRegistrationStatus =
  | 0  // Pending
  | 1  // InProgress
  | 2  // Completed
  | 3  // Failed
  | 4; // Cancelled

interface AdminDomainRegistrationResponse {
  id: string;
  domain: {
    topLevelDomain: string;    // e.g. "com"
    secondLevelDomain: string; // e.g. "mysite"
  };
  contactInformation: null;    // always redacted in admin cross-user listing
  createdAt: string;           // ISO 8601
  lastUpdatedAt: string;       // ISO 8601
  status: DomainRegistrationStatus;
}
```

#### Example

```typescript
async function listIncompleteDomains(
  adminToken: string,
  maxResults?: number,
  baseUrl = ""
): Promise<AdminDomainRegistrationResponse[]> {
  const url = new URL(`${baseUrl}/api/admin/domain-registrations`);
  if (maxResults !== undefined) url.searchParams.set("maxResults", String(maxResults));

  const response = await fetch(url.toString(), {
    headers: { Authorization: `Bearer ${adminToken}` },
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`HTTP ${response.status}: ${body}`);
  }

  return response.json();
}
```

---

### POST /api/admin/domain-registrations/{registrationId}/complete

Completes the full domain-provisioning workflow for a partially registered author site **without requiring a Stripe subscription**.

| Detail | Value |
|--------|-------|
| Method | `POST` |
| Auth | Bearer JWT with `Admin` role claim |
| Path param | `registrationId` — the Cosmos DB document ID of the domain registration |
| Request body | *(none required)* |

## Authorization

Both admin endpoints require a JWT that includes the `Admin` value in the `roles` claim issued by Microsoft Entra ID:

```json
{
  "sub": "...",
  "roles": ["Admin"],
  ...
}
```

Requests without a valid JWT receive **401 Unauthorized**.  
Requests with a valid JWT but no `Admin` role receive **403 Forbidden**.

---

## Response Codes (POST complete)

| Status | Meaning |
|--------|---------|
| `200 OK` | Workflow ran. Check `status` field — `"Completed"` = all steps succeeded, `"InProgress"` = partial success |
| `400 Bad Request` | Missing or empty `registrationId` |
| `401 Unauthorized` | Missing or invalid JWT |
| `403 Forbidden` | Authenticated user does not have the `Admin` role |
| `404 Not Found` | No domain registration with the supplied ID |
| `500 Internal Server Error` | Unexpected server error |

---

## Response Body

```typescript
interface DomainRegistrationResponse {
  id: string;
  domain: {
    topLevelDomain: string;   // e.g. "com"
    secondLevelDomain: string; // e.g. "mysite"
  };
  contactInformation: {
    firstName: string;
    lastName: string;
    address: string;
    address2?: string;
    city: string;
    state: string;
    country: string;
    zipCode: string;
    emailAddress: string;
    telephoneNumber: string;
  };
  createdAt: string;       // ISO 8601
  lastUpdatedAt: string;   // ISO 8601
  status: DomainRegistrationStatus;
}

type DomainRegistrationStatus =
  | "Pending"     // 0 – awaiting processing
  | "InProgress"  // 1 – partially provisioned
  | "Completed"   // 2 – all steps succeeded
  | "Failed"      // 3 – registration failed
  | "Cancelled";  // 4 – registration cancelled
```

> **Note:** The numeric values above match the server-side `DomainRegistrationStatus` enum.
> The API serializes them as integers by default, so check the actual response and adjust comparisons accordingly.

---

## JavaScript / TypeScript Examples

### Minimal fetch call

```typescript
async function adminCompleteDomain(
  registrationId: string,
  adminToken: string,
  baseUrl = ""
): Promise<DomainRegistrationResponse> {
  const url = `${baseUrl}/api/admin/domain-registrations/${encodeURIComponent(registrationId)}/complete`;

  const response = await fetch(url, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${adminToken}`,
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`HTTP ${response.status}: ${body}`);
  }

  return response.json() as Promise<DomainRegistrationResponse>;
}
```

---

### Full example with error handling

```typescript
import { MSAL_CONFIG } from "./authConfig"; // your MSAL config

async function getAdminToken(): Promise<string> {
  // Acquire token silently for the API scope
  const msalInstance = new PublicClientApplication(MSAL_CONFIG);
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) throw new Error("No authenticated account found");

  const result = await msalInstance.acquireTokenSilent({
    scopes: ["api://<YOUR_API_CLIENT_ID>/.default"],
    account: accounts[0],
  });

  return result.accessToken;
}

async function completeDomainCreation(registrationId: string): Promise<void> {
  const token = await getAdminToken();

  const response = await fetch(
    `/api/admin/domain-registrations/${encodeURIComponent(registrationId)}/complete`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      },
    }
  );

  switch (response.status) {
    case 200: {
      const registration: DomainRegistrationResponse = await response.json();
      if (registration.status === 2 /* Completed */) {
        console.log("✅ Domain fully provisioned:", registration.domain);
      } else {
        console.warn(
          "⚠️  Domain partially provisioned (InProgress). Some steps may need retry."
        );
      }
      break;
    }
    case 400:
      console.error("Bad request – check that registrationId is non-empty.");
      break;
    case 401:
      console.error("Unauthorized – obtain a fresh JWT and retry.");
      break;
    case 403:
      console.error("Forbidden – your account does not have the Admin role.");
      break;
    case 404:
      console.error(`Registration ${registrationId} not found.`);
      break;
    default:
      console.error("Unexpected error:", response.status, await response.text());
  }
}
```

---

### React component example

```tsx
import React, { useState } from "react";

interface Props {
  registrationId: string;
  adminToken: string;
}

const CompleteDomainButton: React.FC<Props> = ({ registrationId, adminToken }) => {
  const [status, setStatus] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleComplete = async () => {
    setLoading(true);
    setStatus(null);

    try {
      const response = await fetch(
        `/api/admin/domain-registrations/${encodeURIComponent(registrationId)}/complete`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${adminToken}`,
            "Content-Type": "application/json",
          },
        }
      );

      if (!response.ok) {
        const err = await response.json().catch(() => ({ error: response.statusText }));
        throw new Error(err.error ?? `HTTP ${response.status}`);
      }

      const result: DomainRegistrationResponse = await response.json();
      // Status 2 = Completed, 1 = InProgress
      setStatus(result.status === 2 ? "Completed ✅" : "Partially provisioned ⚠️");
    } catch (err: unknown) {
      setStatus(`Error: ${err instanceof Error ? err.message : String(err)}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <button onClick={handleComplete} disabled={loading}>
        {loading ? "Processing…" : "Complete Domain Creation"}
      </button>
      {status && <p>{status}</p>}
    </div>
  );
};

export default CompleteDomainButton;
```

---

### Listing pending registrations before completing them

Use the standard `GET /api/domain-registrations` endpoint to list registrations, then filter by `status === 0` (Pending) before calling the admin endpoint.

```typescript
interface ListDomainRegistrationsResponse extends Array<DomainRegistrationResponse> {}

async function listPendingRegistrations(
  userToken: string
): Promise<DomainRegistrationResponse[]> {
  const response = await fetch("/api/domain-registrations", {
    headers: { Authorization: `Bearer ${userToken}` },
  });

  if (!response.ok) throw new Error(`HTTP ${response.status}`);

  const all: ListDomainRegistrationsResponse = await response.json();
  return all.filter((r) => r.status === 0 /* Pending */);
}

// Admin workflow: list pending → complete each one
async function processAllPending(adminToken: string): Promise<void> {
  const pending = await listPendingRegistrations(adminToken);
  console.log(`Found ${pending.length} pending registrations`);

  for (const reg of pending) {
    console.log(`Completing domain: ${reg.domain.secondLevelDomain}.${reg.domain.topLevelDomain}`);
    await completeDomainCreation(reg.id);   // function defined above
  }
}
```

---

## Provisioning Workflow Details

When the endpoint is called it executes these steps in order:

```
1. WHMCS RegisterDomain
       ↓ (success only)
2. Azure DNS EnsureDnsZoneExists
       ↓ (success only)
3. Azure DNS GetNameServers
       ↓ (2–5 name servers returned)
4. WHMCS UpdateNameServers
       ↓ (always continues, even if steps 1–4 fail)
5. Azure Front Door AddDomainToFrontDoor
       ↓
6. Cosmos DB UpdateAsync  ← status set to Completed or InProgress
```

A final `status` of `InProgress` means the record was updated but at least one step failed. The front door step always runs regardless of the WHMCS/DNS outcome.

---

## Finding a Registration ID

The registration `id` is the Cosmos DB document ID returned when the domain was first created:

```typescript
// POST /api/domain-registrations  →  DomainRegistrationResponse
// The `id` field on that response is what you pass to the admin endpoint.
const created: DomainRegistrationResponse = await createDomainRegistration(payload, token);
await completeDomainCreation(created.id);
```

You can also look up a registration by calling `GET /api/domain-registrations/{registrationId}`.

---

## Entra ID Setup for the Admin Role

To receive the `Admin` role in the JWT:

1. Open **Microsoft Entra ID** → **App registrations** → select your API app.
2. Go to **App roles** → **Create app role** with value `Admin`.
3. In **Enterprise applications** → **Users and groups** → assign the `Admin` role to the admin user.
4. The `roles` claim will be included in tokens issued for that user.

> See [IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md](IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) for the full guide.

---

## Related Documentation

- [API-Documentation.md](API-Documentation.md) — full API reference
- [authentication/README.md](authentication/README.md) — Entra ID authentication setup
- [IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md](IMPLEMENTATION_SUMMARY_ENTRA_ID_ROLES.md) — role-based authorization
- [WHMCS_INTEGRATION_SUMMARY.md](WHMCS_INTEGRATION_SUMMARY.md) — WHMCS integration details
