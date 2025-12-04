namespace InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Service interface for tracking Stripe events in Application Insights telemetry.
    /// Enables Power BI dashboards for tracking user purchase journeys and trends.
    /// </summary>
    public interface IStripeTelemetryService
    {
        /// <summary>
        /// Tracks a customer creation event.
        /// </summary>
        /// <param name="customerId">The Stripe customer ID.</param>
        /// <param name="email">The customer email address.</param>
        void TrackCustomerCreated(string customerId, string? email = null);

        /// <summary>
        /// Tracks a checkout session creation event.
        /// </summary>
        /// <param name="checkoutSessionId">The checkout session ID.</param>
        /// <param name="customerId">The Stripe customer ID.</param>
        /// <param name="priceId">The price ID for the checkout.</param>
        void TrackCheckoutSessionCreated(string checkoutSessionId, string customerId, string? priceId = null);

        /// <summary>
        /// Tracks a checkout session retrieval event.
        /// </summary>
        /// <param name="checkoutSessionId">The checkout session ID.</param>
        /// <param name="customerId">The Stripe customer ID.</param>
        /// <param name="status">The checkout session status.</param>
        /// <param name="paymentStatus">The payment status.</param>
        void TrackCheckoutSessionRetrieved(string checkoutSessionId, string customerId, string? status = null, string? paymentStatus = null);

        /// <summary>
        /// Tracks a subscription creation event.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="customerId">The Stripe customer ID.</param>
        /// <param name="priceId">The price ID.</param>
        void TrackSubscriptionCreated(string subscriptionId, string customerId, string? priceId = null);

        /// <summary>
        /// Tracks a subscription cancellation event.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="customerId">The customer ID (if available).</param>
        void TrackSubscriptionCancelled(string subscriptionId, string? customerId = null);

        /// <summary>
        /// Tracks a subscription update event.
        /// </summary>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="customerId">The customer ID (if available).</param>
        /// <param name="newPriceId">The new price ID (if changed).</param>
        void TrackSubscriptionUpdated(string subscriptionId, string? customerId = null, string? newPriceId = null);

        /// <summary>
        /// Tracks subscriptions being listed for a customer.
        /// </summary>
        /// <param name="customerId">The Stripe customer ID.</param>
        /// <param name="count">Number of subscriptions returned.</param>
        void TrackSubscriptionsListed(string customerId, int count);

        /// <summary>
        /// Tracks a Stripe webhook event received.
        /// </summary>
        /// <param name="eventType">The Stripe event type (e.g., invoice.paid).</param>
        /// <param name="objectId">The object ID from the event.</param>
        /// <param name="customerId">The customer ID (if available).</param>
        /// <param name="subscriptionId">The subscription ID (if available).</param>
        /// <param name="invoiceId">The invoice ID (if available).</param>
        /// <param name="paymentIntentId">The payment intent ID (if available).</param>
        /// <param name="priceId">The price ID (if available).</param>
        void TrackWebhookEvent(
            string eventType,
            string objectId,
            string? customerId = null,
            string? subscriptionId = null,
            string? invoiceId = null,
            string? paymentIntentId = null,
            string? priceId = null);

        /// <summary>
        /// Tracks an invoice preview request.
        /// </summary>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="subscriptionId">The subscription ID.</param>
        /// <param name="newPriceId">The new price ID being previewed.</param>
        void TrackInvoicePreview(string customerId, string? subscriptionId = null, string? newPriceId = null);

        /// <summary>
        /// Tracks a Stripe API error.
        /// </summary>
        /// <param name="operation">The operation that failed (e.g., CreateSubscription).</param>
        /// <param name="errorCode">The Stripe error code.</param>
        /// <param name="errorType">The Stripe error type.</param>
        /// <param name="customerId">The customer ID (if available).</param>
        void TrackStripeError(string operation, string? errorCode = null, string? errorType = null, string? customerId = null);
    }
}
