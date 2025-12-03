using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Service for listing Stripe subscriptions following Stripe's sample patterns.
    /// </summary>
    public class ListSubscriptions : IListSubscriptions
    {
        private readonly ILogger<ListSubscriptions> _logger;
        private readonly StripeClient _stripeClient;
        public ListSubscriptions(ILogger<ListSubscriptions> logger, StripeClient stripeClient)
        {
            _logger = logger;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
        }

        /// <summary>
        /// Lists Stripe subscriptions with optional filters.
        /// </summary>
        /// <param name="customerId">Optional Stripe customer ID to filter subscriptions.</param>
        /// <param name="status">Optional subscription status filter (e.g., "all", "active", "trialing", "past_due").</param>
        /// <param name="limit">Optional page size (default 100).</param>
        /// <param name="startingAfter">Cursor for pagination (id of the last item in the previous page).</param>
        /// <param name="expandLatestInvoicePaymentIntent">Whether to expand latest invoice's payment intent for each item.</param>
        /// <returns>A <see cref="SubscriptionsResponse"/> containing the Stripe subscriptions list.</returns>
        public async Task<SubscriptionsResponse> ListAsync(
            string? customerId = null,
            string? status = null,
            int? limit = null,
            string? startingAfter = null,
            bool expandLatestInvoicePaymentIntent = false)
        {
            _logger.LogInformation(
                "Listing Stripe subscriptions. Customer={CustomerId}, Status={Status}, Limit={Limit}, StartingAfter={StartingAfter}, ExpandPI={Expand}",
                customerId, status, limit, startingAfter, expandLatestInvoicePaymentIntent);

            try
            {
                var service = new SubscriptionService(_stripeClient);
                var options = new SubscriptionListOptions
                {
                    Limit = limit ?? 100,
                };

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    options.Customer = customerId;
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    options.Status = status;
                }

                if (!string.IsNullOrWhiteSpace(startingAfter))
                {
                    options.StartingAfter = startingAfter;
                }

                if (expandLatestInvoicePaymentIntent)
                {
                    options.AddExpand("data.latest_invoice.payment_intent");
                }

                var subscriptions = await service.ListAsync(options);

                var wrapper = SubscriptionMappers.Map(subscriptions.Data, subscriptions.HasMore);

                return new SubscriptionsResponse
                {
                    Subscriptions = subscriptions,
                    Wrapper = wrapper
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error listing subscriptions. Status={Status} Code={Code} Type={Type}",
                    ex.HttpStatusCode, ex.StripeError?.Code, ex.StripeError?.Type);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing subscriptions");
                throw;
            }
        }
    }
}

