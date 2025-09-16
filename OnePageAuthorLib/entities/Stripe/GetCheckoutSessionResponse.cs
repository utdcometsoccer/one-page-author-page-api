namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents details of a Stripe Checkout Session for retrieval endpoints.
    /// </summary>
    public class GetCheckoutSessionResponse
    {
        public string CheckoutSessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
