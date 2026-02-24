using System.Security.Claims;

namespace InkStainedWretch.OnePageAuthorAPI.Authentication;

/// <summary>
/// Determines whether an authenticated user possesses a given role.
/// </summary>
public interface IRoleChecker
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="user"/> has the specified <paramref name="role"/>.
    /// </summary>
    bool HasRole(ClaimsPrincipal user, string role);
}

/// <summary>
/// Default implementation that delegates to <see cref="JwtAuthenticationHelper.HasRole"/>,
/// covering all JWT claim-type representations used by Microsoft Entra ID.
/// </summary>
public class RoleChecker : IRoleChecker
{
    /// <inheritdoc/>
    public bool HasRole(ClaimsPrincipal user, string role)
        => JwtAuthenticationHelper.HasRole(user, role);
}
