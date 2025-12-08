using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
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
        /// Validates that a user has at least one active subscription for the specified domain.
        /// Active subscriptions include those with status "active" or "trialing".
        /// The domain name must match the "domain_name" metadata in the subscription.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="domainName">The domain name to validate the subscription for</param>
        /// <returns>True if the user has at least one active subscription for the domain, false otherwise</returns>
        public async Task<bool> HasValidSubscriptionAsync(ClaimsPrincipal user, string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException("Domain name is required", nameof(domainName));
            }

            var upn = _userIdentityService.GetUserUpn(user);
            
            _logger.LogInformation("Validating subscription for user {Upn} and domain {DomainName}", upn, domainName);
            
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
                
                // Check if any subscription is active/trialing AND matches the domain in metadata
                var hasValidSubscription = false;
                if (subscriptionsResponse.Subscriptions?.Data != null)
                {
                    foreach (var subscription in subscriptionsResponse.Subscriptions.Data)
                    {
                        // Check if subscription is active or trialing
                        var isActiveStatus = subscription.Status == "active" || subscription.Status == "trialing";
                        
                        // Check if domain name matches metadata
                        var domainMatches = subscription.Metadata != null &&
                                          subscription.Metadata.TryGetValue("domain_name", out var metadataDomain) &&
                                          string.Equals(metadataDomain, domainName, StringComparison.OrdinalIgnoreCase);
                        
                        if (isActiveStatus && domainMatches)
                        {
                            hasValidSubscription = true;
                            _logger.LogInformation(
                                "User {Upn} has valid subscription {SubscriptionId} for domain {DomainName} (customer: {CustomerId})",
                                upn, subscription.Id, domainName, userProfile.StripeCustomerId);
                            break;
                        }
                    }
                }
                
                if (!hasValidSubscription)
                {
                    _logger.LogWarning(
                        "User {Upn} has no valid subscription for domain {DomainName} (customer: {CustomerId})",
                        upn, domainName, userProfile.StripeCustomerId);
                }
                
                return hasValidSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error validating subscriptions for user {Upn} and domain {DomainName} (customer: {CustomerId})",
                    upn, domainName, userProfile.StripeCustomerId);
                throw;
            }
        }
    }
}
