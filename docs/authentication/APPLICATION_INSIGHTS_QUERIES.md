# Microsoft Entra ID Authentication - Application Insights KQL Queries

## Overview

This document provides a comprehensive collection of KQL (Kusto Query Language) queries for monitoring and troubleshooting Microsoft Entra ID authentication in Application Insights. These queries help identify authentication issues, monitor performance, and analyze user behavior.

## Table of Contents

1. [Authentication Monitoring Queries](#authentication-monitoring-queries)
2. [Error Detection & Diagnostics](#error-detection--diagnostics)
3. [Performance & Metrics](#performance--metrics)
4. [User Activity Analysis](#user-activity-analysis)
5. [Security & Audit](#security--audit)
6. [Creating Workbooks](#creating-workbooks)
7. [Setting Up Alerts](#setting-up-alerts)

## Authentication Monitoring Queries

### Authentication Success Rate

Track overall authentication success rate over time.

```kql
// Authentication success rate over last 24 hours
let timeRange = 24h;
let authRequests = requests
| where timestamp > ago(timeRange)
| where url contains "/api/" and name !contains "health"
| extend hasAuthHeader = isnotempty(parse_json(customDimensions).Authorization) or 
                         isnotempty(parse_json(customDimensions).authorization);

let totalAuth = authRequests | count;
let successfulAuth = authRequests | where resultCode < 400 | count;
let failedAuth = authRequests | where resultCode in (401, 403) | count;

print 
    TimeRange = strcat(timeRange),
    TotalRequests = totalAuth,
    Successful = successfulAuth,
    Failed = failedAuth,
    SuccessRate = strcat(round(todouble(successfulAuth) / todouble(totalAuth) * 100, 2), "%"),
    FailureRate = strcat(round(todouble(failedAuth) / todouble(totalAuth) * 100, 2), "%")
```

**Visualization**: Single value card or KPI
**Use Case**: Dashboard overview of authentication health

---

### Recent 401 Unauthorized Responses

Find recent authentication failures with details.

```kql
// Recent 401 Unauthorized responses with context
requests
| where timestamp > ago(1h)
| where resultCode == 401
| order by timestamp desc
| extend 
    Endpoint = name,
    URL = url,
    Duration_ms = duration,
    ClientIP = client_IP
| project 
    timestamp,
    Endpoint,
    URL,
    Duration_ms,
    ClientIP,
    operation_Id
| take 50
```

**Visualization**: Table
**Use Case**: Real-time monitoring of authentication failures

---

### Authentication Failures by Endpoint

Identify which endpoints have the most authentication failures.

```kql
// Authentication failures grouped by endpoint (last 24 hours)
requests
| where timestamp > ago(24h)
| where resultCode == 401
| summarize 
    FailureCount = count(),
    AvgDuration = avg(duration),
    UniqueClients = dcount(client_IP)
    by name
| order by FailureCount desc
| take 20
```

**Visualization**: Bar chart
**Use Case**: Identify problematic endpoints

---

### Authenticated Function Activity

Track activity across authenticated functions (uses custom telemetry).

```kql
// Authenticated function calls summary
customEvents
| where name == 'AuthenticatedFunctionCall'
| extend 
    FunctionName = tostring(customDimensions["FunctionName"]),
    UserId = tostring(customDimensions["UserId"]),
    UserEmailDomain = tostring(customDimensions["UserEmailDomain"])
| summarize 
    CallCount = count(),
    UniqueUsers = dcount(UserId),
    UniqueEmailDomains = dcount(UserEmailDomain)
    by bin(timestamp, 1h), FunctionName
| order by timestamp desc, CallCount desc
| render timechart
```

**Visualization**: Time chart
**Use Case**: Understand usage patterns across authenticated endpoints

---

## Error Detection & Diagnostics

### JWT Validation Errors

Detect all JWT-related validation errors.

```kql
// JWT validation errors with details
traces
| where timestamp > ago(24h)
| where message contains "JWT" 
    or message contains "token validation"
    or message contains "IDX"
    or message contains "Bearer"
| where severityLevel >= 2  // Warning or higher
| extend 
    ErrorType = case(
        message contains "IDX10503", "Signature Key Not Found",
        message contains "IDX10205", "Invalid Issuer",
        message contains "IDX10214", "Invalid Audience",
        message contains "IDX10223", "Token Expired",
        message contains "Authorization header", "Missing/Invalid Header",
        "Other JWT Error"
    )
| order by timestamp desc
| project 
    timestamp,
    severityLevel,
    ErrorType,
    message,
    operation_Id
| take 100
```

**Visualization**: Table
**Use Case**: Diagnose specific JWT validation failures

---

### Signing Key Rotation Issues

Detect when signing key rotation causes authentication failures.

```kql
// Detect signing key rotation events
traces
| where timestamp > ago(7d)
| where message contains "SecurityTokenSignatureKeyNotFoundException"
    or message contains "IDX10503"
    or message contains "Signature validation failed"
    or message contains "Signing key not found"
    or message contains "refresh metadata"
| extend 
    Event = case(
        message contains "Attempting to refresh", "Key Refresh Initiated",
        message contains "successfully after metadata refresh", "Key Refresh Successful",
        message contains "IDX10503", "Key Not Found Error",
        "Key Rotation Event"
    )
| summarize 
    EventCount = count(),
    FirstOccurrence = min(timestamp),
    LastOccurrence = max(timestamp),
    SampleMessage = any(message)
    by Event
| order by EventCount desc
```

**Visualization**: Table with summary stats
**Use Case**: Monitor signing key rotation health

---

### Token Expiration Failures

Find errors due to expired tokens.

```kql
// Token expiration errors
traces
| where timestamp > ago(24h)
| where message contains "IDX10223" 
    or message contains "token is expired"
    or message contains "Lifetime validation failed"
    or message contains "exp claim"
| order by timestamp desc
| project 
    timestamp,
    severityLevel,
    message,
    operation_Id
| take 50
```

**Visualization**: Table
**Use Case**: Identify clients not refreshing tokens properly

---

### Audience Validation Failures

Detect audience mismatch errors.

```kql
// Audience validation failures
traces
| where timestamp > ago(24h)
| where message contains "IDX10214" 
    or message contains "Audience validation failed"
| extend 
    TokenAudience = extract("Audiences: '([^']+)'", 1, message),
    ExpectedAudience = extract("ValidAudience: '([^']+)'", 1, message)
| order by timestamp desc
| project 
    timestamp,
    message,
    TokenAudience,
    ExpectedAudience,
    operation_Id
| take 50
```

**Visualization**: Table
**Use Case**: Diagnose audience configuration issues

---

### Authenticated Function Errors

Track errors from authenticated functions with full context.

```kql
// Authenticated function errors analysis
customEvents
| where name == 'AuthenticatedFunctionError'
| extend 
    FunctionName = tostring(customDimensions["FunctionName"]),
    UserId = tostring(customDimensions["UserId"]),
    UserEmailDomain = tostring(customDimensions["UserEmailDomain"]),
    ErrorMessage = tostring(customDimensions["ErrorMessage"]),
    ErrorType = tostring(customDimensions["ErrorType"])
| summarize 
    ErrorCount = count(),
    AffectedUsers = dcount(UserId),
    SampleErrorMessage = any(ErrorMessage)
    by bin(timestamp, 1h), FunctionName, ErrorType
| order by timestamp desc, ErrorCount desc
| render timechart
```

**Visualization**: Time chart
**Use Case**: Monitor error trends in authenticated endpoints

---

## Performance & Metrics

### Authentication Latency

Measure authentication overhead on request processing.

```kql
// Average request duration by authentication status
requests
| where timestamp > ago(24h)
| where name !contains "health"
| extend IsAuthenticated = resultCode < 400
| summarize 
    AvgDuration = avg(duration),
    P50Duration = percentile(duration, 50),
    P95Duration = percentile(duration, 95),
    P99Duration = percentile(duration, 99),
    RequestCount = count()
    by IsAuthenticated
| project 
    AuthStatus = iff(IsAuthenticated, "Authenticated", "Failed Auth"),
    AvgDuration_ms = round(AvgDuration, 2),
    P50_ms = round(P50Duration, 2),
    P95_ms = round(P95Duration, 2),
    P99_ms = round(P99Duration, 2),
    RequestCount
```

**Visualization**: Table
**Use Case**: Understand authentication performance impact

---

### Metadata Refresh Frequency

Track how often OpenID Connect metadata is refreshed.

```kql
// Metadata refresh events
traces
| where timestamp > ago(7d)
| where message contains "ConfigurationManager" 
    or message contains "metadata"
    or message contains "refresh"
| where message contains "OpenIdConnect" or message contains "signing keys"
| summarize RefreshCount = count() by bin(timestamp, 1h)
| render timechart
```

**Visualization**: Time chart
**Use Case**: Monitor metadata refresh patterns

---

### Token Validation Time

Measure JWT validation performance.

```kql
// Token validation duration
dependencies
| where timestamp > ago(24h)
| where name contains "ValidateToken" or name contains "JWT"
| summarize 
    AvgDuration = avg(duration),
    P95Duration = percentile(duration, 95),
    MaxDuration = max(duration),
    Count = count()
    by bin(timestamp, 1h)
| render timechart
```

**Visualization**: Time chart
**Use Case**: Track validation performance over time

---

## User Activity Analysis

### Active Users by Time Period

Track unique authenticated users over time.

```kql
// Active authenticated users over time
customEvents
| where name in ('AuthenticatedFunctionCall', 'AuthenticatedFunctionSuccess')
| extend UserId = tostring(customDimensions["UserId"])
| where isnotempty(UserId)
| summarize UniqueUsers = dcount(UserId) by bin(timestamp, 1h)
| render timechart
```

**Visualization**: Time chart
**Use Case**: Monitor user engagement

---

### User Activity Details

Analyze individual user authentication patterns.

```kql
// User-specific authentication activity
let userId = "user-id-here";  // Replace with actual user ID
union requests, traces, customEvents
| where timestamp > ago(7d)
| where message contains userId 
    or customDimensions.UserId == userId
    or operation_UserId == userId
| extend 
    EventType = case(
        itemType == "request" and resultCode == 200, "Successful Request",
        itemType == "request" and resultCode == 401, "Auth Failed",
        itemType == "trace" and severityLevel >= 3, "Error",
        itemType == "customEvent" and name contains "AuthenticatedFunction", "Function Call",
        "Other"
    )
| order by timestamp asc
| project 
    timestamp,
    EventType,
    Details = coalesce(message, name),
    StatusCode = coalesce(resultCode, 0),
    operation_Id
```

**Visualization**: Table
**Use Case**: Troubleshoot user-specific issues

---

### Most Active Users

Identify users with highest API usage.

```kql
// Top authenticated users by activity
customEvents
| where name in ('AuthenticatedFunctionCall', 'AuthenticatedFunctionSuccess')
| where timestamp > ago(7d)
| extend 
    UserId = tostring(customDimensions["UserId"]),
    UserEmailDomain = tostring(customDimensions["UserEmailDomain"])
| summarize 
    TotalCalls = count(),
    UniqueFunctions = dcount(tostring(customDimensions["FunctionName"])),
    FirstSeen = min(timestamp),
    LastSeen = max(timestamp)
    by UserId, UserEmailDomain
| order by TotalCalls desc
| take 50
```

**Visualization**: Table
**Use Case**: Understand usage patterns, identify power users

---

### User Sessions

Track user session duration and activity.

```kql
// User sessions analysis
customEvents
| where name in ('AuthenticatedFunctionCall', 'AuthenticatedFunctionSuccess')
| where timestamp > ago(24h)
| extend UserId = tostring(customDimensions["UserId"])
| summarize 
    SessionStart = min(timestamp),
    SessionEnd = max(timestamp),
    EventCount = count()
    by UserId, operation_Id
| extend SessionDuration = datetime_diff('minute', SessionEnd, SessionStart)
| where SessionDuration > 0
| project 
    UserId,
    SessionStart,
    SessionDuration_minutes = SessionDuration,
    EventCount
| order by SessionDuration_minutes desc
```

**Visualization**: Table
**Use Case**: Understand user engagement duration

---

## Security & Audit

### Failed Authentication Attempts by IP

Detect potential brute force attempts.

```kql
// Failed auth attempts by client IP
requests
| where timestamp > ago(1h)
| where resultCode == 401
| summarize 
    FailedAttempts = count(),
    UniqueEndpoints = dcount(name),
    FirstAttempt = min(timestamp),
    LastAttempt = max(timestamp)
    by client_IP, client_City, client_CountryOrRegion
| where FailedAttempts > 10  // Threshold for suspicious activity
| order by FailedAttempts desc
```

**Visualization**: Table with map
**Use Case**: Security monitoring, detect attacks

---

### Geographic Distribution of Authentication

View authentication requests by location.

```kql
// Authentication requests by geographic location
requests
| where timestamp > ago(24h)
| where resultCode < 500  // Exclude server errors
| extend AuthStatus = iff(resultCode < 400, "Success", "Failed")
| summarize 
    TotalRequests = count(),
    SuccessfulAuth = countif(AuthStatus == "Success"),
    FailedAuth = countif(AuthStatus == "Failed")
    by client_CountryOrRegion, client_City
| extend SuccessRate = round(todouble(SuccessfulAuth) / todouble(TotalRequests) * 100, 2)
| order by TotalRequests desc
```

**Visualization**: Map or table
**Use Case**: Identify unusual geographic patterns

---

### Unauthorized Access Attempts

Track all 401/403 responses with details.

```kql
// Detailed unauthorized access attempts
requests
| where timestamp > ago(24h)
| where resultCode in (401, 403)
| extend 
    Endpoint = name,
    Method = url_method = parse_url(url).Scheme,
    Path = url
| summarize 
    AttemptCount = count(),
    UniqueIPs = dcount(client_IP),
    FirstAttempt = min(timestamp),
    LastAttempt = max(timestamp),
    SampleIP = any(client_IP)
    by Endpoint, resultCode
| order by AttemptCount desc
```

**Visualization**: Table
**Use Case**: Security audit, detect unauthorized access patterns

---

### Token Tampering Detection

Detect potential token tampering attempts.

```kql
// Potential token tampering (signature failures)
traces
| where timestamp > ago(24h)
| where message contains "signature" 
    and (message contains "failed" or message contains "invalid")
| extend 
    ClientIP = tostring(customDimensions.ClientIP),
    Endpoint = tostring(customDimensions.Endpoint)
| summarize 
    TamperingAttempts = count(),
    UniqueIPs = dcount(ClientIP),
    Endpoints = make_set(Endpoint)
    by bin(timestamp, 1h)
| where TamperingAttempts > 5
| render timechart
```

**Visualization**: Time chart with threshold
**Use Case**: Detect token manipulation attempts

---

### Role-Based Access Patterns

Analyze access patterns by user roles.

```kql
// Access patterns by user roles
customEvents
| where name in ('AuthenticatedFunctionCall', 'AuthenticatedFunctionSuccess')
| where timestamp > ago(7d)
| extend 
    UserId = tostring(customDimensions["UserId"]),
    FunctionName = tostring(customDimensions["FunctionName"])
| join kind=inner (
    // Extract roles from token validation logs
    traces
    | where message contains "roles" and message contains "claim"
    | extend 
        UserId = tostring(customDimensions["UserId"]),
        Roles = extract_all(@"ImageStorageTier\.(\w+)", message)
    | summarize Roles = make_set(Roles) by UserId
) on UserId
| summarize 
    AccessCount = count(),
    UniqueFunctions = dcount(FunctionName)
    by UserId, tostring(Roles)
| order by AccessCount desc
```

**Visualization**: Table
**Use Case**: Understand role-based access patterns, validate RBAC

---

## Creating Workbooks

### Authentication Dashboard Workbook

Create a comprehensive authentication monitoring workbook in Azure Portal:

**Step 1: Create Workbook**

1. Navigate to Application Insights
2. Select **Workbooks** from the left menu
3. Click **New** to create a new workbook

**Step 2: Add Sections**

Add these sections in order:

#### Section 1: Overview Metrics

- Add **Metrics** tile
- Add 4 KPI tiles:
  - Total Requests (last 24h)
  - Success Rate
  - Failed Authentications
  - Unique Active Users

#### Section 2: Authentication Success Rate

- Add **Query** tile
- Use "Authentication Success Rate" query above
- Visualization: Line chart
- Time range: Last 24 hours

#### Section 3: Recent Failures

- Add **Query** tile
- Use "Recent 401 Unauthorized Responses" query
- Visualization: Table
- Add refresh button

#### Section 4: Error Analysis

- Add **Query** tile
- Use "JWT Validation Errors" query
- Visualization: Pie chart (by ErrorType)

#### Section 5: User Activity

- Add **Query** tile
- Use "Most Active Users" query
- Visualization: Table

#### Section 6: Geographic Distribution

- Add **Query** tile
- Use "Geographic Distribution of Authentication" query
- Visualization: Map

**Step 3: Configure Auto-Refresh**

- Set workbook to auto-refresh every 5 minutes
- Pin important tiles to Azure Dashboard

**Step 4: Share Workbook**

- Save workbook with descriptive name: "Authentication Monitoring Dashboard"
- Share with team members
- Set default time range to 24 hours

---

## Setting Up Alerts

### Alert 1: High Authentication Failure Rate

**Purpose**: Alert when authentication failure rate exceeds threshold

**Configuration**:

```kql
// Alert query: High authentication failure rate
let timeRange = 5m;
let threshold = 0.20;  // 20% failure rate

let totalRequests = requests
| where timestamp > ago(timeRange)
| where url contains "/api/"
| count;

let failedAuth = requests
| where timestamp > ago(timeRange)
| where resultCode == 401
| count;

let failureRate = todouble(failedAuth) / todouble(totalRequests);

print FailureRate = failureRate, Threshold = threshold
| where FailureRate > Threshold
```

**Alert Settings**:

- Severity: 2 (Warning)
- Frequency: Every 5 minutes
- Time window: 5 minutes
- Threshold: When failure rate > 20%
- Action: Send email to DevOps team

---

### Alert 2: Signing Key Rotation Failures

**Purpose**: Alert when signing key rotation causes repeated failures

**Configuration**:

```kql
// Alert query: Signing key rotation issues
traces
| where timestamp > ago(10m)
| where message contains "SecurityTokenSignatureKeyNotFoundException"
    or message contains "IDX10503"
| summarize ErrorCount = count()
| where ErrorCount > 10
```

**Alert Settings**:

- Severity: 1 (Error)
- Frequency: Every 5 minutes
- Time window: 10 minutes
- Threshold: When error count > 10
- Action: Send email + Create incident

---

### Alert 3: Unusual Failed Authentication from Single IP

**Purpose**: Detect potential brute force attempts

**Configuration**:

```kql
// Alert query: Brute force detection
requests
| where timestamp > ago(5m)
| where resultCode == 401
| summarize FailedAttempts = count() by client_IP
| where FailedAttempts > 20
```

**Alert Settings**:

- Severity: 1 (Error)
- Frequency: Every 5 minutes
- Time window: 5 minutes
- Threshold: When single IP has > 20 failures
- Action: Send email to Security team + Create incident

---

### Alert 4: No Authenticated Traffic

**Purpose**: Alert if no authenticated traffic detected (potential outage)

**Configuration**:

```kql
// Alert query: No authenticated traffic
customEvents
| where timestamp > ago(15m)
| where name == 'AuthenticatedFunctionCall'
| count
| where Count == 0
```

**Alert Settings**:

- Severity: 1 (Error)
- Frequency: Every 15 minutes
- Time window: 15 minutes
- Threshold: When count == 0
- Action: Send email + SMS to on-call engineer

---

## Query Best Practices

### Performance Optimization

1. **Use time filters early**:

   ```kql
   // Good
   requests
   | where timestamp > ago(24h)  // Filter first
   | where resultCode == 401
   
   // Bad
   requests
   | where resultCode == 401  // Scans entire table
   | where timestamp > ago(24h)
   ```

2. **Use summarize for aggregations**:

   ```kql
   // Good
   requests
   | where timestamp > ago(24h)
   | summarize count() by name
   
   // Bad (slower)
   requests
   | where timestamp > ago(24h)
   | extend Count = 1
   | summarize sum(Count) by name
   ```

3. **Limit result sets**:

   ```kql
   // Always use take for large result sets
   requests
   | where timestamp > ago(24h)
   | take 1000  // Limit results
   ```

### Readability

1. **Use comments**:

   ```kql
   // Authentication failure analysis
   // Purpose: Identify endpoints with high failure rates
   // Time range: Last 24 hours
   requests
   | where timestamp > ago(24h)
   | where resultCode == 401
   ```

2. **Format for clarity**:

   ```kql
   requests
   | where timestamp > ago(24h)
   | where resultCode == 401
   | summarize 
       FailureCount = count(),
       UniqueIPs = dcount(client_IP)
       by name
   | order by FailureCount desc
   ```

3. **Use meaningful variable names**:

   ```kql
   let timeRange = 24h;
   let failureThreshold = 100;
   
   requests
   | where timestamp > ago(timeRange)
   | where resultCode == 401
   | summarize FailureCount = count()
   | where FailureCount > failureThreshold
   ```

## Related Documentation

- [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) - JWT troubleshooting guide
- [ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md) - Complete exception reference
- [AUTHENTICATION_LOGGING.md](AUTHENTICATION_LOGGING.md) - Logging implementation guide
- [../AUTHENTICATED_FUNCTION_LOGGING_IMPLEMENTATION.md](../AUTHENTICATED_FUNCTION_LOGGING_IMPLEMENTATION.md) - Telemetry implementation

## External Resources

- [KQL Quick Reference](https://learn.microsoft.com/azure/data-explorer/kql-quick-reference)
- [Application Insights Query Language](https://learn.microsoft.com/azure/azure-monitor/logs/get-started-queries)
- [Log Analytics Workbooks](https://learn.microsoft.com/azure/azure-monitor/visualize/workbooks-overview)

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-13 | GitHub Copilot | Initial comprehensive KQL queries collection |

---

**Need More Help?** See [JWT_INVALID_TOKEN_TROUBLESHOOTING.md](JWT_INVALID_TOKEN_TROUBLESHOOTING.md) for step-by-step troubleshooting or [ENTRA_ID_EXCEPTIONS_REFERENCE.md](ENTRA_ID_EXCEPTIONS_REFERENCE.md) for error details.
