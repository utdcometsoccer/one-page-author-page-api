using Stripe;

namespace OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Extracts the client secret from an expanded Stripe invoice's payment intent.
    /// </summary>
    public interface IClientSecretFromInvoice
    {
        /// <summary>
        /// Returns the client secret from the invoice's expanded <see cref="PaymentIntent"/>.
        /// The invoice must be retrieved with <c>Expand = ["payment_intent"]</c> so that
        /// When retrieved with expansion, the associated <see cref="Invoice"/> and <see cref="PaymentIntent"/> are populated.
        /// </summary>
        /// <param name="invoice">The Stripe invoice (may be null).</param>
        /// <returns>The client secret if available; otherwise null.</returns>
        Task<string> ExtractAsync(Invoice invoice);
    }
}
