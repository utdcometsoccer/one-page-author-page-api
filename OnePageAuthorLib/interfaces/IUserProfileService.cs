using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Interface for user profile management services.
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// Ensures a user profile exists for the authenticated user.
        /// Creates a new profile if one doesn't exist.
        /// </summary>
        /// <param name="user">The authenticated claims principal</param>
        /// <returns>The user profile</returns>
        /// <exception cref="InvalidOperationException">Thrown if user is not authenticated or UPN is missing</exception>
        Task<UserProfile> EnsureUserProfileAsync(ClaimsPrincipal user);
    }
}