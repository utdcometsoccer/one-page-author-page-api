# Migration Plan: Upgrade to the Latest Application Insights (.NET) Implementation

## Goal

Move the solution forward from the current Application Insights .NET SDK 2.x–style integration (the version currently pinned in this repo) to the **latest Application Insights .NET approach**, while preserving:

- Azure Functions isolated-worker compatibility
- Existing telemetry emitted by `TelemetryClient.Track*` calls in `OnePageAuthorLib`
- Correlation between host + worker telemetry

This plan is written to avoid repeating the `ITelemetryInitializer`-style startup failures that occur when package versions are mismatched.

## Current State (Repo)

- Function apps use isolated worker telemetry wiring:
  - `AddApplicationInsightsTelemetryWorkerService()`
  - `ConfigureFunctionsApplicationInsights()`
  - Package: `Microsoft.Azure.Functions.Worker.ApplicationInsights`

- Shared library code (`OnePageAuthorLib`) uses classic Application Insights types:
  - `TelemetryClient`
  - `EventTelemetry`, etc.

- We have already observed (and documented) that upgrading `Microsoft.ApplicationInsights*` to a new major version without matching the Functions worker integration can break startup.

See also: [docs/ITELEMETRYINITIALIZER_BUG.md](ITELEMETRYINITIALIZER_BUG.md)

## Key Decision: Two Supported Upgrade Tracks

Microsoft’s current guidance supports two practical paths:

### Track A (Preferred for Azure Functions): OpenTelemetry export to Application Insights

Use Azure Functions’ OpenTelemetry integration to export to Azure Monitor / Application Insights.

- Docs: https://learn.microsoft.com/azure/azure-functions/opentelemetry-howto
- This path minimizes coupling to `Microsoft.ApplicationInsights.*` packages inside Functions.

**Implication for repo**: telemetry code in `OnePageAuthorLib` should migrate away from `TelemetryClient` to OpenTelemetry-native patterns (`ActivitySource`, `ILogger`, `Meter`) to avoid dual pipelines.

### Track B (Keep `TelemetryClient` API): Application Insights .NET SDK 3.x (OTel-based) upgrade

Upgrade to Application Insights .NET SDK 3.x, which keeps most `TelemetryClient`/`TelemetryConfiguration` APIs while routing through an OpenTelemetry-based implementation.

- Docs: https://learn.microsoft.com/azure/azure-monitor/app/migrate-to-opentelemetry
- Important constraint from Microsoft guidance: **don’t use Application Insights .NET SDK 3.x and the Azure Monitor OpenTelemetry Distro in the same application**.

**Risk for repo**: Azure Functions isolated worker integration (`Microsoft.Azure.Functions.Worker.ApplicationInsights`) may not be compatible with the newest AI major version. This must be validated via a spike before committing the whole solution.

## Recommended Plan

Because this repo is primarily Azure Functions isolated worker, default to **Track A** unless there is a hard requirement to keep `TelemetryClient` in production code.

If you *must* keep `TelemetryClient` with minimal code change, attempt Track B first as a controlled spike. If the Functions host fails to start or telemetry breaks correlation, switch to Track A.

## Decision Record (Repository Default)

- **Decision**: Use **Track A (OpenTelemetry export to Application Insights)** as the default upgrade path for this repository.
- **Status**: Adopted (this is the default unless explicitly overridden).
- **Rationale**: This solution is Azure Functions isolated-worker heavy and has already experienced startup failures from Application Insights major-version mismatches. Track A reduces direct dependency coupling to `Microsoft.ApplicationInsights.*` inside Functions.
- **When to override**: Choose Track B only if there is a strict requirement to keep `TelemetryClient` APIs with minimal refactor *and* the Track B compatibility spike succeeds for `Microsoft.Azure.Functions.Worker.ApplicationInsights` in this solution.

## Current Implementation Status (Track A)

Track A is now implemented in this repository:

- All Azure Functions isolated-worker hosts have been migrated to OpenTelemetry + Azure Monitor exporter (Application Insights backend):
  - `function-app`
  - `ImageAPI`
  - `InkStainedWretchFunctions`
  - `InkStainedWretchStripe`
  - `InkStainedWretchesConfig`
- The Function host telemetry mode is set to OpenTelemetry via `host.json` (`"telemetryMode": "OpenTelemetry"`).
- Legacy Application Insights SDK wiring (`AddApplicationInsightsTelemetryWorkerService` / `ConfigureFunctionsApplicationInsights`) and related packages have been removed from those hosts to avoid dual pipelines.
- Manual “custom event” telemetry previously sent via `TelemetryClient.TrackEvent(...)` in `OnePageAuthorLib` has been migrated to structured `ILogger` events (with an `EventName` dimension plus properties/metrics), so events remain queryable in Application Insights.

