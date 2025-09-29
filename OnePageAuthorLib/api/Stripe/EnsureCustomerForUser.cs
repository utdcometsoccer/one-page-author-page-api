using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.API.ImageAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;
using Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class EnsureCustomerForUser : IEnsureCustomerForUser
    {
        private readonly ILogger<EnsureCustomerForUser> _logger;
        private readonly IUserProfileRepository _profiles;
        private readonly ICreateCustomer _creator;
        private readonly IImageStorageTierRepository _tierRepository;
        private readonly IImageStorageTierMembershipRepository _membershipRepository;

        public EnsureCustomerForUser(ILogger<EnsureCustomerForUser> logger, IUserProfileRepository profiles, ICreateCustomer creator, IImageStorageTierRepository tierRepository, IImageStorageTierMembershipRepository membershipRepository)
        {
            _logger = logger;
            _profiles = profiles;
            _creator = creator;
            _tierRepository = tierRepository;
            _membershipRepository = membershipRepository;
        }

        public async Task<CreateCustomerResponse> EnsureAsync(ClaimsPrincipal user, CreateCustomerRequest request)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Unauthenticated user attempting to ensure Stripe customer.");
                throw new InvalidOperationException("User must be authenticated.");
            }
            if (request is null) throw new ArgumentNullException(nameof(request));

            string? oid = user.FindFirst("oid")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? upn = user.FindFirst("preferred_username")?.Value
                           ?? user.FindFirst(ClaimTypes.Upn)?.Value
                           ?? user.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(upn))
            {
                _logger.LogWarning("Authenticated user missing UPN/email; cannot partition UserProfile.");
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

            // Safeguard: return existing Stripe customer id
            if (!string.IsNullOrWhiteSpace(profile.StripeCustomerId))
            {
                _logger.LogInformation("UPN {Upn} already linked to Stripe customer {CustomerId}", upn, profile.StripeCustomerId);
                return new CreateCustomerResponse { Customer = new Customer { Id = profile.StripeCustomerId } };
            }

            // Create via inner creator
            var response = _creator.Execute(request);
            if (response?.Customer?.Id is string id && !string.IsNullOrWhiteSpace(id))
            {
                profile.StripeCustomerId = id;
                await _profiles.UpdateAsync(profile);
                _logger.LogInformation("Linked UPN {Upn} to Stripe customer {CustomerId}", upn, id);

                // Enroll the new customer in the free image storage tier if available
                if (!string.IsNullOrWhiteSpace(profile.id))
                {
                    await EnrollInFreeTierAsync(profile.id);
                }
            }

            return response ?? new CreateCustomerResponse();
        }

        private async Task EnrollInFreeTierAsync(string userProfileId)
        {
            try
            {
                // Check if user already has a tier membership
                var existingMembership = await _membershipRepository.GetForUserAsync(userProfileId);
                if (existingMembership != null)
                {
                    _logger.LogInformation("User {UserProfileId} already has image storage tier membership", userProfileId);
                    return;
                }

                // Find the free tier (cost = 0)
                var allTiers = await _tierRepository.GetAllAsync();
                var freeTier = allTiers.FirstOrDefault(t => t.CostInDollars == 0);

                if (freeTier == null)
                {
                    _logger.LogWarning("No free image storage tier found (CostInDollars = 0)");
                    return;
                }

                // Create membership for the user
                var membership = new ImageStorageTierMembership
                {
                    id = Guid.NewGuid().ToString(),
                    TierId = freeTier.id,
                    UserProfileId = userProfileId
                };

                await _membershipRepository.AddAsync(membership);
                _logger.LogInformation("Enrolled user {UserProfileId} in free image storage tier {TierId} ({TierName})",
                    userProfileId, freeTier.id, freeTier.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enroll user {UserProfileId} in free image storage tier", userProfileId);
                // Don't throw - this is a nice-to-have feature and shouldn't break customer creation
            }
        }
    }
}
