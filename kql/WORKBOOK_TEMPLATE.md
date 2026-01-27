# Log Analytics Workbook Template for Authenticated Functions

This is a sample Azure Monitor Workbook configuration for monitoring authenticated functions. Import this into Azure Monitor Workbooks to get started quickly.

## Quick Setup

1. Navigate to your Application Insights resource in Azure Portal
2. Select "Workbooks" from the left menu
3. Click "New" to create a new workbook
4. Click "Advanced Editor" (</> icon)
5. Copy sections below and add them as query components

## Workbook Sections

### Section 1: Authenticated Function Activity Overview

**Query:**

```kql
let timeRange = {TimeRange};
customEvents
| where timestamp {timeRange}
| where name == 'AuthenticatedFunctionCall'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| extend UserId = tostring(customDimensions["UserId"])
| summarize 
    TotalCalls = count(),
    UniqueUsers = dcount(UserId)
    by FunctionName
| order by TotalCalls desc
```

**Chart Type:** Bar Chart  
**Title:** Authenticated Function Call Summary  
**Size:** Large

---

### Section 2: User Activity Timeline

**Query:**

```kql
let timeRange = {TimeRange};
customEvents
| where timestamp {timeRange}
| where name == 'AuthenticatedFunctionCall'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| extend UserId = tostring(customDimensions["UserId"])
| summarize CallCount = count() by bin(timestamp, 1h), FunctionName
| order by timestamp desc
```

**Chart Type:** Line Chart (Time Chart)  
**Title:** Function Calls Over Time  
**Size:** Large

---

### Section 3: Error Rate Analysis

**Query:**

```kql
let timeRange = {TimeRange};
let calls = customEvents
| where timestamp {timeRange}
| where name == 'AuthenticatedFunctionCall'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| summarize TotalCalls = count() by FunctionName;
let errors = customEvents
| where timestamp {timeRange}
| where name == 'AuthenticatedFunctionError'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| summarize ErrorCount = count() by FunctionName;
calls
| join kind=leftouter errors on FunctionName
| extend ErrorCount = coalesce(ErrorCount, 0)
| extend ErrorRate = round(100.0 * ErrorCount / TotalCalls, 2)
| project FunctionName, TotalCalls, ErrorCount, ErrorRate
| order by ErrorRate desc
```

**Chart Type:** Table  
**Title:** Function Error Rates  
**Size:** Medium

---

### Section 4: Top Active Users

**Query:**

```kql
let timeRange = {TimeRange};
customEvents
| where timestamp {timeRange}
| where name == 'AuthenticatedFunctionCall'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| extend UserId = tostring(customDimensions["UserId"])
| extend UserEmailDomain = tostring(customDimensions["UserEmailDomain"])
| summarize 
    TotalCalls = count(),
    UniqueFunctions = dcount(FunctionName),
    LastActivity = max(timestamp)
    by UserId, UserEmailDomain
| top 20 by TotalCalls desc
| project UserId, UserEmailDomain, TotalCalls, UniqueFunctions, LastActivity
```

**Chart Type:** Table  
**Title:** Top 20 Active Users  
**Size:** Medium

---

### Section 5: Recent Errors (Detailed)

**Query:**

```kql
let timeRange = {TimeRange};
customEvents
| where timestamp {timeRange}
| where name == 'AuthenticatedFunctionError'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| extend UserId = tostring(customDimensions["UserId"])
| extend UserEmailDomain = tostring(customDimensions["UserEmailDomain"])
| extend ErrorMessage = tostring(customDimensions["ErrorMessage"])
| extend ErrorType = tostring(customDimensions["ErrorType"])
| project 
    Timestamp = timestamp,
    FunctionName,
    ErrorType,
    ErrorMessage,
    UserId,
    UserEmailDomain
| order by Timestamp desc
| take 50
```

**Chart Type:** Table  
**Title:** Recent Errors (Last 50)  
**Size:** Large

---

### Section 6: Subscription Management Activity

**Query:**

```kql
let timeRange = {TimeRange};
customEvents
| where timestamp {timeRange}
| where name in ('AuthenticatedFunctionCall', 'AuthenticatedFunctionSuccess')
| extend FunctionName = tostring(customDimensions["FunctionName"])
| where FunctionName in ('FindSubscription', 'ListSubscription', 'UpdateSubscription', 'InvoicePreview')
| extend UserId = tostring(customDimensions["UserId"])
| summarize 
    OperationCount = count(),
    UniqueUsers = dcount(UserId)
    by bin(timestamp, 1h), FunctionName
| order by timestamp desc
```

