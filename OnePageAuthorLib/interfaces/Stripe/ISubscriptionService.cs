using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface ISubscriptionService
    {
        Task<SubscriptionCreateResponse> CreateAsync(CreateSubscriptionRequest request);
    }
}
