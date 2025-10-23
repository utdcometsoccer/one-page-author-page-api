using System.Security.Claims;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;

namespace InkStainedWretch.OnePageAuthorAPI.API.ImageServices
{
    /// <summary>
    /// Service for determining user's image storage tier from Cosmos DB memberships.
    /// Designed specifically for Personal Microsoft Account apps which cannot use app roles.
    /// Automatically assigns new users to the "Starter" tier.
    /// </summary>
    public class ImageStorageTierService : IImageStorageTierService
    {
        private readonly ILogger<ImageStorageTierService> _logger;
        private readonly IImageStorageTierRepository _tierRepository;
        private readonly IImageStorageTierMembershipRepository _membershipRepository;

        public ImageStorageTierService(
            ILogger<ImageStorageTierService> logger,
            IImageStorageTierRepository tierRepository,
            IImageStorageTierMembershipRepository membershipRepository)
        {
            _logger = logger;
            _tierRepository = tierRepository;
            _membershipRepository = membershipRepository;
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
            _logger.LogInformation("Getting tier for user {UserProfileId} (Personal Microsoft Account app - using Cosmos DB only)", userProfileId);

            // Get all tiers
            var allTiers = await _tierRepository.GetAllAsync();
            if (allTiers == null || !allTiers.Any())
            {
                _logger.LogError("No image storage tiers found in database");
                return null;
            }

            // Personal Microsoft Account apps cannot have app roles, so we skip JWT role checking
            // and go directly to Cosmos DB membership lookup
            
            try
            {
                var membership = await _membershipRepository.GetForUserAsync(userProfileId);
                if (membership != null)
                {
                    var membershipTier = await _tierRepository.GetByIdAsync(membership.TierId);
                    if (membershipTier != null)
                    {
                        _logger.LogInformation("User {UserProfileId} has Cosmos DB tier membership: {TierName}", userProfileId, membershipTier.Name);
                        return membershipTier;
                    }
                    else
                    {
                        _logger.LogWarning("User {UserProfileId} has membership for tier {TierId} but tier not found", userProfileId, membership.TierId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query Cosmos DB membership for user {UserProfileId}", userProfileId);
            }

            // No existing membership found - assign default tier for new users
            _logger.LogInformation("User {UserProfileId} has no tier assignment, creating default membership", userProfileId);
            
            // Try to find "Starter" tier first
            var defaultTier = await _tierRepository.GetByNameAsync("Starter");
            if (defaultTier == null)
            {
                // If Starter doesn't exist, get the lowest cost tier
                defaultTier = allTiers.OrderBy(t => t.CostInDollars).FirstOrDefault();
            }

            if (defaultTier != null)
            {
                // Create a new membership for the user
                try
                {
                    var newMembership = new ImageStorageTierMembership
                    {
                        id = Guid.NewGuid().ToString(),
                        UserProfileId = userProfileId,
                        TierId = defaultTier.id,
                        StorageUsedInBytes = 0,
                        BandwidthUsedInBytes = 0
                    };

                    await _membershipRepository.AddAsync(newMembership);
                    _logger.LogInformation("Created new {TierName} membership for user {UserProfileId}", defaultTier.Name, userProfileId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create default membership for user {UserProfileId}, continuing with tier assignment", userProfileId);
                }

                return defaultTier;
            }
            else
            {
                _logger.LogError("No default tier could be determined for user {UserProfileId}", userProfileId);
                return null;
            }
        }
    }
}
