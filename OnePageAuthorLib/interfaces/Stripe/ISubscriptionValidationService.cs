using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe
{
    /// <summary>
    /// Interface for validating user subscriptions.
    /// </summary>
    public interface ISubscriptionValidationService
    {
        /// <summary>
        /// Validates that a user has at least one active subscription.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <returns>True if the user has at least one active subscription, false otherwise</returns>
        Task<bool> HasValidSubscriptionAsync(ClaimsPrincipal user);
    }
}
