namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Optional settings when cancelling a subscription.
    /// </summary>
    public class CancelSubscriptionRequest
    {
        /// <summary>
        /// If true, creates a final invoice for any unbilled usage.
        /// </summary>
        public bool? InvoiceNow { get; set; }

        /// <summary>
        /// If true, prorates when cancelling mid-period (if applicable).
        /// </summary>
        public bool? Prorate { get; set; }
    }

    /// <summary>
    /// Result of cancelling a subscription.
    /// </summary>
    public class CancelSubscriptionResponse
    {
        public string SubscriptionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? CanceledAt { get; set; }
    }
}
