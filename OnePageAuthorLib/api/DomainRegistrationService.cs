using System.Security.Claims;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
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

        public DomainRegistrationService(
            ILogger<DomainRegistrationService> logger,
            IDomainRegistrationRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<DomainRegistration> CreateDomainRegistrationAsync(
            ClaimsPrincipal user, 
            Domain domain, 
            ContactInformation contactInformation)
        {
            var upn = GetUserUpn(user);
            
            // Validate domain information first
            ValidateDomain(domain);
            ValidateContactInformation(contactInformation);
            
            _logger.LogInformation("Creating domain registration for user {Upn}, domain {Domain}", 
                upn, $"{domain.SecondLevelDomain}.{domain.TopLevelDomain}");

            var domainRegistration = new DomainRegistration(upn, domain, contactInformation);
            
            return await _repository.CreateAsync(domainRegistration);
        }

        public async Task<IEnumerable<DomainRegistration>> GetUserDomainRegistrationsAsync(ClaimsPrincipal user)
        {
            var upn = GetUserUpn(user);
            
            _logger.LogInformation("Retrieving domain registrations for user {Upn}", upn);
            
            return await _repository.GetByUserAsync(upn);
        }

        public async Task<DomainRegistration?> GetDomainRegistrationByIdAsync(ClaimsPrincipal user, string registrationId)
        {
            var upn = GetUserUpn(user);
            
            _logger.LogInformation("Retrieving domain registration {RegistrationId} for user {Upn}", 
                registrationId, upn);
            
            return await _repository.GetByIdAsync(registrationId, upn);
        }

        public async Task<DomainRegistration?> UpdateDomainRegistrationStatusAsync(
            ClaimsPrincipal user, 
            string registrationId, 
            DomainRegistrationStatus status)
        {
            var upn = GetUserUpn(user);
            
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

        private static string GetUserUpn(ClaimsPrincipal user)
        {
            if (!user.Identity?.IsAuthenticated == true)
                throw new InvalidOperationException("User is not authenticated");

            var upn = user.FindFirst("upn")?.Value ?? user.FindFirst("email")?.Value;
            if (string.IsNullOrWhiteSpace(upn))
                throw new InvalidOperationException("User UPN or email claim is required");

            return upn;
        }

        private static void ValidateDomain(Domain domain)
        {
            if (domain == null)
                throw new ArgumentException("Domain information is required", nameof(domain));

            if (string.IsNullOrWhiteSpace(domain.SecondLevelDomain))
                throw new ArgumentException("Second level domain is required", nameof(domain));

            if (string.IsNullOrWhiteSpace(domain.TopLevelDomain))
                throw new ArgumentException("Top level domain is required", nameof(domain));

            // Basic domain name validation
            if (!IsValidDomainName(domain.SecondLevelDomain))
                throw new ArgumentException("Invalid second level domain name", nameof(domain));

            if (!IsValidTopLevelDomain(domain.TopLevelDomain))
                throw new ArgumentException("Invalid top level domain", nameof(domain));
        }

        private static void ValidateContactInformation(ContactInformation contactInfo)
        {
            if (contactInfo == null)
                throw new ArgumentException("Contact information is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.FirstName))
                throw new ArgumentException("First name is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.LastName))
                throw new ArgumentException("Last name is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.EmailAddress))
                throw new ArgumentException("Email address is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.Address))
                throw new ArgumentException("Address is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.City))
                throw new ArgumentException("City is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.State))
                throw new ArgumentException("State is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.Country))
                throw new ArgumentException("Country is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.ZipCode))
                throw new ArgumentException("ZIP code is required", nameof(contactInfo));

            if (string.IsNullOrWhiteSpace(contactInfo.TelephoneNumber))
                throw new ArgumentException("Telephone number is required", nameof(contactInfo));

            // Basic email validation
            if (!IsValidEmail(contactInfo.EmailAddress))
                throw new ArgumentException("Invalid email address format", nameof(contactInfo));
        }

        private static bool IsValidDomainName(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
                return false;

            // Basic domain name validation - alphanumeric and hyphens, not starting/ending with hyphen
            return System.Text.RegularExpressions.Regex.IsMatch(domainName, @"^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$");
        }

        private static bool IsValidTopLevelDomain(string tld)
        {
            if (string.IsNullOrWhiteSpace(tld))
                return false;

            // Basic TLD validation - letters only, 2-6 characters
            return System.Text.RegularExpressions.Regex.IsMatch(tld, @"^[a-zA-Z]{2,6}$");
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}