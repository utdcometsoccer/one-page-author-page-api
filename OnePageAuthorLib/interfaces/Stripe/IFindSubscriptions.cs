using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Service for finding subscriptions by customer email and domain name.
    /// </summary>
    public interface IFindSubscriptions
    {
        /// <summary>
        /// Finds subscriptions for a customer by email address and domain name.
        /// </summary>
        /// <param name="email">The customer's email address.</param>
        /// <param name="domainName">The domain name associated with the subscription.</param>
        /// <returns>A response containing matching subscriptions.</returns>
        Task<FindSubscriptionResponse> FindByEmailAndDomainAsync(string email, string domainName);
    }
}
