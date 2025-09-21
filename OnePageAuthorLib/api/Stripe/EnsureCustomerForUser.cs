using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
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

        public EnsureCustomerForUser(ILogger<EnsureCustomerForUser> logger, IUserProfileRepository profiles, ICreateCustomer creator)
        {
            _logger = logger;
            _profiles = profiles;
            _creator = creator;
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
            }

            return response ?? new CreateCustomerResponse();
        }
    }
}
