# Authenticated Function Logging Implementation Summary

## Overview

This implementation adds extensive logging and telemetry tracking to all authenticated Azure Functions in the OnePageAuthor API platform. The logging infrastructure captures user context, operation details, success/failure metrics, and provides KQL queries for analysis in Log Analytics workbooks.

## What Was Added

### 1. Telemetry Service (`AuthenticatedFunctionTelemetryService`)

**Location**: `OnePageAuthorLib/api/AuthenticatedFunctionTelemetryService.cs`

A comprehensive telemetry service that tracks authenticated function activity with Application Insights custom events:

#### Features

- **User Context Extraction**: Automatically extracts user ID and email from JWT claims
- **Privacy-First Design**: Only stores email domain (not full email addresses)
- **Three Event Types**:
  - `AuthenticatedFunctionCall`: Tracks every function invocation
  - `AuthenticatedFunctionSuccess`: Tracks successful operations with metrics
  - `AuthenticatedFunctionError`: Tracks errors with full context

#### Methods

```csharp
public interface IAuthenticatedFunctionTelemetryService
{
    void TrackAuthenticatedFunctionCall(
        string functionName,
        string? userId,
        string? userEmail,
        Dictionary<string, string>? additionalProperties = null);

    void TrackAuthenticatedFunctionError(
        string functionName,
        string? userId,
        string? userEmail,
        string errorMessage,
        string? errorType = null,
        Dictionary<string, string>? additionalProperties = null);

    void TrackAuthenticatedFunctionSuccess(
        string functionName,
        string? userId,
        string? userEmail,
        Dictionary<string, string>? additionalProperties = null,
        Dictionary<string, double>? metrics = null);
}
```

#### Helper Methods

```csharp
// Extract user ID from various JWT claim types
public static string? ExtractUserId(ClaimsPrincipal? user)

// Extract user email from various JWT claim types
public static string? ExtractUserEmail(ClaimsPrincipal? user)
```

### 2. Updated Authenticated Functions

#### Stripe Functions (`InkStainedWretchStripe/`)

All authenticated Stripe functions now include comprehensive logging:

1. **FindSubscription.cs**
   - Tracks: User context, email/domain search parameters
   - Metrics: Subscription count, customer found status
   - Error tracking: Validation errors, operation failures

2. **ListSubscription.cs**
   - Tracks: User context, customer ID, filter parameters
   - Metrics: Subscription count, pagination status
   - Error tracking: All operation errors with context

3. **UpdateSubscription.cs**
   - Tracks: User context, subscription ID, update parameters
   - Context: Price changes, quantity changes, cancellation status
   - Error tracking: Validation and operation errors

4. **InvoicePreview.cs**
   - Tracks: User context, customer ID, preview parameters
   - Context: Subscription changes, price changes
   - Error tracking: Validation and operation errors

#### Testimonial Functions (`InkStainedWretchFunctions/`)

All testimonial management functions now include comprehensive logging:

1. **CreateTestimonial.cs**
   - Tracks: User context, testimonial details
   - Metrics: Testimonial ID, author name, rating
   - Error tracking: Validation errors, creation failures

2. **UpdateTestimonial.cs**
   - Tracks: User context, testimonial ID, update details
   - Metrics: Updated fields, rating changes
   - Error tracking: Validation errors, not found errors, update failures

3. **DeleteTestimonial.cs**
   - Tracks: User context, testimonial ID
   - Error tracking: Not found errors, deletion failures

### 3. Logging Pattern Used

Each authenticated function follows this pattern:

```csharp
public async Task<IActionResult> Run(HttpRequest req)
{
    // 1. Extract user context from JWT claims
    var user = req.HttpContext.User;
    var userId = AuthenticatedFunctionTelemetryService.ExtractUserId(user);
    var userEmail = AuthenticatedFunctionTelemetryService.ExtractUserEmail(user);

    // 2. Track function invocation
    _telemetry.TrackAuthenticatedFunctionCall(
        "FunctionName",
        userId,
        userEmail,
        additionalProperties);

    // 3. Validate inputs and track errors
    if (validation fails)
    {
        _telemetry.TrackAuthenticatedFunctionError(
            "FunctionName",
            userId,
            userEmail,
            "Error message",
            "ValidationError",
            context);
        return BadRequest();
    }

    try
    {
        // 4. Execute operation
        var result = await _service.DoWork();

        // 5. Track success with metrics
        _telemetry.TrackAuthenticatedFunctionSuccess(
            "FunctionName",
            userId,
            userEmail,
            properties,
            metrics);

        return Ok(result);
    }
    catch (Exception ex)
    {
        // 6. Track errors with full context
        _telemetry.TrackAuthenticatedFunctionError(
            "FunctionName",
            userId,
            userEmail,
            ex.Message,
            ex.GetType().Name,
            context);
        return InternalServerError();
    }
}
```

### 4. KQL Queries for Analysis

Seven comprehensive KQL queries were created in the `kql/` directory:

#### Core Queries

1. **authenticated-function-calls.kql**
   - Tracks all authenticated function calls over time
   - Shows: Call count, unique users, unique email domains
   - Visualization: Time chart

2. **authenticated-user-activity.kql**
   - Analyzes user engagement patterns
   - Shows: Total calls per user, functions used, activity timeline
   - Visualization: Table

3. **authenticated-function-success.kql**
   - Monitors successful operations
   - Shows: Success count, unique users per function
   - Visualization: Time chart

4. **authenticated-function-errors.kql**
   - Analyzes errors across functions
   - Shows: Error count by type, affected users, sample messages
   - Visualization: Time chart

