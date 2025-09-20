using System.Text.Json.Serialization;

namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Lightweight DTO wrapper for Stripe subscriptions that is easier for clients to consume
    /// and includes pagination helpers.
    /// </summary>
    public class SubscriptionListResponse
    {
        /// <summary>
        /// The mapped subscription items.
        /// </summary>
        public List<SubscriptionDto> Items { get; set; } = new();

        /// <summary>
        /// Indicates whether there are more items available for pagination.
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// The last subscription id in the current page, useful for pagination cursors.
        /// </summary>
        public string LastId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Simplified subscription model exposing commonly used fields.
    /// </summary>
    public class SubscriptionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public string LatestInvoiceId { get; set; } = string.Empty;
        public string LatestInvoicePaymentIntentId { get; set; } = string.Empty;

        /// <summary>
        /// The subscription items (prices/products) on this subscription.
        /// </summary>
        public List<SubscriptionItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Simplified representation of a subscription item (price/product + quantity).
    /// </summary>
    public class SubscriptionItemDto
    {
        public string PriceId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public long? Quantity { get; set; }
        public long? UnitAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string RecurringInterval { get; set; } = string.Empty;
        public long? RecurringIntervalCount { get; set; }

        [JsonIgnore]
        public decimal AmountDecimal => UnitAmount.HasValue ? (decimal)UnitAmount.Value / 100m : 0m;
    }
}
