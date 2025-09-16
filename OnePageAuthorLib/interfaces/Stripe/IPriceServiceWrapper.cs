using System.Threading.Tasks;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IPriceServiceWrapper
    {
        Task<SubscriptionPlanListResponse> GetPricesAsync(PriceListRequest request);
        Task<SubscriptionPlan?> GetPriceByIdAsync(string priceId);
    }
}
