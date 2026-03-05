namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

/// <summary>
/// Shared constants for JWT claim type names used in OAuth/OpenID Connect tokens,
/// including both the raw JWT form and the URI-mapped form produced by
/// <see cref="System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler"/> when
/// <c>MapInboundClaims</c> is enabled (the default).
/// </summary>
public static class AuthClaimTypes
{
    /// <summary>
    /// The short-form claim name (<c>scp</c>) used in raw JWT tokens for OAuth delegated scopes.
    /// </summary>
    public const string Scp = "scp";

    /// <summary>
    /// The URI-mapped claim type that <see cref="System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler"/>
    /// remaps the <c>scp</c> claim to when <c>MapInboundClaims</c> is enabled (the default).
    /// </summary>
    public const string ScopeUri = "http://schemas.microsoft.com/identity/claims/scope";
}
