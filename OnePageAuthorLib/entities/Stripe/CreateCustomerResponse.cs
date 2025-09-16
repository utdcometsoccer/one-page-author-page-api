using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.Entities.Stripe
{
    /// <summary>
    /// Represents the result of a request to create a Stripe customer.
    /// </summary>
    public class CreateCustomerResponse
    {
        public Customer? Customer { get; set; }
    }
}
