using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Application Insights telemetry service for tracking Stripe events.
    /// Emits custom events for Power BI dashboard integration.
    /// </summary>
    public class StripeTelemetryService : IStripeTelemetryService
    {
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

        public StripeTelemetryService(ILogger<StripeTelemetryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected internal virtual void TrackEvent(string eventName, IReadOnlyDictionary<string, object?> properties)
        {
            using (_logger.BeginScope(properties))
            {
                _logger.LogInformation("TelemetryEvent {EventName}", eventName);
            }
        }

        public void TrackCustomerCreated(string customerId, string? email = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = CustomerCreatedEvent,
                ["CustomerId"] = customerId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(email))
            {
                var domain = email.Contains('@') ? email.Split('@')[1] : "unknown";
                properties["EmailDomain"] = domain;
            }

            TrackEvent(CustomerCreatedEvent, properties);
        }

        public void TrackCheckoutSessionCreated(string checkoutSessionId, string customerId, string? priceId = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = CheckoutSessionCreatedEvent,
                ["CheckoutSessionId"] = checkoutSessionId ?? string.Empty,
                ["CustomerId"] = customerId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(priceId))
            {
                properties["PriceId"] = priceId;
            }

            TrackEvent(CheckoutSessionCreatedEvent, properties);
        }

        public void TrackCheckoutSessionRetrieved(string checkoutSessionId, string customerId, string? status = null, string? paymentStatus = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = CheckoutSessionRetrievedEvent,
                ["CheckoutSessionId"] = checkoutSessionId ?? string.Empty,
                ["CustomerId"] = customerId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(status))
            {
                properties["Status"] = status;
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                properties["PaymentStatus"] = paymentStatus;
            }

            TrackEvent(CheckoutSessionRetrievedEvent, properties);
        }

        public void TrackSubscriptionCreated(string subscriptionId, string customerId, string? priceId = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = SubscriptionCreatedEvent,
                ["SubscriptionId"] = subscriptionId ?? string.Empty,
                ["CustomerId"] = customerId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(priceId))
            {
                properties["PriceId"] = priceId;
            }

            TrackEvent(SubscriptionCreatedEvent, properties);
        }

        public void TrackSubscriptionCancelled(string subscriptionId, string? customerId = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = SubscriptionCancelledEvent,
                ["SubscriptionId"] = subscriptionId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(customerId))
            {
                properties["CustomerId"] = customerId;
            }

            TrackEvent(SubscriptionCancelledEvent, properties);
        }

        public void TrackSubscriptionUpdated(string subscriptionId, string? customerId = null, string? newPriceId = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = SubscriptionUpdatedEvent,
                ["SubscriptionId"] = subscriptionId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(customerId))
            {
                properties["CustomerId"] = customerId;
            }

            if (!string.IsNullOrEmpty(newPriceId))
            {
                properties["NewPriceId"] = newPriceId;
            }

            TrackEvent(SubscriptionUpdatedEvent, properties);
        }

        public void TrackSubscriptionsListed(string customerId, int count)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = SubscriptionsListedEvent,
                ["CustomerId"] = customerId ?? string.Empty,
                ["SubscriptionCount"] = count.ToString(),
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            TrackEvent(SubscriptionsListedEvent, properties);
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
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = WebhookEventReceived,
                ["EventType"] = eventType ?? string.Empty,
                ["ObjectId"] = objectId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(customerId))
            {
                properties["CustomerId"] = customerId;
            }

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                properties["SubscriptionId"] = subscriptionId;
            }

            if (!string.IsNullOrEmpty(invoiceId))
            {
                properties["InvoiceId"] = invoiceId;
            }

            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                properties["PaymentIntentId"] = paymentIntentId;
            }

            if (!string.IsNullOrEmpty(priceId))
            {
                properties["PriceId"] = priceId;
            }

            TrackEvent(WebhookEventReceived, properties);
        }

        public void TrackInvoicePreview(string customerId, string? subscriptionId = null, string? newPriceId = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = InvoicePreviewEvent,
                ["CustomerId"] = customerId ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(subscriptionId))
            {
                properties["SubscriptionId"] = subscriptionId;
            }

            if (!string.IsNullOrEmpty(newPriceId))
            {
                properties["NewPriceId"] = newPriceId;
            }

            TrackEvent(InvoicePreviewEvent, properties);
        }

        public void TrackStripeError(string operation, string? errorCode = null, string? errorType = null, string? customerId = null)
        {
            var properties = new Dictionary<string, object?>
            {
                ["EventName"] = StripeErrorEvent,
                ["Operation"] = operation ?? string.Empty,
                ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O")
            };

            if (!string.IsNullOrEmpty(errorCode))
            {
                properties["ErrorCode"] = errorCode;
            }

            if (!string.IsNullOrEmpty(errorType))
            {
                properties["ErrorType"] = errorType;
            }

            if (!string.IsNullOrEmpty(customerId))
            {
                properties["CustomerId"] = customerId;
            }

            TrackEvent(StripeErrorEvent, properties);
        }
    }
}
