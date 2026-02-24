using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Service for validating OAuth scopes on a claims principal.
    /// </summary>
    public interface IScopeValidationService
    {
        /// <summary>
        /// Determines whether the authenticated user has the required scope.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal.</param>
        /// <param name="requiredScope">The scope value that must be present (e.g. "Author.Read").</param>
        /// <returns><c>true</c> if the user's <c>scp</c> claim contains <paramref name="requiredScope"/>; otherwise <c>false</c>.</returns>
        bool HasRequiredScope(ClaimsPrincipal user, string requiredScope);
    }
}