### Configuration requirement

To export telemetry to the Application Insights backend, set `APPLICATIONINSIGHTS_CONNECTION_STRING` in the environment (local.settings, User Secrets, or Key Vault/Function App settings).

### Validation queries (KQL)

The manual “custom events” in this repo are now emitted as **structured logs** (message: `TelemetryEvent {EventName}`) with dimensions stored in `customDimensions` (for example: `customDimensions.EventName`, `customDimensions.CustomerId`, `customDimensions.FunctionName`).

Use these queries in **Application Insights Logs** to validate behavior.

#### 1) See recent telemetry events

```kusto
traces
| where timestamp > ago(24h)
| where message startswith "TelemetryEvent"
| extend EventName = tostring(customDimensions["EventName"])
| project timestamp, cloud_RoleName, operation_Id, severityLevel, EventName, message, customDimensions
| order by timestamp desc
```

#### 2) Count events by name (top N)

```kusto
traces
| where timestamp > ago(7d)
| where message startswith "TelemetryEvent"
| extend EventName = tostring(customDimensions["EventName"])
| summarize Count = count() by EventName
| order by Count desc
```

#### 3) Stripe telemetry examples

```kusto
// Example: Stripe customer created
traces
| where timestamp > ago(30d)
| where message startswith "TelemetryEvent"
| where tostring(customDimensions["EventName"]) == "StripeCustomerCreated"
| project timestamp,
      cloud_RoleName,
      operation_Id,
      CustomerId = tostring(customDimensions["CustomerId"]),
      EmailDomain = tostring(customDimensions["EmailDomain"]),
      customDimensions
| order by timestamp desc
```

```kusto
// Example: Stripe API errors
traces
| where timestamp > ago(30d)
| where message startswith "TelemetryEvent"
| where tostring(customDimensions["EventName"]) == "StripeApiError"
| project timestamp,
      cloud_RoleName,
      operation_Id,
      Operation = tostring(customDimensions["Operation"]),
      ErrorCode = tostring(customDimensions["ErrorCode"]),
      ErrorType = tostring(customDimensions["ErrorType"]),
      CustomerId = tostring(customDimensions["CustomerId"])
| order by timestamp desc
```

#### 4) Authenticated function telemetry examples

```kusto
// Authenticated function errors
traces
| where timestamp > ago(30d)
| where message startswith "TelemetryEvent"
| where tostring(customDimensions["EventName"]) == "AuthenticatedFunctionError"
| project timestamp,
      cloud_RoleName,
      operation_Id,
      FunctionName = tostring(customDimensions["FunctionName"]),
      UserId = tostring(customDimensions["UserId"]),
      UserEmailDomain = tostring(customDimensions["UserEmailDomain"]),
      ErrorType = tostring(customDimensions["ErrorType"]),
      ErrorMessage = tostring(customDimensions["ErrorMessage"])
| order by timestamp desc
```

#### 5) Correlate a telemetry event to its request/dependencies

Use `operation_Id` from a telemetry event to pivot into request + dependency telemetry.

```kusto
let operationId = toscalar(
  traces
  | where timestamp > ago(24h)
  | where message startswith "TelemetryEvent"
  | where tostring(customDimensions["EventName"]) == "StripeWebhookEvent"
  | top 1 by timestamp desc
  | project operation_Id
);

union isfuzzy=true
(
  requests
  | where operation_Id == operationId
  | project timestamp, ItemType = "request", operation_Id, name, success, resultCode, duration, url
),
(
  dependencies
  | where operation_Id == operationId
  | project timestamp, ItemType = "dependency", operation_Id, name, success, resultCode, duration, target, type
),
(
  traces
  | where operation_Id == operationId
  | project timestamp, ItemType = "trace", operation_Id, message, severityLevel, customDimensions
)
| order by timestamp asc
```

#### 6) Common troubleshooting queries

```kusto
// If you see zero events, verify the connection string is present and the host is running in OpenTelemetry mode.
// This shows the most recent traces regardless of message.
traces
| where timestamp > ago(1h)
| project timestamp, cloud_RoleName, operation_Id, severityLevel, message
| order by timestamp desc
```

---

## Track B Plan (Spike First): Upgrade `TelemetryClient` Usage to AI .NET SDK 3.x

## Phase 0 — Preconditions

- Create a migration branch.
- Identify “telemetry owners” and define success criteria:
  - Function host starts locally and in Azure
  - Custom events emitted by `AuthenticatedFunctionTelemetryService` still arrive
  - Correlation of requests/dependencies remains acceptable

## Phase 1 — Compatibility Spike (Single Function App)

1. Choose one Functions host (start with `function-app`).
2. Upgrade **only** the minimal telemetry packages needed for that host and its referenced projects.
3. Build and run the host locally:
   - `dotnet build OnePageAuthorAPI.sln`
   - `cd function-app; func host start`
