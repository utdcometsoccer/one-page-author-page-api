namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the payload for creating a Stripe customer.
    /// </summary>
    public class CreateCustomerRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
