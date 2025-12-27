# KQL Queries for Authenticated Functions

This directory contains Kusto Query Language (KQL) queries for analyzing authenticated function activity in Log Analytics workbooks.

## Authenticated Function Queries

### authenticated-function-calls.kql
**Purpose**: Track all authenticated function calls across the platform  
**Visualization**: Time chart  
**Use Case**: Monitor which authenticated endpoints are being accessed and when  
**Metrics**:
- Call count per hour by function
- Unique users per function
- Unique email domains

### authenticated-user-activity.kql
**Purpose**: Analyze user activity patterns across authenticated functions  
**Visualization**: Table  
**Use Case**: Identify power users, track user engagement, understand feature adoption  
**Metrics**:
- Total calls per user
- Unique functions accessed per user
- Days since first activity
- Last activity timestamp
- List of functions used

### authenticated-function-success.kql
**Purpose**: Monitor successful authenticated function operations  
**Visualization**: Time chart  
**Use Case**: Track successful API operations and usage patterns  
**Metrics**:
- Success count per hour by function
- Unique users per successful operation

### authenticated-function-errors.kql
**Purpose**: Analyze errors in authenticated functions  
**Visualization**: Time chart  
**Use Case**: Identify and troubleshoot authentication/authorization issues  
**Metrics**:
- Error count per hour by function and error type
- Affected users count
- Sample error messages

### authenticated-function-error-details.kql
**Purpose**: Detailed error analysis with full context  
**Visualization**: Table  
**Use Case**: Drill down into specific errors for troubleshooting  
**Fields**:
- Timestamp, Function name, Error type/message
- User ID and email domain
- Context fields (CustomerId, SubscriptionId, TestimonialId, Domain, etc.)

## Feature-Specific Queries

### testimonial-operations.kql
**Purpose**: Track testimonial CRUD operations  
**Visualization**: Time chart  
**Use Case**: Monitor testimonial management activity  
**Metrics**:
- Total operations, success/error counts
- Success rate percentage
- Unique users and testimonials
- Operations by function (Create/Update/Delete)

### subscription-management-operations.kql
**Purpose**: Track subscription-related authenticated operations  
**Visualization**: Time chart  
**Use Case**: Monitor subscription management by users  
**Metrics**:
- Total operations, success/error counts
- Success rate percentage
- Unique users and customers
- Operations by function (Find/List/Update/InvoicePreview)

## Using These Queries in Log Analytics

### In Azure Portal
1. Navigate to your Application Insights resource
2. Select "Logs" from the left menu
3. Copy and paste the desired KQL query
4. Click "Run" to execute
5. Use the visualization options to create charts

### In Log Analytics Workbooks
1. Navigate to your Application Insights resource
2. Select "Workbooks" from the left menu
3. Create a new workbook or edit an existing one
4. Add a new query section
5. Paste the KQL query from this directory
6. Configure the visualization type (time chart, table, bar chart, etc.)
7. Set the time range and refresh interval
8. Save the workbook

### Query Parameters
Most queries support these time-based filters:
```kql
| where timestamp > ago(24h)  // Last 24 hours
| where timestamp > ago(7d)   // Last 7 days
| where timestamp > ago(30d)  // Last 30 days
```

You can also filter by specific users or functions:
```kql
| where UserId == "specific-user-id"
| where FunctionName == "ListSubscription"
```

## Custom Dimensions Available

All authenticated function events track these dimensions:
- **FunctionName**: Name of the Azure Function
- **UserId**: User ID from JWT token (oid/sub claim)
- **UserEmailDomain**: Domain part of user email (for privacy)
- **Timestamp**: ISO 8601 timestamp of the event

Additional context-specific dimensions:
- **CustomerId**: Stripe customer ID (subscription operations)
- **SubscriptionId**: Stripe subscription ID
- **TestimonialId**: Testimonial document ID
- **Domain**: Domain name (for domain-related operations)
- **TargetEmail**: Target email for searches (domain only stored)
- **ErrorMessage**: Error description (for error events)
- **ErrorType**: Exception type or validation error type

## Event Types

Three main event types are tracked:
1. **AuthenticatedFunctionCall**: Every authenticated function invocation
2. **AuthenticatedFunctionSuccess**: Successful operation completion
3. **AuthenticatedFunctionError**: Errors with full context

## Privacy Considerations

The telemetry service follows privacy best practices:
- **Email addresses** are never logged in full - only the domain part is stored
- **User IDs** from JWT tokens are hashed identifiers, not personal information
- **Sensitive data** (passwords, tokens, keys) are never logged
- **PII** is minimized - only operational metadata is tracked

## Troubleshooting

### No Data Appearing
1. Verify Application Insights is configured and connected
2. Check that the authenticated functions have been called recently
3. Ensure the time range includes recent activity
4. Verify Application Insights instrumentation key is correct

### Incomplete Data
1. Check that all function apps have Application Insights enabled
2. Verify the `IAuthenticatedFunctionTelemetryService` is registered in DI
3. Ensure functions are calling the telemetry service methods

### Performance Considerations
- Queries with wide time ranges may be slow
- Use `summarize` and `bin()` to aggregate data
- Limit results with `take` or `top` operators
- Consider using materialized views for frequently-run queries

## Related Documentation
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [KQL Quick Reference](https://docs.microsoft.com/azure/data-explorer/kql-quick-reference)
- [Log Analytics Workbooks](https://docs.microsoft.com/azure/azure-monitor/visualize/workbooks-overview)
