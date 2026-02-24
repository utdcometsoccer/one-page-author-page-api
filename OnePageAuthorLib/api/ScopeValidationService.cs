using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Validates OAuth scopes present in a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public class ScopeValidationService : IScopeValidationService
    {
        /// <inheritdoc />
        public bool HasRequiredScope(ClaimsPrincipal user, string requiredScope)
        {
            var scopeTokens = user.FindAll("scp")
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return scopeTokens.Contains(requiredScope);
        }
    }
}