5. **authenticated-function-error-details.kql**
   - Detailed error analysis for troubleshooting
   - Shows: Full error context with all metadata
   - Visualization: Table

#### Feature-Specific Queries

1. **testimonial-operations.kql**
   - Tracks Create/Update/Delete testimonial operations
   - Shows: Success rate, unique users, unique testimonials
   - Visualization: Time chart

2. **subscription-management-operations.kql**
   - Tracks Find/List/Update subscription operations
   - Shows: Success rate, unique users, unique customers
   - Visualization: Time chart

### 5. Documentation

**AUTHENTICATED_FUNCTIONS_README.md** provides:

- Query descriptions and use cases
- Instructions for using queries in Azure Portal and Log Analytics Workbooks
- Custom dimension reference
- Privacy considerations
- Troubleshooting guide

## Custom Dimensions Tracked

All events include these standard dimensions:

- **FunctionName**: Azure Function name
- **UserId**: User ID from JWT token (oid/sub/nameidentifier claim)
- **UserEmailDomain**: Domain part of user email (e.g., "example.com")
- **Timestamp**: ISO 8601 timestamp

Context-specific dimensions (as applicable):

- **CustomerId**: Stripe customer ID
- **SubscriptionId**: Stripe subscription ID
- **TestimonialId**: Testimonial document ID
- **Domain**: Domain name for searches
- **TargetEmail**: Target email domain for searches
- **ErrorMessage**: Error description
- **ErrorType**: Exception type or validation error category
- **Status**: Operation status or filter status
- **PriceId**: Stripe price ID
- **Rating**: Testimonial rating (1-5)

## Using the KQL Queries

### In Azure Portal

1. Navigate to Application Insights resource
2. Select "Logs" from left menu
3. Copy/paste desired query
4. Click "Run"
5. Configure visualization

### In Log Analytics Workbooks

1. Navigate to Application Insights
2. Select "Workbooks"
3. Create new workbook or edit existing
4. Add query section
5. Paste KQL query
6. Configure visualization (time chart, table, bar chart)
7. Set time range and refresh interval
8. Save workbook

### Example Filters

```kql
// Last 24 hours
| where timestamp > ago(24h)

// Specific user
| where UserId == "user-id-here"

// Specific function
| where FunctionName == "ListSubscription"

// Errors only
| where name == 'AuthenticatedFunctionError'
```

## Privacy & Security Considerations

The implementation follows privacy best practices:

1. **Email Privacy**: Only email domains are stored (not full addresses)
2. **User IDs**: JWT claim-based identifiers (not personally identifiable)
3. **No Sensitive Data**: Passwords, tokens, keys are never logged
4. **Minimal PII**: Only operational metadata is tracked
5. **Secure Claims**: User context extracted from validated JWT tokens

## Benefits

### For Operations

- Real-time monitoring of authenticated API usage
- User engagement metrics and patterns
- Error detection and alerting
- Performance monitoring per function

### For Support

- Detailed error context for troubleshooting
- User activity history for support cases
- Operation success/failure tracking

### For Product

- Feature usage analytics
- User behavior insights
- Subscription management patterns
- Testimonial management activity

### For Compliance

- Audit trail of authenticated operations
- User activity tracking
- Privacy-compliant logging

## Configuration

### Required Setup

1. **Application Insights** must be configured and connected
2. **Instrumentation Key** must be set in application configuration
3. **Service Registration**: `IAuthenticatedFunctionTelemetryService` is registered in DI via `ServiceFactory.AddStripeServices()`

### No Additional Configuration Needed

- Telemetry service is automatically injected into functions
- Custom events are automatically sent to Application Insights
- KQL queries work immediately once data is available

## Testing Recommendations

### Manual Testing

1. Call authenticated endpoints with valid JWT tokens
2. Verify telemetry appears in Application Insights (may take 2-3 minutes)
3. Run KQL queries in Log Analytics
4. Test various scenarios: success, validation errors, operation errors

### Validation

```kql
// Check for recent authenticated function calls
customEvents
| where name == 'AuthenticatedFunctionCall'
| where timestamp > ago(1h)
| order by timestamp desc
| take 10
```

### Workbook Creation

1. Create Log Analytics Workbook
2. Add sections for each query type
3. Configure auto-refresh (e.g., 5 minutes)
4. Set default time range (e.g., 24 hours)
5. Share with team

## Future Enhancements

Potential improvements for future iterations:

1. **Dashboard Creation**: Pre-built Azure Dashboard with key metrics
2. **Alerting Rules**: Automated alerts for error thresholds
3. **Power BI Integration**: Direct integration with Power BI for executive dashboards
4. **Performance Metrics**: Add execution time tracking
5. **Rate Limiting**: Track and alert on rate limit violations
6. **Cost Tracking**: Monitor API usage for billing/quota purposes

## Related Documentation

- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [KQL Quick Reference](https://docs.microsoft.com/azure/data-explorer/kql-quick-reference)
- [Log Analytics Workbooks](https://docs.microsoft.com/azure/azure-monitor/visualize/workbooks-overview)
- See `kql/AUTHENTICATED_FUNCTIONS_README.md` for detailed query documentation

## Questions or Issues?

For questions about the logging infrastructure or KQL queries, refer to:

- `kql/AUTHENTICATED_FUNCTIONS_README.md` - Detailed query documentation
- `OnePageAuthorLib/api/AuthenticatedFunctionTelemetryService.cs` - Implementation reference
- Azure Application Insights documentation for troubleshooting
