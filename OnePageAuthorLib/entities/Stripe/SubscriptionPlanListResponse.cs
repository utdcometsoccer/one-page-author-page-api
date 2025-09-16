
namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// A response model analogous to StripePriceListResponse but containing SubscriptionPlan items.
    /// </summary>
    public class SubscriptionPlanListResponse
    {
        /// <summary>
        /// The list of subscription plans derived from Stripe prices.
        /// </summary>
        public List<SubscriptionPlan> Plans { get; set; } = new List<SubscriptionPlan>();

        /// <summary>
        /// Indicates whether there are more items available for pagination.
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// The last item id in the current page, useful for pagination.
        /// </summary>
        public string LastId { get; set; } = string.Empty;
    }
}
