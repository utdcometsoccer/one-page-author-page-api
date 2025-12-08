using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorLib.API.Stripe;
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
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IListSubscriptions _listSubscriptions;

        public DomainRegistrationService(
            ILogger<DomainRegistrationService> logger,
            IDomainRegistrationRepository repository,
            IUserIdentityService userIdentityService,
            IDomainValidationService domainValidationService,
            IContactInformationValidationService contactValidationService,
            IUserProfileRepository userProfileRepository,
            IListSubscriptions listSubscriptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userIdentityService = userIdentityService ?? throw new ArgumentNullException(nameof(userIdentityService));
            _domainValidationService = domainValidationService ?? throw new ArgumentNullException(nameof(domainValidationService));
            _contactValidationService = contactValidationService ?? throw new ArgumentNullException(nameof(contactValidationService));
            _userProfileRepository = userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
            _listSubscriptions = listSubscriptions ?? throw new ArgumentNullException(nameof(listSubscriptions));
        }

        public async Task<DomainRegistration> CreateDomainRegistrationAsync(
            ClaimsPrincipal user, 
            Domain domain, 
            ContactInformation contactInformation)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            // Validate domain information using the dedicated service
            var domainValidationResult = _domainValidationService.ValidateDomain(domain);
            if (!domainValidationResult.IsValid)
            {
                var errorMessage = string.Join("; ", domainValidationResult.Errors);
                _logger.LogWarning("Domain validation failed for user {Upn}: {Errors}", upn, errorMessage);
                throw new ArgumentException($"Domain validation failed: {errorMessage}", nameof(domain));
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

        public async Task<DomainRegistration?> UpdateDomainRegistrationAsync(
            ClaimsPrincipal user,
            string registrationId,
            Domain? domain = null,
            ContactInformation? contactInformation = null,
            DomainRegistrationStatus? status = null)
        {
            var upn = _userIdentityService.GetUserUpn(user);
            
            _logger.LogInformation("Updating domain registration {RegistrationId} for user {Upn}", 
                registrationId, upn);

            // Validate subscription first
            await ValidateUserSubscriptionAsync(upn);

            // Get existing registration
            var existingRegistration = await _repository.GetByIdAsync(registrationId, upn);
            if (existingRegistration == null)
            {
                _logger.LogWarning("Domain registration {RegistrationId} not found for user {Upn}", 
                    registrationId, upn);
                return null;
            }

            // Update domain if provided
            if (domain != null)
            {
                // Validate domain information using the dedicated service
                var domainValidationResult = _domainValidationService.ValidateDomain(domain);
                if (!domainValidationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", domainValidationResult.Errors);
                    _logger.LogWarning("Domain validation failed for user {Upn}: {Errors}", upn, errorMessage);
                    throw new ArgumentException($"Domain validation failed: {errorMessage}", nameof(domain));
                }
                
                existingRegistration.Domain = domain;
            }

            // Update contact information if provided
            if (contactInformation != null)
            {
                // Validate contact information using the dedicated service
                var contactValidationResult = _contactValidationService.ValidateContactInformation(contactInformation);
                if (!contactValidationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", contactValidationResult.Errors);
                    _logger.LogWarning("Contact information validation failed for user {Upn}: {Errors}", upn, errorMessage);
                    throw new ArgumentException($"Contact information validation failed: {errorMessage}", nameof(contactInformation));
                }
                
                existingRegistration.ContactInformation = contactInformation;
            }

            // Update status if provided
            if (status.HasValue)
            {
                existingRegistration.Status = status.Value;
            }

            return await _repository.UpdateAsync(existingRegistration);
        }

        /// <summary>
        /// Validates that the user has an active subscription.
        /// </summary>
        /// <param name="upn">User Principal Name</param>
        /// <exception cref="InvalidOperationException">Thrown if user doesn't have an active subscription</exception>
        private async Task ValidateUserSubscriptionAsync(string upn)
        {
            _logger.LogInformation("Validating subscription for user {Upn}", upn);
            
            // Get user profile to retrieve Stripe customer ID
            var userProfile = await _userProfileRepository.GetByUpnAsync(upn);
            if (userProfile == null || string.IsNullOrWhiteSpace(userProfile.StripeCustomerId))
            {
                _logger.LogWarning("User {Upn} does not have a Stripe customer ID", upn);
                throw new InvalidOperationException("User does not have an active subscription. Please subscribe to update domain registrations.");
            }

            // Check if user has any active subscriptions
            var subscriptionsResponse = await _listSubscriptions.ListAsync(
                customerId: userProfile.StripeCustomerId,
                status: "active",
                limit: 1);

            if (subscriptionsResponse?.Subscriptions?.Data == null || !subscriptionsResponse.Subscriptions.Data.Any())
            {
                _logger.LogWarning("User {Upn} does not have any active subscriptions", upn);
                throw new InvalidOperationException("User does not have an active subscription. Please subscribe to update domain registrations.");
            }

            _logger.LogInformation("User {Upn} has active subscription validated", upn);
        }


    }
}