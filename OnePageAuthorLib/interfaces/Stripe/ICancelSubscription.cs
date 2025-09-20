using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface ICancelSubscription
    {
        Task<CancelSubscriptionResponse> CancelAsync(string subscriptionId, CancelSubscriptionRequest? request = null);
    }
}
