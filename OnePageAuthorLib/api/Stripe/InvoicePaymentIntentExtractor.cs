using Microsoft.Extensions.Logging;
using OnePageAuthorLib.Interfaces.Stripe;
using Stripe;

namespace OnePageAuthorLib.Api.Stripe
{
    /// <summary>
    /// Concrete implementation to traverse Stripe Invoice relations and extract PaymentIntent.
    /// Handles both legacy invoice.payment_intent and the newer payments collection.
    /// </summary>
    public class InvoicePaymentIntentExtractor : IInvoicePaymentIntentExtractor
    {
        private readonly ILogger<InvoicePaymentIntentExtractor> _logger;
        private readonly StripeClient _stripeClient;

        public InvoicePaymentIntentExtractor(ILogger<InvoicePaymentIntentExtractor> logger, StripeClient stripeClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
        }

        private async Task<PaymentIntent?> _paymentIntentHelper(InvoicePayment invoicePayment)
        {
            var paymentIntentService = new PaymentIntentService(_stripeClient);
            return invoicePayment.Payment switch
            {
                InvoicePaymentPayment pi => await paymentIntentService.GetAsync(pi.PaymentIntentId),
                _ => null
            };

        }
        public async Task<PaymentIntent?> ExtractPaymentIntentAsync(Invoice? invoice)
        {
            return invoice switch
            {
                null => null,
                _ => invoice.Payments switch
                {
                    null => null,
                    StripeList<InvoicePayment> payments => payments.Data switch
                    {
                        null => null,
                        { Count: 0 } => null,
                        List<InvoicePayment> data => data.FirstOrDefault() switch
                        {
                            InvoicePayment ip => await _paymentIntentHelper(ip),
                            _ => null
                        }
                    }
                }
            };
        }
    }
}
