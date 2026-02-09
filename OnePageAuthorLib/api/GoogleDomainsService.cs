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
        private readonly IUsStateCodeService _usStateCodeService;
        private readonly string _projectId;
        private readonly string _location;

        public GoogleDomainsService(
            ILogger<GoogleDomainsService> logger,
            IConfiguration configuration,
            IUsStateCodeService usStateCodeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _usStateCodeService = usStateCodeService ?? throw new ArgumentNullException(nameof(usStateCodeService));

            // Get configuration values
            _projectId = _configuration["GOOGLE_CLOUD_PROJECT_ID"]
                ?? throw new InvalidOperationException("GOOGLE_CLOUD_PROJECT_ID configuration is required");
            _location = NormalizeDomainsLocation(_configuration["GOOGLE_DOMAINS_LOCATION"], _logger);

            _logger.LogInformation("GoogleDomainsService initialized with project {ProjectId}, location {Location}",
                _projectId, _location);
        }

        private static string NormalizeDomainsLocation(string? configuredLocation, ILogger logger)
        {
            var location = configuredLocation?.Trim();

            if (string.IsNullOrWhiteSpace(location))
            {
                return "global";
            }

            if (string.Equals(location, "global", StringComparison.OrdinalIgnoreCase))
            {
                return "global";
            }

            logger.LogWarning(
                "Unsupported Google Domains location '{ConfiguredLocation}'. Cloud Domains API currently expects location 'global'. Falling back to 'global'.",
                location);

            return "global";
        }

        private ContactPrivacy GetPreferredContactPrivacy()
        {
            // Cloud Domains APIs vary by TLD; some only support redacted/public contact data.
            // Default to Redacted to avoid InvalidArgument for TLDs that don't support private.
            var configured = _configuration["GOOGLE_DOMAINS_CONTACT_PRIVACY"]?.Trim();

            if (string.IsNullOrWhiteSpace(configured))
            {
                return ContactPrivacy.RedactedContactData;
            }

            return configured.ToLowerInvariant() switch
            {
                "private" => ContactPrivacy.PrivateContactData,
                "redacted" => ContactPrivacy.RedactedContactData,
                "public" => ContactPrivacy.PublicContactData,
                _ => ContactPrivacy.RedactedContactData
            };
        }

        private static string NormalizeRegionCode(string? country)
        {
            var value = country?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            if (string.Equals(value, "United States", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "United States of America", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "USA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "US", StringComparison.OrdinalIgnoreCase))
            {
                return "US";
            }

            if (string.Equals(value, "Canada", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "CA", StringComparison.OrdinalIgnoreCase))
            {
                return "CA";
            }

            if (string.Equals(value, "Mexico", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "MX", StringComparison.OrdinalIgnoreCase))
            {
                return "MX";
            }

            if (value.Length == 2)
            {
                return value.ToUpperInvariant();
            }

            return value;
        }

        private static string NormalizePhoneNumber(string? phoneNumber)
        {
            var value = phoneNumber?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            // Keep leading '+' if present, strip all other non-digits.
            var hasPlus = value.StartsWith('+');
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return hasPlus ? $"+{digits}" : digits;
        }

        private DnsSettings BuildDnsSettings(string domainName)
        {
            // Cloud Domains requires that dns_settings contains a DNS provider.
            // Default to Google Domains free DNS unless explicitly configured.
            var provider = _configuration["GOOGLE_DOMAINS_DNS_PROVIDER"]?.Trim();

            if (string.Equals(provider, "custom", StringComparison.OrdinalIgnoreCase))
            {
                var nameServersRaw = _configuration["GOOGLE_DOMAINS_CUSTOM_NAMESERVERS"]?.Trim();
                var nameServers = (nameServersRaw ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(ns => !string.IsNullOrWhiteSpace(ns))
                    .ToArray();

                if (nameServers.Length == 0)
                {
                    throw new InvalidOperationException(
                        "GOOGLE_DOMAINS_CUSTOM_NAMESERVERS must be set when GOOGLE_DOMAINS_DNS_PROVIDER=custom (comma-separated hostnames).");
                }

                _logger.LogInformation(
                    "Using custom DNS provider for {DomainName} with {NameServerCount} name servers.",
                    domainName,
                    nameServers.Length);

                return new DnsSettings
                {
                    CustomDns = new DnsSettings.Types.CustomDns
                    {
                        NameServers = { nameServers }
                    }
                };
            }

            // Default: Google Domains DNS
            _logger.LogInformation("Using Google Domains DNS provider for {DomainName}.", domainName);
            return new DnsSettings
            {
                GoogleDomainsDns = new DnsSettings.Types.GoogleDomainsDns()
            };
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
                
                var regionCode = NormalizeRegionCode(domainRegistration.ContactInformation.Country);
                var administrativeArea = regionCode == "US"
                    ? _usStateCodeService.NormalizeToCode(domainRegistration.ContactInformation.State)
                    : (domainRegistration.ContactInformation.State ?? string.Empty).Trim();

                var contactName = $"{domainRegistration.ContactInformation.FirstName} {domainRegistration.ContactInformation.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(contactName))
                {
                    _logger.LogWarning(
                        "Contact first/last name is required for Google Domains registration. Domain: {DomainName} (ID: {Id})",
                        domainName, domainRegistration.id);
                    return false;
                }

                var registrantContact = new ContactSettings.Types.Contact
                {
                    PostalAddress = new global::Google.Type.PostalAddress
                    {
                        RegionCode = regionCode,
                        PostalCode = domainRegistration.ContactInformation.ZipCode,
                        AdministrativeArea = administrativeArea,
                        Locality = domainRegistration.ContactInformation.City,
                        Recipients = { contactName },
                        AddressLines =
                        {
                            domainRegistration.ContactInformation.Address
                        }
                    },
                    Email = domainRegistration.ContactInformation.EmailAddress,
                    PhoneNumber = NormalizePhoneNumber(domainRegistration.ContactInformation.TelephoneNumber)
                };

                if (!string.IsNullOrWhiteSpace(domainRegistration.ContactInformation.Address2))
                {
                    registrantContact.PostalAddress.AddressLines.Add(domainRegistration.ContactInformation.Address2.Trim());
                }

                var preferredPrivacy = GetPreferredContactPrivacy();

                var contactData = new ContactSettings
                {
                    RegistrantContact = registrantContact,
                    AdminContact = registrantContact,
                    TechnicalContact = registrantContact,
                    Privacy = preferredPrivacy
                };

                var registration = new Registration
                {
                    DomainName = domainName,
                    ContactSettings = contactData,
                    DnsSettings = BuildDnsSettings(domainName)
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
                // Some TLDs reject PRIVATE_CONTACT_DATA; fall back to REDACTED then PUBLIC.
                try
                {
                    var operation = await client.RegisterDomainAsync(request);

                    _logger.LogInformation(
                        "Domain registration operation started for {DomainName}, operation name: {OperationName}",
                        domainName, operation.Name);

                    // Note: We don't wait for the operation to complete here as it can take a long time.
                    // The function should track the operation status separately if needed.
                    return true;
                }
                catch (Grpc.Core.RpcException ex) when (
                    ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument &&
                    ex.Status.Detail.Contains("does not support contact privacy type", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(ex,
                        "RegisterDomain rejected privacy type {Privacy} for {DomainName}. Retrying with redacted/public privacy.",
                        preferredPrivacy, domainName);

                    contactData.Privacy = ContactPrivacy.RedactedContactData;
                    try
                    {
                        var operation = await client.RegisterDomainAsync(request);

                        _logger.LogInformation(
                            "Domain registration operation started for {DomainName}, operation name: {OperationName}",
                            domainName, operation.Name);

                        return true;
                    }
                    catch (Grpc.Core.RpcException ex2) when (
                        ex2.StatusCode == Grpc.Core.StatusCode.InvalidArgument &&
                        ex2.Status.Detail.Contains("does not support contact privacy type", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(ex2,
                            "RegisterDomain rejected redacted privacy for {DomainName}. Retrying with public contact data.",
                            domainName);

                        contactData.Privacy = ContactPrivacy.PublicContactData;
                        var operation = await client.RegisterDomainAsync(request);

                        _logger.LogInformation(
                            "Domain registration operation started for {DomainName}, operation name: {OperationName}",
                            domainName, operation.Name);

                        return true;
                    }
                }
                catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
                {
                    _logger.LogError(ex,
                        "InvalidArgument from Google Domains RegisterDomain for {DomainName}. Detail: {Detail}. " +
                        "Contact summary: name={ContactName}, region={RegionCode}, admin={AdministrativeArea}, locality={Locality}, postalCode={PostalCode}, email={Email}, phone={Phone}.",
                        domainName,
                        ex.Status.Detail,
                        contactName,
                        regionCode,
                        administrativeArea,
                        domainRegistration.ContactInformation.City,
                        domainRegistration.ContactInformation.ZipCode,
                        domainRegistration.ContactInformation.EmailAddress,
                        NormalizePhoneNumber(domainRegistration.ContactInformation.TelephoneNumber));
                    return false;
                }
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.PermissionDenied)
            {
                _logger.LogError(ex,
                    "Permission denied registering domain {DomainName} (ID: {Id}). " +
                    "Verify Cloud Domains API is enabled for project {ProjectId}, credentials are authorized, and GOOGLE_DOMAINS_LOCATION is set to 'global'.",
                    domainName, domainRegistration.id, _projectId);
                return false;
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
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.PermissionDenied)
            {
                _logger.LogError(ex,
                    "Permission denied searching domain availability for {DomainName}. " +
                    "Verify Cloud Domains API is enabled for project {ProjectId}, credentials are authorized, and GOOGLE_DOMAINS_LOCATION is set to 'global'.",
                    domainName, _projectId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking domain availability for {DomainName}", domainName);
                return false;
            }
        }
    }
}
