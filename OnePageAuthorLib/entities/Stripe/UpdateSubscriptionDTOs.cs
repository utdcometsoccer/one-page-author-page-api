namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    public class UpdateSubscriptionRequest
    {
        /// <summary>
        /// Existing subscription item id to update (if changing price/qty on an item). If omitted and PriceId provided, a new item may be added.
        /// </summary>
        public string? SubscriptionItemId { get; set; }

        /// <summary>
        /// New price id to set on the item.
        /// </summary>
        public string? PriceId { get; set; }

        /// <summary>
        /// Quantity to set on the item.
        /// </summary>
        public long? Quantity { get; set; }

        /// <summary>
        /// Optional proration behavior (create_prorations, always_invoice, none).
        /// </summary>
        public string? ProrationBehavior { get; set; }

        /// <summary>
        /// If true, cancels at period end.
        /// </summary>
        public bool? CancelAtPeriodEnd { get; set; }

        /// <summary>
        /// If true, expands latest invoice's payment intent in response mapping (if available).
        /// </summary>
        public bool ExpandLatestInvoicePaymentIntent { get; set; }
    }

    public class UpdateSubscriptionResponse
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LatestInvoiceId { get; set; } = string.Empty;
        public string LatestInvoicePaymentIntentId { get; set; } = string.Empty;
    }
}
