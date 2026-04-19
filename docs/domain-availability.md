# Domain Availability API

## Overview

The `CheckDomainAvailability` endpoint lets callers query whether a root domain name is currently registered or available for registration. It uses the **RDAP** (Registration Data Access Protocol) service at `rdap.org` as the authoritative data source.

---

## Endpoint

```
GET /api/domain-availability?domain={domainName}
```

### Authorization

`Anonymous` — no authentication token required.

---

## Query Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `domain`  | **Yes**  | The root domain to check, e.g. `example.com`. Subdomains are not accepted. |

---

## Validation Rules

The following rules are enforced before the RDAP lookup is performed:

| Rule | Detail |
|------|--------|
| Non-empty | The `domain` parameter must not be blank. |
| Root domain only | Exactly two labels separated by a dot (e.g. `example.com`). Subdomains such as `www.example.com` are rejected. |
| .MX second-level domains | Three-label `.mx` domains using a recognized second-level domain — `com.mx`, `net.mx`, `org.mx`, `edu.mx`, or `gob.mx` — are accepted (e.g. `example.com.mx`). Any other three-label `.mx` pattern is rejected. |
| Valid characters | Each label may contain ASCII letters (`a-z`), digits (`0-9`), and hyphens (`-`). No other characters are allowed. |
| No leading/trailing hyphen | A label may not begin or end with a hyphen. |
| Label length | Each label must be 1–63 characters. |
| Total length | The full domain (including the dot separator) must not exceed 253 characters. |
| Valid TLD | The TLD (rightmost label) must be at least two ASCII letters, or a valid punycode label (`xn--…`). Numeric-only TLDs are rejected. |

Violations return **400 Bad Request** with an `ErrorResponse` body.

---

## Response Schema

### Success — `200 OK`

```json
{
  "domain": "example.com",
  "available": false,
  "checkedAt": "2026-04-11T19:39:00Z",
  "rdapStatus": 200,
  "rdapSource": "rdap.org"
}
```

| Field       | Type    | Description |
|-------------|---------|-------------|
| `domain`    | string  | Normalized (lowercase) domain name. |
| `available` | boolean | `true` = not yet registered; `false` = already registered. |
| `checkedAt` | string  | ISO-8601 UTC timestamp of the check. |
| `rdapStatus`| integer | Raw HTTP status code from the RDAP service. |
| `rdapSource`| string  | RDAP host used for the lookup (`rdap.org`). |

---

## Error Schema

```json
{
  "error": "InvalidDomain",
  "message": "The domain format is invalid."
}
```

| Field     | Type   | Description |
|-----------|--------|-------------|
| `error`   | string | Machine-readable error code. |
| `message` | string | Human-readable description. |

### Error Codes

| Code               | HTTP Status | Meaning |
|--------------------|-------------|---------|
| `InvalidDomain`    | `400`       | Missing or malformed `domain` parameter. |
| `RdapLookupFailed` | `502`       | RDAP service returned an unexpected response or timed out. |

---

## Example Requests & Responses

### 1. Registered domain

```
GET /api/domain-availability?domain=example.com
```

```json
{
  "domain": "example.com",
  "available": false,
  "checkedAt": "2026-04-11T19:39:00Z",
  "rdapStatus": 200,
  "rdapSource": "rdap.org"
}
```

### 2. Available domain

```
GET /api/domain-availability?domain=mynewdomain123.com
```

```json
{
  "domain": "mynewdomain123.com",
  "available": true,
  "checkedAt": "2026-04-11T19:39:00Z",
  "rdapStatus": 404,
  "rdapSource": "rdap.org"
}
```

### 3. Invalid domain (subdomain)

```
GET /api/domain-availability?domain=www.example.com
```

```json
{
  "error": "InvalidDomain",
  "message": "Subdomains are not allowed. Please supply a root domain (e.g., example.com)."
}
```

### 4. .MX second-level domain (available)

```
GET /api/domain-availability?domain=myblog.com.mx
```

```json
{
  "domain": "myblog.com.mx",
  "available": true,
  "checkedAt": "2026-04-11T19:39:00Z",
  "rdapStatus": 404,
  "rdapSource": "rdap.org"
}
```

### 5. RDAP lookup failure

```
GET /api/domain-availability?domain=example.com
```

*(When RDAP returns 5xx)*

```json
{
  "error": "RdapLookupFailed",
  "message": "The RDAP service returned an unexpected response."
}
```

---

## Notes on RDAP Behavior

See [rdap-behavior.md](rdap-behavior.md) for a full explanation of the RDAP protocol, how status codes are interpreted, and rate-limiting considerations.
