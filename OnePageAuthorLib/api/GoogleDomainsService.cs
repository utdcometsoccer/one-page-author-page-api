using Google.Cloud.Domains.V1;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for registering domains using the Google Domains API.
    /// </summary>
    public class GoogleDomainsService : IGoogleDomainsService
    {
        private readonly ILogger<GoogleDomainsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _projectId;
        private readonly string _location;

        public GoogleDomainsService(
            ILogger<GoogleDomainsService> logger,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configuration values
            _projectId = _configuration["GOOGLE_CLOUD_PROJECT_ID"]
                ?? throw new InvalidOperationException("GOOGLE_CLOUD_PROJECT_ID configuration is required");
            _location = _configuration["GOOGLE_DOMAINS_LOCATION"] ?? "global";

            _logger.LogInformation("GoogleDomainsService initialized with project {ProjectId}, location {Location}",
                _projectId, _location);
        }

        /// <summary>
        /// Registers a domain using the Google Domains API.
        /// </summary>
        public async Task<bool> RegisterDomainAsync(DomainRegistration domainRegistration)
        {
            if (domainRegistration == null)
            {
                _logger.LogWarning("Domain registration is null");
                return false;
            }

            if (domainRegistration.Domain == null)
            {
                _logger.LogWarning("Domain is null in registration {Id}", domainRegistration.id);
                return false;
            }

            var domainName = domainRegistration.Domain.FullDomainName;
            if (string.IsNullOrWhiteSpace(domainName))
            {
                _logger.LogWarning("Domain name is empty in registration {Id}", domainRegistration.id);
                return false;
            }

            try
            {
                _logger.LogInformation("Starting domain registration for {DomainName} (ID: {Id})",
                    domainName, domainRegistration.id);

                // Create the Domains service client
                var client = await DomainsClient.CreateAsync();

                // Prepare the registration request
                var parent = $"projects/{_projectId}/locations/{_location}";
                
                var registrantContact = new ContactSettings.Types.Contact
                {
                    PostalAddress = new global::Google.Type.PostalAddress
                    {
                        RegionCode = domainRegistration.ContactInformation.Country,
                        PostalCode = domainRegistration.ContactInformation.ZipCode,
                        AdministrativeArea = domainRegistration.ContactInformation.State,
                        Locality = domainRegistration.ContactInformation.City,
                        AddressLines =
                        {
                            domainRegistration.ContactInformation.Address
                        }
                    },
                    Email = domainRegistration.ContactInformation.EmailAddress,
                    PhoneNumber = domainRegistration.ContactInformation.TelephoneNumber
                };

                var contactData = new ContactSettings
                {
                    RegistrantContact = registrantContact,
                    AdminContact = registrantContact,
                    TechnicalContact = registrantContact
                };

                var registration = new Registration
                {
                    DomainName = domainName,
                    ContactSettings = contactData
                };

                var request = new RegisterDomainRequest
                {
                    Parent = parent,
                    Registration = registration,
                    // Set yearly auto-renewal by default
                    YearlyPrice = new global::Google.Type.Money
                    {
                        CurrencyCode = "USD",
                        Units = 12 // This is a placeholder; actual price is determined by Google
                    }
                };

                _logger.LogInformation("Submitting domain registration request to Google Domains API for {DomainName}",
                    domainName);

                // Register the domain (this is a long-running operation)
                var operation = await client.RegisterDomainAsync(request);
                
                _logger.LogInformation("Domain registration operation started for {DomainName}, operation name: {OperationName}",
                    domainName, operation.Name);

                // Note: We don't wait for the operation to complete here as it can take a long time
                // The function should track the operation status separately if needed
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering domain {DomainName} (ID: {Id})",
                    domainName, domainRegistration.id);
                return false;
            }
        }

        /// <summary>
        /// Checks if a domain is available for registration.
        /// </summary>
        public async Task<bool> IsDomainAvailableAsync(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                _logger.LogWarning("Domain name is null or empty");
                return false;
            }

            try
            {
                _logger.LogInformation("Checking domain availability for {DomainName}", domainName);

                var client = await DomainsClient.CreateAsync();
                var parent = $"projects/{_projectId}/locations/{_location}";

                var request = new SearchDomainsRequest
                {
                    Location = parent,
                    Query = domainName
                };

                var response = await client.SearchDomainsAsync(request);

                // Check if any register parameters indicate the domain is available
                var isAvailable = response.RegisterParameters.Count > 0 
                    && response.RegisterParameters.Any(rp => rp.Availability == RegisterParameters.Types.Availability.Available);

                _logger.LogInformation("Domain {DomainName} availability: {IsAvailable}",
                    domainName, isAvailable);

                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking domain availability for {DomainName}", domainName);
                return false;
            }
        }
    }
}
