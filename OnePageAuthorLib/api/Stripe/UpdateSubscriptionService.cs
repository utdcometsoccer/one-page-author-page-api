using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class UpdateSubscriptionService : IUpdateSubscription
    {
        private readonly ILogger<UpdateSubscriptionService> _logger;
        private readonly IStripeInvoiceServiceHelper _invoiceHelper;
        private readonly StripeClient _stripeClient;
        private readonly IStripeTelemetryService? _telemetryService;

        public UpdateSubscriptionService(ILogger<UpdateSubscriptionService> logger, IStripeInvoiceServiceHelper invoiceHelper, StripeClient stripeClient, IStripeTelemetryService? telemetryService = null)
        {
            _logger = logger;
            _invoiceHelper = invoiceHelper;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
            _telemetryService = telemetryService;
        }

        public async Task<UpdateSubscriptionResponse> UpdateAsync(string subscriptionId, UpdateSubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentException("subscriptionId is required", nameof(subscriptionId));
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                var svc = new SubscriptionService(_stripeClient);
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = request.CancelAtPeriodEnd
                };

                if (!string.IsNullOrWhiteSpace(request.ProrationBehavior))
                {
                    options.ProrationBehavior = request.ProrationBehavior;
                }

                if (!string.IsNullOrWhiteSpace(request.SubscriptionItemId) || !string.IsNullOrWhiteSpace(request.PriceId))
                {
                    options.Items = new List<SubscriptionItemOptions>();

                    if (!string.IsNullOrWhiteSpace(request.SubscriptionItemId))
                    {
                        options.Items.Add(new SubscriptionItemOptions
                        {
                            Id = request.SubscriptionItemId,
                            Price = request.PriceId,
                            Quantity = request.Quantity
                        });
                    }
                    else if (!string.IsNullOrWhiteSpace(request.PriceId))
                    {
                        options.Items.Add(new SubscriptionItemOptions
                        {
                            Price = request.PriceId,
                            Quantity = request.Quantity
                        });
                    }
                }

                if (request.ExpandLatestInvoicePaymentIntent)
                {
                    options.AddExpand("latest_invoice");
                    options.AddExpand("latest_invoice.payment_intent");
                }

                var sub = await svc.UpdateAsync(subscriptionId, options);

                var response = new UpdateSubscriptionResponse
                {
                    SubscriptionId = sub?.Id ?? subscriptionId,
                    Status = sub?.Status ?? string.Empty,
                    LatestInvoiceId = sub?.LatestInvoiceId ?? string.Empty,
                    LatestInvoicePaymentIntentId = string.Empty
                };

                // Note: Even if expansion is requested, we do not attempt to read expanded nested objects directly here.
                // Some SDK versions may not surface PaymentIntent directly on Invoice, so we always rely on the explicit hydration helper for consistent retrieval.

                // Fallback hydration: if PI is still missing but we have an invoice id, fetch via helper
                if (request.ExpandLatestInvoicePaymentIntent && string.IsNullOrWhiteSpace(response.LatestInvoicePaymentIntentId) && !string.IsNullOrWhiteSpace(response.LatestInvoiceId))
                {
                    var (piId, clientSecret) = await _invoiceHelper.TryGetPaymentIntentAsync(response.LatestInvoiceId);
                    response.LatestInvoicePaymentIntentId = piId;
                    // clientSecret returned if needed by callers later
                }

                // Track subscription update in Application Insights
                _telemetryService?.TrackSubscriptionUpdated(subscriptionId, sub?.CustomerId, request.PriceId);

                return response;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error updating subscription {SubscriptionId}", subscriptionId);
                _telemetryService?.TrackStripeError("UpdateSubscription", ex.StripeError?.Code, ex.StripeError?.Type);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }
    }
}
