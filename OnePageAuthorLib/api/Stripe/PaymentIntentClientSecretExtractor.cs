using Microsoft.Extensions.Logging;
using OnePageAuthorLib.Interfaces.Stripe;
using Stripe;

namespace OnePageAuthorLib.Api.Stripe
{
    public class PaymentIntentClientSecretExtractor : IPaymentIntentClientSecretExtractor
    {
        private readonly ILogger<PaymentIntentClientSecretExtractor> _logger;

        public PaymentIntentClientSecretExtractor(ILogger<PaymentIntentClientSecretExtractor> logger)
        {
            _logger = logger;
        }

        public string Extract(PaymentIntent? paymentIntent)
        {
            // Favor the original switch-style pattern for readability and consistency
            switch (paymentIntent)
            {
                case null:
                    _logger.LogWarning("PaymentIntent is null; ensure 'latest_invoice.payment_intent' expansion is requested");
                    return string.Empty;

                default:
                    // Nested switch for the client secret state
                    switch (string.IsNullOrWhiteSpace(paymentIntent.ClientSecret))
                    {
                        case true:
                            _logger.LogInformation("PaymentIntent {PaymentIntentId} has no client secret available", paymentIntent.Id);
                            return string.Empty;
                        case false:
                            _logger.LogDebug("Extracted client secret from PaymentIntent {PaymentIntentId}", paymentIntent.Id);
                            return paymentIntent.ClientSecret!;
                    }
            }
        }
    }
}
