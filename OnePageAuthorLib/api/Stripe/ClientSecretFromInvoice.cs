using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OnePageAuthorLib.Interfaces.Stripe;
using Stripe;

namespace OnePageAuthorLib.Api.Stripe
{
    /// <summary>
    /// Implementation that safely extracts a client secret from an expanded invoice.
    /// </summary>
    public class ClientSecretFromInvoice : IClientSecretFromInvoice
    {
        private readonly ILogger<ClientSecretFromInvoice> _logger;
        private readonly IPaymentIntentClientSecretExtractor _paymentIntentExtractor;
        private readonly IInvoicePaymentIntentExtractor _invoicePaymentIntentExtractor;

        public ClientSecretFromInvoice(ILogger<ClientSecretFromInvoice> logger, IPaymentIntentClientSecretExtractor paymentIntentExtractor, IInvoicePaymentIntentExtractor invoicePaymentIntentExtractor)
        {
            _logger = logger;
            _paymentIntentExtractor = paymentIntentExtractor ?? throw new ArgumentNullException(nameof(paymentIntentExtractor));
            _invoicePaymentIntentExtractor = invoicePaymentIntentExtractor ?? throw new ArgumentNullException(nameof(invoicePaymentIntentExtractor));
        }

        public async Task<string> ExtractAsync(Invoice? invoice)
        {
            // Basic context for observability
            _logger.LogInformation(
                "Extracting client secret from invoice. InvoiceId={InvoiceId}, PaymentsCount={PaymentsCount}",
                invoice?.Id,
            invoice?.Payments?.Data?.Count ?? 0);

            return invoice switch
            {
                null => HandleNullInvoice(),
                _ => invoice.Payments switch
                {
                    null => HandleNullInvoice(),
                    StripeList<InvoicePayment> payments => payments.Data switch
                    {
                        null => HandleNullInvoice(),
                        { Count: 0 } => HandleNullInvoice(),
                        List<InvoicePayment> data => await _invoicePaymentIntentExtractor.ExtractPaymentIntentAsync(invoice) switch
                        {
                            null => HandleNullInvoice(),
                            PaymentIntent paymentIntent => _paymentIntentExtractor.Extract(paymentIntent)
                        }
                    }
                }

            };
        }

        private string HandleNullInvoice()
        {
            throw new InvalidOperationException("PaymentIntent is not expanded or no payments found on invoice. Ensure Expand: ['latest_invoice.payment_intent'] when creating/retrieving the subscription/invoice.");
        }
    }
}
