using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Application Insights telemetry service for tracking Stripe events.
    /// Emits custom events for Power BI dashboard integration.
    /// </summary>
    public class StripeTelemetryService : IStripeTelemetryService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<StripeTelemetryService> _logger;

        // Event name constants for consistent event naming
        private const string CustomerCreatedEvent = "StripeCustomerCreated";
        private const string CheckoutSessionCreatedEvent = "StripeCheckoutSessionCreated";
        private const string CheckoutSessionRetrievedEvent = "StripeCheckoutSessionRetrieved";
        private const string SubscriptionCreatedEvent = "StripeSubscriptionCreated";
        private const string SubscriptionCancelledEvent = "StripeSubscriptionCancelled";
        private const string SubscriptionUpdatedEvent = "StripeSubscriptionUpdated";
        private const string SubscriptionsListedEvent = "StripeSubscriptionsListed";
        private const string WebhookEventReceived = "StripeWebhookEvent";
        private const string InvoicePreviewEvent = "StripeInvoicePreview";
        private const string StripeErrorEvent = "StripeApiError";

        public StripeTelemetryService(TelemetryClient telemetryClient, ILogger<StripeTelemetryService> logger)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected internal virtual void TrackEvent(EventTelemetry telemetry)
        {
            _telemetryClient.TrackEvent(telemetry);
        }

        public void TrackCustomerCreated(string customerId, string? email = null)
        {
            var telemetry = new EventTelemetry(CustomerCreatedEvent);
            telemetry.Properties["CustomerId"] = customerId ?? string.Empty;
            if (!string.IsNullOrEmpty(email))
            {
                // Only store domain for privacy
                var domain = email.Contains('@') ? email.Split('@')[1] : "unknown";
                telemetry.Properties["EmailDomain"] = domain;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for customer {CustomerId}", CustomerCreatedEvent, customerId);
        }

        public void TrackCheckoutSessionCreated(string checkoutSessionId, string customerId, string? priceId = null)
        {
            var telemetry = new EventTelemetry(CheckoutSessionCreatedEvent);
            telemetry.Properties["CheckoutSessionId"] = checkoutSessionId ?? string.Empty;
            telemetry.Properties["CustomerId"] = customerId ?? string.Empty;
            if (!string.IsNullOrEmpty(priceId))
            {
                telemetry.Properties["PriceId"] = priceId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for checkout {CheckoutSessionId}", CheckoutSessionCreatedEvent, checkoutSessionId);
        }

        public void TrackCheckoutSessionRetrieved(string checkoutSessionId, string customerId, string? status = null, string? paymentStatus = null)
        {
            var telemetry = new EventTelemetry(CheckoutSessionRetrievedEvent);
            telemetry.Properties["CheckoutSessionId"] = checkoutSessionId ?? string.Empty;
            telemetry.Properties["CustomerId"] = customerId ?? string.Empty;
            if (!string.IsNullOrEmpty(status))
            {
                telemetry.Properties["Status"] = status;
            }
            if (!string.IsNullOrEmpty(paymentStatus))
            {
                telemetry.Properties["PaymentStatus"] = paymentStatus;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for checkout {CheckoutSessionId} with status {Status}", 
                CheckoutSessionRetrievedEvent, checkoutSessionId, status);
        }

        public void TrackSubscriptionCreated(string subscriptionId, string customerId, string? priceId = null)
        {
            var telemetry = new EventTelemetry(SubscriptionCreatedEvent);
            telemetry.Properties["SubscriptionId"] = subscriptionId ?? string.Empty;
            telemetry.Properties["CustomerId"] = customerId ?? string.Empty;
            if (!string.IsNullOrEmpty(priceId))
            {
                telemetry.Properties["PriceId"] = priceId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for subscription {SubscriptionId}", SubscriptionCreatedEvent, subscriptionId);
        }

        public void TrackSubscriptionCancelled(string subscriptionId, string? customerId = null)
        {
            var telemetry = new EventTelemetry(SubscriptionCancelledEvent);
            telemetry.Properties["SubscriptionId"] = subscriptionId ?? string.Empty;
            if (!string.IsNullOrEmpty(customerId))
            {
                telemetry.Properties["CustomerId"] = customerId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for subscription {SubscriptionId}", SubscriptionCancelledEvent, subscriptionId);
        }

        public void TrackSubscriptionUpdated(string subscriptionId, string? customerId = null, string? newPriceId = null)
        {
            var telemetry = new EventTelemetry(SubscriptionUpdatedEvent);
            telemetry.Properties["SubscriptionId"] = subscriptionId ?? string.Empty;
            if (!string.IsNullOrEmpty(customerId))
            {
                telemetry.Properties["CustomerId"] = customerId;
            }
            if (!string.IsNullOrEmpty(newPriceId))
            {
                telemetry.Properties["NewPriceId"] = newPriceId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for subscription {SubscriptionId}", SubscriptionUpdatedEvent, subscriptionId);
        }

        public void TrackSubscriptionsListed(string customerId, int count)
        {
            var telemetry = new EventTelemetry(SubscriptionsListedEvent);
            telemetry.Properties["CustomerId"] = customerId ?? string.Empty;
            telemetry.Properties["SubscriptionCount"] = count.ToString();
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for customer {CustomerId} with {Count} subscriptions", 
                SubscriptionsListedEvent, customerId, count);
        }

        public void TrackWebhookEvent(
            string eventType,
            string objectId,
            string? customerId = null,
            string? subscriptionId = null,
            string? invoiceId = null,
            string? paymentIntentId = null,
            string? priceId = null)
        {
            var telemetry = new EventTelemetry(WebhookEventReceived);
            telemetry.Properties["EventType"] = eventType ?? string.Empty;
            telemetry.Properties["ObjectId"] = objectId ?? string.Empty;
            
            if (!string.IsNullOrEmpty(customerId))
            {
                telemetry.Properties["CustomerId"] = customerId;
            }
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                telemetry.Properties["SubscriptionId"] = subscriptionId;
            }
            if (!string.IsNullOrEmpty(invoiceId))
            {
                telemetry.Properties["InvoiceId"] = invoiceId;
            }
            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                telemetry.Properties["PaymentIntentId"] = paymentIntentId;
            }
            if (!string.IsNullOrEmpty(priceId))
            {
                telemetry.Properties["PriceId"] = priceId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} of type {EventType} for object {ObjectId}", 
                WebhookEventReceived, eventType, objectId);
        }

        public void TrackInvoicePreview(string customerId, string? subscriptionId = null, string? newPriceId = null)
        {
            var telemetry = new EventTelemetry(InvoicePreviewEvent);
            telemetry.Properties["CustomerId"] = customerId ?? string.Empty;
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                telemetry.Properties["SubscriptionId"] = subscriptionId;
            }
            if (!string.IsNullOrEmpty(newPriceId))
            {
                telemetry.Properties["NewPriceId"] = newPriceId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for customer {CustomerId}", InvoicePreviewEvent, customerId);
        }

        public void TrackStripeError(string operation, string? errorCode = null, string? errorType = null, string? customerId = null)
        {
            var telemetry = new EventTelemetry(StripeErrorEvent);
            telemetry.Properties["Operation"] = operation ?? string.Empty;
            if (!string.IsNullOrEmpty(errorCode))
            {
                telemetry.Properties["ErrorCode"] = errorCode;
            }
            if (!string.IsNullOrEmpty(errorType))
            {
                telemetry.Properties["ErrorType"] = errorType;
            }
            if (!string.IsNullOrEmpty(customerId))
            {
                telemetry.Properties["CustomerId"] = customerId;
            }
            telemetry.Properties["Timestamp"] = DateTimeOffset.UtcNow.ToString("O");

            TrackEvent(telemetry);
            _logger.LogDebug("Tracked {EventName} for operation {Operation} with code {ErrorCode}", 
                StripeErrorEvent, operation, errorCode);
        }
    }
}
