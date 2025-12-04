using System;
using Stripe;

namespace OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Extracts PaymentIntent from a Stripe Invoice that may contain payments collection.
    /// </summary>
    public interface IInvoicePaymentIntentExtractor
    {
        /// <summary>
        /// Attempts to extract the PaymentIntent from the provided invoice.
        /// Returns null when not available or invoice is null.
        /// </summary>
        Task<PaymentIntent?> ExtractPaymentIntentAsync(Invoice? invoice);
    }
}
