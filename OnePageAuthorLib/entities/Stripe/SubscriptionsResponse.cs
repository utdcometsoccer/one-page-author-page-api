using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the result of a request to list Stripe subscriptions.
    /// </summary>
    public class SubscriptionsResponse
    {
        /// <summary>
        /// The Stripe subscriptions list returned by the API.
        /// </summary>
        public StripeList<Subscription>? Subscriptions { get; set; }

        /// <summary>
        /// Optional convenience wrapper for clients that prefer a simplified DTO.
        /// </summary>
        public SubscriptionListResponse? Wrapper { get; set; }
    }
}

