using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Interface for validating user subscriptions.
    /// </summary>
    public interface ISubscriptionValidationService
    {
        /// <summary>
        /// Validates that a user has at least one active subscription for the specified domain.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="domainName">The domain name to validate the subscription for</param>
        /// <returns>True if the user has at least one active subscription for the domain, false otherwise</returns>
        Task<bool> HasValidSubscriptionAsync(ClaimsPrincipal user, string domainName);
    }
}