4. Validate:
   - No startup exceptions
   - Custom events appear in Application Insights
   - Dependencies are tracked

**Exit criteria**:

- If startup fails with AI extensibility/runtime binding errors (similar to the `ITelemetryInitializer` issue), Track B is not viable with the current Functions integration stack.

## Phase 2 — Roll Forward (If Spike Succeeds)

1. Upgrade `OnePageAuthorLib` to Application Insights .NET SDK 3.x (per Microsoft migration guidance).
2. Upgrade each Functions host in isolation (one PR per host to limit blast radius).
3. Re-run build + tests between each host upgrade.

## Phase 3 — Validation & Observability

- Confirm the following in App Insights:
  - custom events still show (`AuthenticatedFunctionCall`, `AuthenticatedFunctionSuccess`, `AuthenticatedFunctionError`)
  - dependency telemetry still correlates with requests
  - log volume and severity levels are expected (Functions host and worker have separate logging config per Microsoft guidance)

Docs note on isolated worker AI integration and logging: https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide#logging

---

## Track A Plan (Preferred): Move Azure Functions to OpenTelemetry Exporter

## Phase 0 — Inventory

- Enumerate all uses of Application Insights SDK types in `OnePageAuthorLib`:
  - `TelemetryClient`
  - `EventTelemetry`, `ExceptionTelemetry`, etc.
- Enumerate per-host DI wiring where App Insights is enabled.

## Phase 1 — Introduce OpenTelemetry in One Function Host

Following Microsoft’s Azure Functions OpenTelemetry how-to for **Application Insights** export:

- Add packages to the selected host:
  - `Microsoft.Azure.Functions.Worker.OpenTelemetry`
  - `OpenTelemetry.Extensions.Hosting`
  - `Azure.Monitor.OpenTelemetry.Exporter`

Docs: https://learn.microsoft.com/azure/azure-functions/opentelemetry-howto#enable-opentelemetry-in-your-app

Then:

1. Configure OpenTelemetry in the worker startup.
2. Ensure the host-side OpenTelemetry settings (in `host.json`) are aligned with the chosen telemetry mode (per the Functions OTel guide).
3. Run the host locally and verify logs/traces reach Application Insights.

## Phase 2 — Migrate `OnePageAuthorLib` Manual Telemetry

Replace “classic AI” manual tracking calls with OTel-native equivalents:

- Custom operations / tracing: `ActivitySource` + `Activity`
- Logging: `ILogger`
- Metrics: `Meter`

Microsoft mapping guidance:

- Telemetry initializers/processors vs OpenTelemetry processors: https://learn.microsoft.com/azure/azure-monitor/app/application-insights-faq#how-do-telemetry-processors-and-initializers-map-to-opentelemetry

## Phase 3 — Remove Direct AI SDK Dependencies from Function Hosts

Once OpenTelemetry export is verified:

- Remove `Microsoft.ApplicationInsights` / `Microsoft.ApplicationInsights.WorkerService` references from Functions apps (and ideally from shared library) to prevent dual-pipeline behavior and version conflicts.

## Phase 4 — Roll Out Across All Hosts

Apply the same OpenTelemetry config to:

- `function-app`
- `ImageAPI`
- `InkStainedWretchFunctions`
- `InkStainedWretchStripe`
- `InkStainedWretchesConfig`

Validate each host in isolation.

---

## Testing & Acceptance Criteria (Both Tracks)

## Must-pass checks

- `dotnet build OnePageAuthorAPI.sln`
- `dotnet test OnePageAuthorAPI.sln` (or the existing VS Code `run-tests` task)
- Start each Functions host locally without telemetry startup exceptions.

## Telemetry checks (App Insights)

- Events still appear for authenticated functions (Stripe + Testimonial functions)
- Dependencies (Cosmos/HTTP/Stripe) appear and correlate
- No unexpected doubling of request telemetry (watch for duplicate request telemetry if mixing instrumentations; see Functions OTel troubleshooting)

OTel troubleshooting notes (duplicates, missing request telemetry, etc.):

- https://learn.microsoft.com/azure/azure-functions/opentelemetry-howto#troubleshooting

---

## Rollback Strategy

- Keep the current “known-good” package set pinned in a revertable commit.
- Upgrade one host at a time; if a host fails startup in Azure, revert that host’s telemetry changes before touching the next.
- Prefer configuration-only rollbacks (environment variables / host.json) where possible.

---

## Recommendation Summary

- If you want the most stable path for Azure Functions isolated worker: **Track A (OpenTelemetry export)**.
- If you must keep `TelemetryClient` APIs with minimal refactor: try **Track B spike** first, but be prepared to fall back to Track A if Functions’ integration packages don’t support the new major AI SDK cleanly.
