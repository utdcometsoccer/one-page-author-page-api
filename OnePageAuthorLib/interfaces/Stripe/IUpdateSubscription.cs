using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IUpdateSubscription
    {
        Task<UpdateSubscriptionResponse> UpdateAsync(string subscriptionId, UpdateSubscriptionRequest request);
    }
}
