using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Mapping helpers from Stripe entities to our DTOs. Kept simple for unit testing.
    /// </summary>
    public static class SubscriptionMappers
    {
        public static SubscriptionListResponse Map(IEnumerable<Subscription> data, bool hasMore)
        {
            var list = data?.ToList() ?? new List<Subscription>();
            return new SubscriptionListResponse
            {
                Items = list.Select(MapSubscription).ToList(),
                HasMore = hasMore,
                LastId = list.LastOrDefault()?.Id ?? string.Empty
            };
        }

        private static SubscriptionDto MapSubscription(Subscription s)
        {
            return new SubscriptionDto
            {
                Id = s.Id,
                Status = s.Status ?? string.Empty,
                CustomerId = s.CustomerId ?? string.Empty,
                LatestInvoiceId = s.LatestInvoiceId ?? string.Empty,
                // Some Stripe.NET versions expose PaymentIntent via expandable fields; avoid hard dependency
                // and leave it empty unless resolved elsewhere.
                LatestInvoicePaymentIntentId = string.Empty,
                Items = s.Items?.Data?.Select(MapItem).ToList() ?? new List<SubscriptionItemDto>()
            };
        }

        private static SubscriptionItemDto MapItem(SubscriptionItem i)
        {
            return new SubscriptionItemDto
            {
                PriceId = i.Price?.Id ?? string.Empty,
                ProductId = i.Price?.ProductId ?? string.Empty,
                Quantity = i.Quantity,
                UnitAmount = i.Price?.UnitAmount,
                Currency = i.Price?.Currency ?? string.Empty,
                Nickname = i.Price?.Nickname ?? string.Empty,
                RecurringInterval = i.Price?.Recurring?.Interval ?? string.Empty,
                RecurringIntervalCount = i.Price?.Recurring?.IntervalCount
            };
        }
    }
}
