using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// No-op implementation of ISubscriptionValidationService that always returns true.
    /// Used when Stripe API is not configured, allowing domain registrations without subscription validation.
    /// </summary>
    public class NoOpSubscriptionValidationService : ISubscriptionValidationService
    {
        private readonly ILogger<NoOpSubscriptionValidationService> _logger;

        public NoOpSubscriptionValidationService(ILogger<NoOpSubscriptionValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Always returns true to allow operations without subscription validation.
        /// Logs a warning to indicate that subscription validation is disabled.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal</param>
        /// <param name="domainName">The domain name to validate the subscription for</param>
        /// <returns>Always returns true</returns>
        public Task<bool> HasValidSubscriptionAsync(ClaimsPrincipal user, string domainName)
        {
            _logger.LogWarning(
                "Subscription validation is disabled (Stripe API not configured). Allowing operation for domain: {DomainName}",
                domainName);
            return Task.FromResult(true);
        }
    }
}
