using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Service for validating user subscriptions.
    /// </summary>
    public class SubscriptionValidationService : ISubscriptionValidationService
    {
        private readonly ILogger<SubscriptionValidationService> _logger;
        private readonly IUserIdentityService _userIdentityService;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IListSubscriptions _listSubscriptions;

        public SubscriptionValidationService(
            ILogger<SubscriptionValidationService> logger,
            IUserIdentityService userIdentityService,
            IUserProfileRepository userProfileRepository,
            IListSubscriptions listSubscriptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userIdentityService = userIdentityService ?? throw new ArgumentNullException(nameof(userIdentityService));
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _listSubscriptions = listSubscriptions ?? throw new ArgumentNullException(nameof(listSubscriptions));
        }

        /// <summary>
        /// Validates that a user has at least one active subscription.
        /// Active subscriptions include those with status "active" or "trialing".
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <returns>True if the user has at least one active subscription, false otherwise</returns>
        public async Task<bool> HasValidSubscriptionAsync(ClaimsPrincipal user)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            _logger.LogInformation("Validating subscription for user {Upn}", upn);
            
            // Get user profile to retrieve Stripe customer ID
            var userProfile = await _userProfileRepository.GetByUpnAsync(upn);
            
            if (userProfile == null)
            {
                _logger.LogWarning("User profile not found for {Upn}", upn);
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(userProfile.StripeCustomerId))
            {
                _logger.LogWarning("User {Upn} has no Stripe customer ID", upn);
                return false;
            }
            
            try
            {
                // Get all subscriptions for the customer
                var subscriptionsResponse = await _listSubscriptions.ListAsync(
                    customerId: userProfile.StripeCustomerId,
                    status: "all",
                    limit: 100);
                
                // Check if any subscription is active or trialing
                var hasValidSubscription = subscriptionsResponse.Wrapper?.Items
                    .Any(s => s.Status == "active" || s.Status == "trialing") ?? false;
                
                if (hasValidSubscription)
                {
                    _logger.LogInformation(
                        "User {Upn} has valid subscription (customer: {CustomerId})",
                        upn, userProfile.StripeCustomerId);
                }
                else
                {
                    _logger.LogWarning(
                        "User {Upn} has no valid subscriptions (customer: {CustomerId})",
                        upn, userProfile.StripeCustomerId);
                }
                
                return hasValidSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error validating subscriptions for user {Upn} (customer: {CustomerId})",
                    upn, userProfile.StripeCustomerId);
                throw;
            }
        }
    }
}
