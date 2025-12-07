namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents a request to find subscriptions by customer email and domain name.
    /// </summary>
    public class FindSubscriptionRequest
    {
        /// <summary>
        /// The email address of the customer.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The domain name associated with the subscription.
        /// </summary>
        public string DomainName { get; set; } = string.Empty;
    }
}
