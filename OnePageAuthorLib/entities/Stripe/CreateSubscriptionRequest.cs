namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the payload for creating a subscription from a Stripe price.
    /// </summary>
    public class CreateSubscriptionRequest
    {
        /// <summary>
        /// The Stripe Price ID to subscribe to (e.g., price_...).
        /// </summary>
        public string PriceId { get; set; } = string.Empty;

        /// <summary>
        /// The Stripe Customer ID (e.g., cus_...). Required by Stripe to create a subscription.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;
    }
}

