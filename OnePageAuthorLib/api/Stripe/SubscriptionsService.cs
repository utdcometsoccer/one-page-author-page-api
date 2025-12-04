using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;
using OnePageAuthorLib.Interfaces.Stripe;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class SubscriptionsService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionsService> _logger;
        private readonly StripeClient _stripeClient;
        private readonly IClientSecretFromInvoice _clientSecretFromInvoice;
        private readonly IStripeTelemetryService? _telemetryService;

        public SubscriptionsService(ILogger<SubscriptionsService> logger, StripeClient stripeClient, IClientSecretFromInvoice clientSecretFromInvoice, IStripeTelemetryService? telemetryService = null)
        {
            _logger = logger;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
            _clientSecretFromInvoice = clientSecretFromInvoice ?? throw new ArgumentNullException(nameof(clientSecretFromInvoice));
            _telemetryService = telemetryService;
        }

        public async Task<SubscriptionCreateResponse> CreateAsync(CreateSubscriptionRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.PriceId)) throw new ArgumentException("PriceId is required", nameof(request.PriceId));
            if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException("CustomerId is required", nameof(request.CustomerId));

            try
            {
                string clientSecret = string.Empty;
                var subscriptionService = new SubscriptionService(_stripeClient);
                var options = new SubscriptionCreateOptions
                {
                    Customer = request.CustomerId,

                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions { Price = request.PriceId }
                    },
                    // With default_incomplete, the subscription is created and requires payment confirmation
                    PaymentBehavior = "default_incomplete",
                    Expand = new List<string> {
                        "latest_invoice",
                        "latest_invoice.payments",
                        "latest_invoice.payment_intent"
                         }
                };

                Subscription subscription = await subscriptionService.CreateAsync(options);
                // LatestInvoice is populated when expanded via 'latest_invoice.payment_intent'
                clientSecret = subscription.LatestInvoice switch
                {
                    Invoice latestInvoiceObject => await _clientSecretFromInvoice.ExtractAsync(latestInvoiceObject),
                    _ => throw new InvalidOperationException($"Subscription {subscription.Id} has no latest invoice (LatestInvoice was null). Ensure Expand option is set.")
                };

                // Track subscription creation in Application Insights
                _telemetryService?.TrackSubscriptionCreated(
                    subscription.Id,
                    request.CustomerId,
                    request.PriceId);

                return new SubscriptionCreateResponse
                {
                    SubscriptionId = subscription.Id,
                    ClientSecret = clientSecret
                };


            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating subscription. Status={Status} Code={Code} Type={Type}",
                    ex.HttpStatusCode, ex.StripeError?.Code, ex.StripeError?.Type);
                _telemetryService?.TrackStripeError("CreateSubscription", ex.StripeError?.Code, ex.StripeError?.Type, request.CustomerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating subscription");
                throw;
            }
        }
    }
}
