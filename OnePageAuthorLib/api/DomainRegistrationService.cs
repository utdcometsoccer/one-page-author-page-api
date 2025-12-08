using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for managing domain registration operations for authenticated users.
    /// </summary>
    public class DomainRegistrationService : IDomainRegistrationService
    {
        private readonly ILogger<DomainRegistrationService> _logger;
        private readonly IDomainRegistrationRepository _repository;
        private readonly IUserIdentityService _userIdentityService;
        private readonly IDomainValidationService _domainValidationService;
        private readonly IContactInformationValidationService _contactValidationService;
        private readonly ISubscriptionValidationService _subscriptionValidationService;

        public DomainRegistrationService(
            ILogger<DomainRegistrationService> logger,
            IDomainRegistrationRepository repository,
            IUserIdentityService userIdentityService,
            IDomainValidationService domainValidationService,
            IContactInformationValidationService contactValidationService,
            ISubscriptionValidationService subscriptionValidationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userIdentityService = userIdentityService ?? throw new ArgumentNullException(nameof(userIdentityService));
            _domainValidationService = domainValidationService ?? throw new ArgumentNullException(nameof(domainValidationService));
            _contactValidationService = contactValidationService ?? throw new ArgumentNullException(nameof(contactValidationService));
            _subscriptionValidationService = subscriptionValidationService ?? throw new ArgumentNullException(nameof(subscriptionValidationService));
        }

        public async Task<DomainRegistration> CreateDomainRegistrationAsync(
            ClaimsPrincipal user, 
            Domain domain, 
            ContactInformation contactInformation)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            // Validate domain information first to ensure it's not null
            var domainValidationResult = _domainValidationService.ValidateDomain(domain);
            if (!domainValidationResult.IsValid)
            {
                var errorMessage = string.Join("; ", domainValidationResult.Errors);
                _logger.LogWarning("Domain validation failed for user {Upn}: {Errors}", upn, errorMessage);
                throw new ArgumentException($"Domain validation failed: {errorMessage}", nameof(domain));
            }
            
            // Validate that user has an active subscription for this domain
            var hasValidSubscription = await _subscriptionValidationService.HasValidSubscriptionAsync(user, domain.FullDomainName);
            if (!hasValidSubscription)
            {
                _logger.LogWarning("Domain registration denied for user {Upn}: No valid subscription for domain {DomainName}", upn, domain.FullDomainName);
                throw new InvalidOperationException($"A valid subscription is required to register the domain '{domain.FullDomainName}'");
            }
            
            // Validate contact information using the dedicated service
            var contactValidationResult = _contactValidationService.ValidateContactInformation(contactInformation);
            if (!contactValidationResult.IsValid)
            {
                var errorMessage = string.Join("; ", contactValidationResult.Errors);
                _logger.LogWarning("Contact information validation failed for user {Upn}: {Errors}", upn, errorMessage);
                throw new ArgumentException($"Contact information validation failed: {errorMessage}", nameof(contactInformation));
            }
            
            _logger.LogInformation("Creating domain registration for user {Upn}, domain {Domain}", 
                upn, domain.FullDomainName);

            var domainRegistration = new DomainRegistration(upn, domain, contactInformation);
            
            return await _repository.CreateAsync(domainRegistration);
        }

        public async Task<IEnumerable<DomainRegistration>> GetUserDomainRegistrationsAsync(ClaimsPrincipal user)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            _logger.LogInformation("Retrieving domain registrations for user {Upn}", upn);
            
            return await _repository.GetByUserAsync(upn);
        }

        public async Task<DomainRegistration?> GetDomainRegistrationByIdAsync(ClaimsPrincipal user, string registrationId)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            _logger.LogInformation("Retrieving domain registration {RegistrationId} for user {Upn}", 
                registrationId, upn);
            
            return await _repository.GetByIdAsync(registrationId, upn);
        }

        public async Task<DomainRegistration?> UpdateDomainRegistrationStatusAsync(
            ClaimsPrincipal user, 
            string registrationId, 
            DomainRegistrationStatus status)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            _logger.LogInformation("Updating domain registration {RegistrationId} status to {Status} for user {Upn}", 
                registrationId, status, upn);

            var existingRegistration = await _repository.GetByIdAsync(registrationId, upn);
            if (existingRegistration == null)
            {
                _logger.LogWarning("Domain registration {RegistrationId} not found for user {Upn}", 
                    registrationId, upn);
                return null;
            }

            existingRegistration.Status = status;
            
            return await _repository.UpdateAsync(existingRegistration);
        }


    }
}