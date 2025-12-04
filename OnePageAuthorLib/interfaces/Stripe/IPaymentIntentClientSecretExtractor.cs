using Microsoft.Extensions.Logging;
using Stripe;

namespace OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Extracts client secret from an expanded Stripe PaymentIntent.
    /// </summary>
    public interface IPaymentIntentClientSecretExtractor
    {
        string Extract(PaymentIntent? paymentIntent);
    }
}
