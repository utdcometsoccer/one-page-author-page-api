using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class CancelSubscriptionService : ICancelSubscription
    {
        private readonly ILogger<CancelSubscriptionService> _logger;
        private readonly StripeClient _stripeClient;
        private readonly IStripeTelemetryService? _telemetryService;

        public CancelSubscriptionService(ILogger<CancelSubscriptionService> logger, StripeClient stripeClient, IStripeTelemetryService? telemetryService = null)
        {
            _logger = logger;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
            _telemetryService = telemetryService;
        }

        public async Task<CancelSubscriptionResponse> CancelAsync(string subscriptionId, CancelSubscriptionRequest? request = null)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentException("subscriptionId is required", nameof(subscriptionId));

            try
            {
                var svc = new SubscriptionService(_stripeClient);
                var options = new SubscriptionCancelOptions();

                if (request?.InvoiceNow is not null)
                {
                    options.InvoiceNow = request.InvoiceNow;
                }

                if (request?.Prorate is not null)
                {
                    options.Prorate = request.Prorate;
                }

                var sub = await svc.CancelAsync(subscriptionId, options);

                // Track subscription cancellation in Application Insights
                _telemetryService?.TrackSubscriptionCancelled(subscriptionId, sub.CustomerId);

                return CancelSubscriptionMappers.Map(sub, subscriptionId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error cancelling subscription {SubscriptionId}", subscriptionId);
                _telemetryService?.TrackStripeError("CancelSubscription", ex.StripeError?.Code, ex.StripeError?.Type);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error cancelling subscription {SubscriptionId}", subscriptionId);
                throw;
            }
        }
    }
}
