using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class UpdateSubscriptionService : IUpdateSubscription
    {
        private readonly ILogger<UpdateSubscriptionService> _logger;
        private readonly IStripeInvoiceServiceHelper _invoiceHelper;

        public UpdateSubscriptionService(ILogger<UpdateSubscriptionService> logger, IStripeInvoiceServiceHelper invoiceHelper)
        {
            _logger = logger;
            _invoiceHelper = invoiceHelper;
        }

        public async Task<UpdateSubscriptionResponse> UpdateAsync(string subscriptionId, UpdateSubscriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ArgumentException("subscriptionId is required", nameof(subscriptionId));
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                var svc = new SubscriptionService();
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

                if (request.ExpandLatestInvoicePaymentIntent && !string.IsNullOrWhiteSpace(response.LatestInvoiceId))
                {
                    var (piId, clientSecret) = await _invoiceHelper.TryGetPaymentIntentAsync(response.LatestInvoiceId);
                    response.LatestInvoicePaymentIntentId = piId;
                    // clientSecret is available if needed by callers later
                }

                return response;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error updating subscription {SubscriptionId}", subscriptionId);
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
