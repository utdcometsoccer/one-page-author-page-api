using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public static class CancelSubscriptionMappers
    {
        public static CancelSubscriptionResponse Map(Subscription? sub, string fallbackId)
        {
            return new CancelSubscriptionResponse
            {
                SubscriptionId = sub?.Id ?? fallbackId,
                Status = sub?.Status ?? string.Empty,
                CanceledAt = sub?.CanceledAt
            };
        }
    }
}
