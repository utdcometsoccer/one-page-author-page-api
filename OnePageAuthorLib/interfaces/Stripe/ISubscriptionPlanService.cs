using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Service for mapping Stripe prices to subscription plans with features from Stripe products.
    /// </summary>
    public interface ISubscriptionPlanService
    {
        /// <summary>
        /// Maps a PriceDto to a SubscriptionPlan with features retrieved from Stripe.
        /// </summary>
        /// <param name="priceDto">The price information from Stripe</param>
        /// <returns>A subscription plan with populated features from Stripe product data</returns>
        Task<SubscriptionPlan> MapToSubscriptionPlanAsync(PriceDto priceDto);

        /// <summary>
        /// Maps multiple PriceDtos to SubscriptionPlans with features retrieved from Stripe.
        /// </summary>
        /// <param name="priceDtos">The price information list from Stripe</param>
        /// <returns>A list of subscription plans with populated features from Stripe product data</returns>
        Task<List<SubscriptionPlan>> MapToSubscriptionPlansAsync(IEnumerable<PriceDto> priceDtos);
    }
}