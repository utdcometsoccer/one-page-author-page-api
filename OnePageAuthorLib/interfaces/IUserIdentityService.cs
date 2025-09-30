using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service for extracting user identity information from claims.
    /// </summary>
    public interface IUserIdentityService
    {
        /// <summary>
        /// Extracts the User Principal Name (UPN) from the authenticated user's claims.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <returns>The user's UPN or email address</returns>
        /// <exception cref="InvalidOperationException">Thrown when user is not authenticated or required claims are missing</exception>
        string GetUserUpn(ClaimsPrincipal user);
    }
}