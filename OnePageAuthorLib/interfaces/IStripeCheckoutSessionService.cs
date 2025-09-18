using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Abstraction for creating Stripe Checkout Sessions.
    /// </summary>
    [Obsolete("Use InkStainedWretch.OnePageAuthorLib.API.Stripe.ICheckoutSessionService in interfaces/Stripe instead.")]
    public interface IStripeCheckoutSessionService
    {
        /// <summary>
        /// Creates a Stripe Checkout Session for the provided request.
        /// </summary>
    Task<CreateCheckoutSessionResponse> CreateAsync(CreateCheckoutSessionRequest request);

        /// <summary>
        /// Retrieves a Stripe Checkout Session details by its identifier.
        /// </summary>

        Task<GetCheckoutSessionResponse?> GetAsync(string checkoutSessionId);
    }
}
