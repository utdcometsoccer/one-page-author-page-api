using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Collections.Generic;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class SubscriptionsService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionsService> _logger;
        private readonly StripeClient _stripeClient;

        public SubscriptionsService(ILogger<SubscriptionsService> logger, StripeClient stripeClient)
        {
            _logger = logger;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
        }

        public async Task<SubscriptionCreateResponse> CreateAsync(CreateSubscriptionRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.PriceId)) throw new ArgumentException("PriceId is required", nameof(request.PriceId));
            if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException("CustomerId is required", nameof(request.CustomerId));

            try
            {
                string clientSecret = string.Empty;
                var svc = new SubscriptionService(_stripeClient);
                var options = new SubscriptionCreateOptions
                {
                    Customer = request.CustomerId,

                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions { Price = request.PriceId }
                    },
                    // With default_incomplete, the subscription is created and requires payment confirmation
                    PaymentBehavior = "default_incomplete"
                };


                var subscription = await svc.CreateAsync(options);
                var invoiceSvc = new InvoiceService(_stripeClient);
                var invoice = await invoiceSvc.GetAsync(subscription.LatestInvoiceId);
                var invoicePaymentService = new InvoicePaymentService(_stripeClient);
                var invoicePaymentOptions = new InvoicePaymentListOptions
                {
                    Invoice = invoice.Id,
                    Limit = 1
                };
                var invoicePayment = (await invoicePaymentService.ListAsync(invoicePaymentOptions)).FirstOrDefault();
                if (invoicePayment?.Payment?.PaymentIntentId is { } paymentIntentId)
                {
                    var paymentIntentService = new PaymentIntentService(_stripeClient);
                    var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
                    clientSecret = paymentIntent.ClientSecret;
                }
                else
                {
                    _logger.LogWarning("No payment found on invoice {InvoiceId} while creating subscription {SubscriptionId}", invoice.Id, subscription.Id);
                }
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
