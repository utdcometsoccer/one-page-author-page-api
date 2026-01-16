using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for extracting user identity information from claims.
    /// </summary>
    public class UserIdentityService : IUserIdentityService
    {
        const string emailClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        const string nameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        /// <summary>
        /// Extracts the User Principal Name (UPN) from the authenticated user's claims.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <returns>The user's UPN or email address</returns>
        /// <exception cref="InvalidOperationException">Thrown when user is not authenticated or required claims are missing</exception>
        public string GetUserUpn(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                throw new InvalidOperationException("User is not authenticated");

            // Try to get UPN first, then fall back to email if UPN is missing or empty
            var upn = GetNonEmptyClaimValue(user, "upn") 
                     ?? GetNonEmptyClaimValue(user, "email")
                     ?? GetNonEmptyClaimValue(user, nameClaimType)
                     ?? GetNonEmptyClaimValue(user, emailClaimType)
                     ?? GetNonEmptyClaimValue(user, "preferred_username")
                     ?? (string.IsNullOrWhiteSpace(user.Identity?.Name) ? null : user.Identity.Name);

            if (string.IsNullOrWhiteSpace(upn))
                throw new InvalidOperationException("User UPN or email claim is required");

            return upn;
        }

        /// <summary>
        /// Helper method to get a claim value that is not null or whitespace.
        /// </summary>
        /// <param name="user">The claims principal</param>
        /// <param name="claimType">The type of claim to retrieve</param>
        /// <returns>The claim value if it exists and is not empty/whitespace, otherwise null</returns>
        private static string? GetNonEmptyClaimValue(ClaimsPrincipal user, string claimType)
        {
            var claimValue = user.FindFirst(claimType)?.Value;
            return string.IsNullOrWhiteSpace(claimValue) ? null : claimValue;
        }
    }
}