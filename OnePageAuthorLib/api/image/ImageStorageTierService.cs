using System.Security.Claims;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices
{
    /// <summary>
    /// Service for determining user's image storage tier from Entra ID roles.
    /// </summary>
    public class ImageStorageTierService : IImageStorageTierService
    {
        private readonly ILogger<ImageStorageTierService> _logger;
        private readonly IImageStorageTierRepository _tierRepository;

        public ImageStorageTierService(
            ILogger<ImageStorageTierService> logger,
            IImageStorageTierRepository tierRepository)
        {
            _logger = logger;
            _tierRepository = tierRepository;
        }

        public async Task<ImageStorageTier?> GetUserTierAsync(ClaimsPrincipal user)
        {
            var userProfileId = user.FindFirst("oid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userProfileId))
            {
                _logger.LogWarning("User profile ID not found in claims");
                return null;
            }

            var roles = user.FindAll("roles").Select(c => c.Value).ToArray();
            return await GetUserTierByRolesAsync(userProfileId, roles);
        }

        public async Task<ImageStorageTier?> GetUserTierByRolesAsync(string userProfileId, string[] roles)
        {
            _logger.LogInformation("Getting tier for user {UserProfileId} with {RoleCount} roles", userProfileId, roles.Length);

            // Get all tiers
            var allTiers = await _tierRepository.GetAllAsync();
            if (!allTiers.Any())
            {
                _logger.LogError("No image storage tiers found in database");
                return null;
            }

            // Check if user has any ImageStorageTier role
            ImageStorageTier? userTier = null;
            foreach (var role in roles)
            {
                if (role.StartsWith("ImageStorageTier.", StringComparison.OrdinalIgnoreCase))
                {
                    var tierName = role.Substring("ImageStorageTier.".Length);
                    userTier = await _tierRepository.GetByNameAsync(tierName);
                    
                    if (userTier != null)
                    {
                        _logger.LogInformation("User {UserProfileId} has tier role: {TierName}", userProfileId, tierName);
                        return userTier;
                    }
                    else
                    {
                        _logger.LogWarning("User {UserProfileId} has role {Role} but tier not found in database", userProfileId, role);
                    }
                }
            }

            // User has no tier role - assign default tier
            _logger.LogInformation("User {UserProfileId} has no tier role, assigning default tier", userProfileId);
            
            // Try to find "Starter" tier first
            var defaultTier = await _tierRepository.GetByNameAsync("Starter");
            if (defaultTier != null)
            {
                _logger.LogInformation("Assigned Starter tier to user {UserProfileId}", userProfileId);
                return defaultTier;
            }

            // If Starter doesn't exist, get the lowest cost tier
            defaultTier = allTiers.OrderBy(t => t.CostInDollars).FirstOrDefault();
            if (defaultTier != null)
            {
                _logger.LogInformation("Assigned lowest cost tier '{TierName}' to user {UserProfileId}", defaultTier.Name, userProfileId);
            }
            else
            {
                _logger.LogError("No default tier could be determined for user {UserProfileId}", userProfileId);
            }

            return defaultTier;
        }
    }
}
