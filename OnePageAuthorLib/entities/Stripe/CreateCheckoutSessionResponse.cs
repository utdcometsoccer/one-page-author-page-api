namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the response payload after creating a Stripe Checkout Session.
    /// </summary>
    public class CreateCheckoutSessionResponse
    {
        /// <summary>
        /// The client secret for the Stripe Checkout Session.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// The Stripe Checkout Session ID (e.g., cs_...).
        /// </summary>
        public string CheckoutSessionId { get; set; } = string.Empty;
    }
}
