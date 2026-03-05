using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Authentication;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Validates OAuth scopes present in a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public class ScopeValidationService : IScopeValidationService
    {
        /// <summary>
        /// The short-form claim name used in raw JWT tokens for OAuth delegated scopes.
        /// </summary>
        /// <remarks>Alias of <see cref="AuthClaimTypes.Scp"/>.</remarks>
        public const string ScpClaimType = AuthClaimTypes.Scp;

        /// <summary>
        /// The URI-mapped claim type that <see cref="System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler"/>
        /// may remap the <c>scp</c> claim to when <c>MapInboundClaims</c> is enabled (the default).
        /// </summary>
        /// <remarks>Alias of <see cref="AuthClaimTypes.ScopeUri"/>.</remarks>
        public const string ScopeUriClaimType = AuthClaimTypes.ScopeUri;

        /// <inheritdoc />
        public bool HasRequiredScope(ClaimsPrincipal user, string requiredScope)
        {
            // JwtSecurityTokenHandler (with MapInboundClaims = true, the default) remaps the "scp"
            // JWT claim to the URI form.  Check both so that callers are insulated from the claim
            // type mapping behaviour of the token handler being used.
            var scopeTokens = user.FindAll(ScpClaimType)
                .Concat(user.FindAll(ScopeUriClaimType))
                .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return scopeTokens.Contains(requiredScope);
        }
    }
}
