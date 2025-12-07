namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the response from finding subscriptions by customer email and domain name.
    /// </summary>
    public class FindSubscriptionResponse
    {
        /// <summary>
        /// The Stripe customer ID found for the given email.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// The list of subscriptions matching the criteria.
        /// </summary>
        public List<SubscriptionDto> Subscriptions { get; set; } = new();

        /// <summary>
        /// Indicates whether a customer was found with the given email.
        /// </summary>
        public bool CustomerFound { get; set; }

        /// <summary>
        /// Indicates whether any subscriptions were found for the domain.
        /// </summary>
        public bool SubscriptionsFound => Subscriptions.Count > 0;
    }
}
