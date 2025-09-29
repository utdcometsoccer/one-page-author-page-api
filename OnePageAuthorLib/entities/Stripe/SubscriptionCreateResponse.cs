namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Response for creating a subscription, returning identifiers needed by the client.
    /// </summary>
    public class SubscriptionCreateResponse
    {
        /// <summary>
        /// The Stripe Subscription ID (e.g., sub_...).
        /// </summary>
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>
        /// The client secret associated with the subscription's payment.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
    }
}