**Chart Type:** Stacked Area Chart  
**Title:** Subscription Management Operations  
**Size:** Large

---

### Section 7: Testimonial Operations

**Query:**

```kql
let timeRange = {TimeRange};
let operations = customEvents
| where timestamp {timeRange}
| where name in ('AuthenticatedFunctionCall', 'AuthenticatedFunctionSuccess', 'AuthenticatedFunctionError')
| extend FunctionName = tostring(customDimensions["FunctionName"])
| where FunctionName in ('CreateTestimonial', 'UpdateTestimonial', 'DeleteTestimonial')
| extend EventType = case(
    name == 'AuthenticatedFunctionSuccess', 'Success',
    name == 'AuthenticatedFunctionError', 'Error',
    'Call'
);
operations
| summarize 
    TotalOperations = count(),
    SuccessCount = countif(EventType == 'Success'),
    ErrorCount = countif(EventType == 'Error')
    by bin(timestamp, 1h), FunctionName
| extend SuccessRate = round(100.0 * SuccessCount / TotalOperations, 2)
| order by timestamp desc
```

**Chart Type:** Line Chart  
**Title:** Testimonial Operations Success Rate  
**Size:** Medium

---

## Parameters

Add these parameters to your workbook for dynamic filtering:

### Time Range Parameter

- **Name:** TimeRange
- **Type:** Time Range Picker
- **Default:** Last 24 hours
- **Available Ranges:** Last hour, Last 24 hours, Last 7 days, Last 30 days, Custom

### Function Name Filter (Optional)

- **Name:** FunctionFilter
- **Type:** Dropdown (Multi-select)
- **Query:**

```kql
customEvents
| where name == 'AuthenticatedFunctionCall'
| extend FunctionName = tostring(customDimensions["FunctionName"])
| distinct FunctionName
| order by FunctionName asc
```

- **Default:** All

### User Filter (Optional)

- **Name:** UserFilter
- **Type:** Text Input
- **Label:** Filter by User ID
- **Default:** (empty)

## Alerts Configuration

Consider setting up these alerts based on the queries:

### High Error Rate Alert

- **Condition:** Error rate > 10% over 5 minutes
- **Query:**

```kql
let errors = customEvents
| where name == 'AuthenticatedFunctionError'
| summarize ErrorCount = count();
let total = customEvents
| where name == 'AuthenticatedFunctionCall'
| summarize TotalCalls = count();
errors
| extend ErrorRate = 100.0 * ErrorCount / toscalar(total | project TotalCalls)
| where ErrorRate > 10
```

### Low Activity Alert

- **Condition:** No activity for 1 hour
- **Query:**

```kql
customEvents
| where name == 'AuthenticatedFunctionCall'
| where timestamp > ago(1h)
| summarize CallCount = count()
| where CallCount == 0
```

### Suspicious Activity Alert

- **Condition:** User makes > 100 calls in 5 minutes
- **Query:**

```kql
customEvents
| where name == 'AuthenticatedFunctionCall'
| where timestamp > ago(5m)
| extend UserId = tostring(customDimensions["UserId"])
| summarize CallCount = count() by UserId
| where CallCount > 100
```

## Tips

1. **Auto-Refresh:** Set workbook to auto-refresh every 5 minutes
2. **Default Time Range:** Set to "Last 24 hours" for most monitoring scenarios
3. **Pin to Dashboard:** Pin key charts to Azure Dashboard for quick access
4. **Share with Team:** Use "Share" button to give access to team members
5. **Export:** Export workbook as template for reuse across environments

## Advanced Customization

### Adding User-Specific Views

Modify queries to filter by specific user:

```kql
| where UserId == "{UserFilter}"
```

### Adding Email Domain Filtering

Filter by organization:

```kql
| where UserEmailDomain == "example.com"
```

### Performance Metrics

Add custom metrics to track operation duration (if instrumented):

```kql
| extend Duration = todouble(customMeasurements["Duration"])
| summarize AvgDuration = avg(Duration), P95Duration = percentile(Duration, 95)
```

## Resources

- [Azure Monitor Workbooks Documentation](https://docs.microsoft.com/azure/azure-monitor/visualize/workbooks-overview)
- [KQL Quick Reference](https://docs.microsoft.com/azure/data-explorer/kql-quick-reference)
- See `kql/AUTHENTICATED_FUNCTIONS_README.md` for query details
- See `AUTHENTICATED_FUNCTION_LOGGING_IMPLEMENTATION.md` for implementation details
