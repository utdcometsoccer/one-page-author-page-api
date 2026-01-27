# Stripe Payment Dashboard Implementation Plan

## Overview

This document outlines the implementation plan for the Power BI audit trail for Stripe events. The solution integrates Application Insights telemetry with Stripe operations to provide comprehensive visibility into the user purchase journey and subscription lifecycle.

## Architecture

### Components

1. **StripeTelemetryService** - Central service for emitting Application Insights custom events
2. **IStripeTelemetryService Interface** - Contract for telemetry operations
3. **KQL Queries** - Pre-built queries for Power BI dashboards
4. **Integration Points** - Stripe services instrumented with telemetry

### Data Flow

```
User Action → Stripe API → Service Layer → StripeTelemetryService → Application Insights → Power BI
```

## Implementation Details

### 1. Telemetry Service Interface

Location: `OnePageAuthorLib/interfaces/Stripe/IStripeTelemetryService.cs`

The interface defines methods for tracking:

- Customer creation events
- Checkout session creation and retrieval
- Subscription lifecycle (create, update, cancel, list)
- Webhook events
- Invoice previews
- API errors

### 2. Telemetry Service Implementation

Location: `OnePageAuthorLib/api/Stripe/StripeTelemetryService.cs`

Custom event names:

- `StripeCustomerCreated` - Customer registration
- `StripeCheckoutSessionCreated` - Checkout initiation
- `StripeCheckoutSessionRetrieved` - Checkout status check
- `StripeSubscriptionCreated` - New subscription
- `StripeSubscriptionCancelled` - Subscription cancellation
- `StripeSubscriptionUpdated` - Subscription modification
- `StripeSubscriptionsListed` - Subscription queries
- `StripeWebhookEvent` - Incoming webhook events
- `StripeInvoicePreview` - Invoice preview requests
- `StripeApiError` - API failures

### 3. Event Properties

Each event includes relevant properties:

| Property | Description | Events |
|----------|-------------|--------|
| CustomerId | Stripe customer ID | All events |
| SubscriptionId | Subscription identifier | Subscription events |
| CheckoutSessionId | Checkout session ID | Checkout events |
| InvoiceId | Invoice identifier | Invoice/webhook events |
| PaymentIntentId | Payment intent ID | Webhook events |
| PriceId | Price/plan ID | Subscription/checkout events |
| EventType | Stripe event type | Webhook events |
| Status | Session/subscription status | Various |
| PaymentStatus | Payment outcome | Checkout events |
| ErrorCode | Stripe error code | Error events |
| ErrorType | Stripe error type | Error events |
| Operation | Failed operation name | Error events |
| Timestamp | Event timestamp | All events |

### 4. Integrated Services

The following services are instrumented with telemetry:

| Service | Events Tracked |
|---------|----------------|
| `CreateCustomer` | Customer creation |
| `CheckoutSessionsService` | Checkout creation, retrieval |
| `SubscriptionsService` | Subscription creation |
| `CancelSubscriptionService` | Subscription cancellation |
| `UpdateSubscriptionService` | Subscription updates |
| `ListSubscriptions` | Subscription queries |
| `InvoicePreviewService` | Invoice previews |
| `StripeWebhookHandler` | All webhook events |

## Power BI Dashboard Design

### Dashboard Pages

#### 1. Purchase Journey Overview

**Purpose:** Track users from registration through successful payment

**Visualizations:**

- Customer funnel (Created → Checkout → Subscribed)
- Average journey duration
- Drop-off points analysis

**KQL Query:** `stripe-purchase-journey.kql`

#### 2. Subscription Trends

**Purpose:** Monitor subscription growth and churn

**Visualizations:**

- Daily/weekly subscription creates vs cancels
- Net subscription growth over time
- Plan distribution changes

**KQL Query:** `stripe-subscription-trends.kql`

#### 3. Payment Analysis

**Purpose:** Track payment success rates and failures

**Visualizations:**

- Payment success/failure ratio (pie chart)
- Failed payment trends
- Revenue by payment status

**KQL Query:** `stripe-payment-outcomes.kql`

#### 4. Customer Lifecycle

**Purpose:** Understand customer stages distribution

**Visualizations:**

- Funnel chart of lifecycle stages
- Stage conversion rates
- Time-in-stage analysis

**KQL Query:** `stripe-customer-lifecycle.kql`

#### 5. Plan Popularity

**Purpose:** Identify most popular subscription plans

**Visualizations:**

- Plan selection distribution (pie chart)
- Plan trends over time
- Upgrade/downgrade patterns

**KQL Query:** `stripe-plan-popularity.kql`

#### 6. System Health

**Purpose:** Monitor API errors and system stability

**Visualizations:**

- Error rate over time
- Error distribution by operation
- Most common error codes

**KQL Query:** `stripe-api-errors.kql`

### Key Metrics

| Metric | Description | Calculation |
|--------|-------------|-------------|
| Conversion Rate | Checkout → Subscription | Subscriptions / Checkouts * 100 |
| Churn Rate | Cancellations / Active Subscriptions | Monthly cancels / Start-of-month active |
| Payment Success Rate | Successful / Total Payments | Paid invoices / Total invoices * 100 |
| Average Journey Time | Registration to First Payment | Avg(FirstPayment - CustomerCreated) |
| Daily Active Customers | Unique customers per day | dcount(CustomerId) per day |

## KQL Queries

All queries are located in the `/kql` folder:

| File | Purpose |
|------|---------|
| `stripe-purchase-journey.kql` | Purchase journey timeline |
| `stripe-subscription-trends.kql` | Subscription lifecycle trends |
| `stripe-payment-outcomes.kql` | Payment success/failure analysis |
| `stripe-customer-lifecycle.kql` | Customer lifecycle stages |
| `stripe-plan-popularity.kql` | Plan selection distribution |
| `stripe-api-errors.kql` | API error analysis |
| `stripe-webhook-events.kql` | Webhook event summary |
| `stripe-daily-active-customers.kql` | Daily active customer count |

## Configuration

### Application Insights Setup

1. Ensure Application Insights is configured in the Azure Functions host:

   ```json
   {
     "logging": {
       "applicationInsights": {
         "samplingSettings": {
           "isEnabled": false
         }
       }
     }
   }
   ```

2. Set the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable

### Service Registration

The telemetry service is automatically registered in `ServiceFactory.AddStripeServices()`:

```csharp
services.AddScoped<IStripeTelemetryService, StripeTelemetryService>();
```

## Privacy Considerations

- Email addresses are stored as domains only (e.g., `gmail.com` instead of `user@gmail.com`)
- Customer IDs are Stripe identifiers, not personal information
- No payment card details are ever logged
- All data follows Stripe's data handling requirements

## Deployment

1. Deploy updated Azure Functions with telemetry integration
2. Verify Application Insights events appear in Azure Portal
3. Import KQL queries into Power BI
4. Configure Power BI dashboard refresh schedule (recommended: every 15 minutes)

## Monitoring and Alerts

Consider setting up alerts for:

- High API error rates (> 5% failure rate)
- Unusual churn spikes
- Payment failure increases
- Webhook delivery failures

## Future Enhancements

1. **Revenue Tracking** - Add monetary values to events for revenue dashboards
2. **Cohort Analysis** - Track customer cohorts over time
3. **A/B Testing** - Track conversion by experiment variant
4. **Geographic Analysis** - Add location data for regional insights
5. **Real-time Streaming** - Use Azure Stream Analytics for real-time dashboards
