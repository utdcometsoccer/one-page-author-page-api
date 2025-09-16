namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the payload for creating a Stripe Checkout Session.
    /// </summary>
    public class CreateCheckoutSessionRequest
    {
        /// <summary>
        /// The base domain used to construct success and cancel URLs.
        /// For example: https://example.com
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// The existing Stripe Customer ID (e.g., cus_...).
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// The Stripe Price ID to purchase (e.g., price_...).
        /// </summary>
        public string PriceId { get; set; } = string.Empty;
    }
}
