using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for ensuring user profiles exist for authenticated users.
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly ILogger<UserProfileService> _logger;
        private readonly IUserProfileRepository _profiles;

        public UserProfileService(ILogger<UserProfileService> logger, IUserProfileRepository profiles)
        {
            _logger = logger;
            _profiles = profiles;
        }

        /// <summary>
        /// Ensures a user profile exists for the authenticated user.
        /// Creates a new profile if one doesn't exist.
        /// </summary>
        /// <param name="user">The authenticated claims principal</param>
        /// <returns>The user profile</returns>
        /// <exception cref="InvalidOperationException">Thrown if user is not authenticated or UPN is missing</exception>
        public async Task<UserProfile> EnsureUserProfileAsync(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Unauthenticated user attempting to access user profile service.");
                throw new InvalidOperationException("User must be authenticated.");
            }

            string? oid = user.FindFirst("oid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? upn = user.FindFirst("preferred_username")?.Value
                           ?? user.FindFirst(ClaimTypes.Upn)?.Value
                           ?? user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(upn))
            {
                _logger.LogWarning("Authenticated user missing UPN/email; cannot create or retrieve UserProfile.");
                throw new InvalidOperationException("User profile key (UPN) not found.");
            }

            // Ensure profile exists
            var profile = await _profiles.GetByUpnAsync(upn);
            if (profile is null)
            {
                profile = new UserProfile(upn, oid);
                await _profiles.AddAsync(profile);
                _logger.LogInformation("Created UserProfile for UPN {Upn}", upn);
            }
            else
            {
                _logger.LogDebug("Found existing UserProfile for UPN {Upn}", upn);
            }

            return profile;
        }
    }
}