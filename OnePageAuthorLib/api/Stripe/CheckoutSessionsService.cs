using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class CheckoutSessionsService : ICheckoutSessionService
    {
        private readonly ILogger<CheckoutSessionsService> _logger;

        public CheckoutSessionsService(ILogger<CheckoutSessionsService> logger)
        {
            _logger = logger;
        }

        public async Task<CreateCheckoutSessionResponse> CreateAsync(CreateCheckoutSessionRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Domain)) throw new ArgumentException("Domain is required", nameof(request.Domain));
            if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException("CustomerId is required", nameof(request.CustomerId));
            if (string.IsNullOrWhiteSpace(request.PriceId)) throw new ArgumentException("PriceId is required", nameof(request.PriceId));

            try
            {
                var service = new SessionService();
                var options = new SessionCreateOptions
                {
                    Mode = "subscription",
                    UiMode = "embedded",
                    Customer = request.CustomerId,
                    RedirectOnCompletion = "never",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = request.PriceId,
                            Quantity = 1
                        }
                    }
                };

                var session = await service.CreateAsync(options);
                return new CreateCheckoutSessionResponse
                {
                    CheckoutSessionId = session.Id ?? string.Empty,
                    ClientSecret = session.ClientSecret ?? string.Empty
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating checkout session. Status={Status} Code={Code} Type={Type}",
                    ex.HttpStatusCode, ex.StripeError?.Code, ex.StripeError?.Type);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating Stripe checkout session");
                throw;
            }
        }

        public async Task<GetCheckoutSessionResponse?> GetAsync(string checkoutSessionId)
        {
            if (string.IsNullOrWhiteSpace(checkoutSessionId))
                throw new ArgumentException("checkoutSessionId is required", nameof(checkoutSessionId));

            try
            {
                var service = new SessionService();
                var session = await service.GetAsync(checkoutSessionId);
                if (session == null) return null;

                return new GetCheckoutSessionResponse
                {
                    CheckoutSessionId = session.Id ?? string.Empty,
                    Status = session.Status ?? string.Empty,
                    PaymentStatus = session.PaymentStatus ?? string.Empty,
                    CustomerId = session.CustomerId ?? string.Empty,
                    Mode = session.Mode ?? string.Empty,
                    Url = session.Url ?? string.Empty
                };
            }
            catch (StripeException ex)
            {
                if (ex.StripeError?.Code == "resource_missing")
                {
                    _logger.LogWarning("Checkout session not found: {CheckoutSessionId}", checkoutSessionId);
                    return null;
                }
                _logger.LogError(ex, "Stripe error retrieving checkout session {CheckoutSessionId}", checkoutSessionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving checkout session {CheckoutSessionId}", checkoutSessionId);
                throw;
            }
        }
    }
}
