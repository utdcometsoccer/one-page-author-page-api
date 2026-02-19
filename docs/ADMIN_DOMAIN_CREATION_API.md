# Admin Domain Creation API — JavaScript / TypeScript Client Guide

> **Audience:** Frontend developers building admin dashboards or tooling in JavaScript or TypeScript.

---

## Overview

The **Admin Domain Creation** endpoint lets users with the `Admin` role complete the full domain-provisioning workflow for a partially registered author site **without requiring a Stripe subscription**. It executes:

1. WHMCS domain registration
2. Azure DNS zone creation and name-server retrieval
3. WHMCS name-server update
4. Azure Front Door custom-domain setup

---

## Endpoint

```
POST /api/admin/domain-registrations/{registrationId}/complete
```

| Detail | Value |
|--------|-------|
| Method | `POST` |
| Auth | Bearer JWT with `Admin` role claim |
| Path param | `registrationId` — the Cosmos DB document ID of the domain registration |
| Request body | *(none required)* |

---

## Authorization

The caller's JWT **must** include the `Admin` value in the `roles` claim issued by Microsoft Entra ID.

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

## Response Codes

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
