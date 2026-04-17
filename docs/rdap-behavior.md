# RDAP Behavior

## What is RDAP?

**RDAP** (Registration Data Access Protocol) is the modern, standardized replacement for the legacy **WHOIS** protocol. It is defined by a series of IETF RFCs (7480–7484) and is maintained by ICANN. RDAP provides machine-readable JSON responses, supports Unicode/internationalised domain names natively, and includes authentication and access-control mechanisms absent from WHOIS.

---

## Why RDAP Instead of WHOIS?

| Concern | WHOIS | RDAP |
|---------|-------|------|
| Response format | Plain text (varies per registrar) | Structured JSON |
| Internationalised names (IDN) | Limited, often broken | Full Unicode support |
| Standardisation | No formal schema | IETF-standardised (RFC 7483) |
| Rate limiting | Opaque; varies widely | Defined in RFC 7480; standard HTTP 429 |
| Bulk lookups | Frequently blocked | Standard HTTP, easy to extend |
| Privacy / GDPR | Inconsistent redaction | Tiered access model built in |
| Protocol | Custom plain-text TCP | Plain HTTPS |

---

## How rdap.org Works

[rdap.org](https://rdap.org) is a community-operated **bootstrap proxy**. When you query:

```
GET https://rdap.org/domain/example.com
```

The proxy:

1. Determines the authoritative RDAP server for the TLD (using IANA's RDAP bootstrap registry).
2. Forwards the request to the correct registry (e.g., Verisign for `.com`).
3. Returns the full RDAP JSON response to the caller.

This means callers do not need to implement TLD-to-server routing themselves.

---

## HTTP Status Code Mapping

| RDAP Status | Meaning in this API | `available` field |
|-------------|---------------------|-------------------|
| `200 OK` | Domain is **registered** — a registration record was found. | `false` |
| `404 Not Found` | Domain is **available** — no registration record exists. | `true` |
| Any other code | Unexpected error from the RDAP service. | N/A — returns `502 Bad Gateway` |

> **Note:** A `404` from RDAP specifically means the registry has no record for that domain, which is the standard way to signal availability. It does not imply an error.

---

## Rate Limiting Considerations

RDAP services, including `rdap.org`, may impose rate limits to protect registry infrastructure:

- **HTTP 429 Too Many Requests** — The client is sending requests too quickly. The response may include a `Retry-After` header.
- This API does **not** currently cache RDAP responses. Callers should avoid calling this endpoint in tight loops.
- For production deployments that need high-volume lookups, consider:
  - Adding a short-lived response cache (e.g., 60 seconds per domain).
  - Implementing exponential back-off on 429 responses.
  - Consulting the specific registry's RDAP terms of service.

---

## Known Limitations

- **Privacy-redacted registrations:** Some registrars redact registration data under GDPR. The domain will still return `200` (registered), but the JSON body may contain limited information.
- **Newly registered domains:** There can be a propagation delay of minutes to hours before a newly registered domain appears in RDAP.
- **ccTLD support:** Not all country-code TLD registries implement RDAP. If a ccTLD registry does not support RDAP, `rdap.org` may return a non-200/404 status, which this API surfaces as a `502 Bad Gateway`.

---

## Timeout Handling and Retry Behaviour

RDAP bootstrap proxies such as `rdap.org` occasionally take longer than expected to forward a request
to the authoritative TLD registry. To mitigate transient slowness without immediately failing the
caller, `RdapClient` implements a **single automatic retry**:

1. The first HTTP request is issued with the configured timeout (default 10 s).
2. If the request **times out** (`OperationCanceledException` / `TaskCanceledException`) or encounters
   a **transient network error** (`HttpRequestException`), the client waits **1 second** and tries once
   more.
3. If the retry also fails, the exception is propagated and the function returns **502 Bad Gateway**
   with error code `RdapLookupFailed`.

Client-initiated cancellations (i.e. the HTTP caller disconnecting before a response is returned) are
**not** retried and always result in a `499 Client Closed Request` response.

### Tuning recommendations for high-timeout environments

If timeouts remain frequent even after the retry, consider the following additional mitigations:

| Mitigation | How |
|------------|-----|
| Increase per-request timeout | Raise `client.Timeout` in `ServiceFactory.AddRdapClient` (e.g. to 15 s) |
| Add a short-lived cache | Cache successful responses for 60–300 s per domain to avoid redundant lookups |
| Exponential back-off on 429 | Respect the `Retry-After` header when the RDAP service rate-limits the client |
| Use a registry-specific RDAP endpoint | For `.com`/`.net`, query `https://rdap.verisign.com/` directly to skip the bootstrap proxy |
