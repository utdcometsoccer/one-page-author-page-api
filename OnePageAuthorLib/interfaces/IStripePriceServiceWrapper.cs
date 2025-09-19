using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Wrapper abstraction around <see cref="IStripePriceService"/> allowing decoration and composition.
    /// </summary>
    [Obsolete("Use InkStainedWretch.OnePageAuthorLib.API.Stripe.IPriceServiceWrapper in interfaces/Stripe instead.")]
    public interface IStripePriceServiceWrapper
    {
        Task<SubscriptionPlanListResponse> GetPricesAsync(PriceListRequest request);
        Task<SubscriptionPlan?> GetPriceByIdAsync(string priceId);
    }
}
