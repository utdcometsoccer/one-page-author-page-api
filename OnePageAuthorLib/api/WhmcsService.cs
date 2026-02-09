using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for interacting with the WHMCS API for domain registration operations.
    /// </summary>
    public class WhmcsService : IWhmcsService
    {
        private readonly ILogger<WhmcsService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _apiUrl;
        private readonly string? _apiIdentifier;
        private readonly string? _apiSecret;
        private readonly bool _isConfigured;

        public WhmcsService(
            ILogger<WhmcsService> logger,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _apiUrl = configuration["WHMCS_API_URL"];
            _apiIdentifier = configuration["WHMCS_API_IDENTIFIER"];
            _apiSecret = configuration["WHMCS_API_SECRET"];
            
            // Check if WHMCS is configured
            _isConfigured = !string.IsNullOrWhiteSpace(_apiUrl) &&
                           !string.IsNullOrWhiteSpace(_apiIdentifier) &&
                           !string.IsNullOrWhiteSpace(_apiSecret);
            
            if (!_isConfigured)
            {
                _logger.LogWarning("WHMCS integration is not configured. Domain registration via WHMCS will be skipped.");
            }
            else
            {
                // Validate API URL is absolute and HTTPS
                if (!Uri.TryCreate(_apiUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                {
                    _logger.LogWarning("WHMCS_API_URL is not a valid HTTPS URL: {Url}. WHMCS integration will be disabled.", _apiUrl);
                    _isConfigured = false;
                }
            }
        }

        /// <summary>
        /// Registers a domain using the WHMCS DomainRegister API.
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <returns>True if the registration was successful, false otherwise</returns>
        public async Task<bool> RegisterDomainAsync(DomainRegistration domainRegistration)
        {
            if (!_isConfigured)
            {
                _logger.LogInformation("WHMCS integration is not configured, skipping domain registration");
                return false;
            }
            
            if (domainRegistration?.Domain == null)
            {
                _logger.LogWarning("Domain registration or domain is null");
                return false;
            }

            var domainName = domainRegistration.Domain.FullDomainName;
            _logger.LogInformation("Attempting to register domain {DomainName} via WHMCS API", domainName);

            try
            {
                // Prepare the request parameters
                var requestData = new Dictionary<string, string>
                {
                    { "action", "DomainRegister" },
                    { "identifier", _apiIdentifier! }, // Guaranteed non-null by _isConfigured check
                    { "secret", _apiSecret! }, // Guaranteed non-null by _isConfigured check
                    { "domain", domainName },
                    { "responsetype", "json" }
                };

                // Add contact information if available
                if (domainRegistration.ContactInformation != null)
                {
                    var contact = domainRegistration.ContactInformation;
                    
                    if (!string.IsNullOrWhiteSpace(contact.FirstName))
                        requestData["firstname"] = contact.FirstName;
                    
                    if (!string.IsNullOrWhiteSpace(contact.LastName))
                        requestData["lastname"] = contact.LastName;
                    
                    if (!string.IsNullOrWhiteSpace(contact.EmailAddress))
                        requestData["email"] = contact.EmailAddress;
                    
                    if (!string.IsNullOrWhiteSpace(contact.Address))
                        requestData["address1"] = contact.Address;
                    
                    if (!string.IsNullOrWhiteSpace(contact.Address2))
                        requestData["address2"] = contact.Address2;
                    
                    if (!string.IsNullOrWhiteSpace(contact.City))
                        requestData["city"] = contact.City;
                    
                    if (!string.IsNullOrWhiteSpace(contact.State))
                        requestData["state"] = contact.State;
                    
                    if (!string.IsNullOrWhiteSpace(contact.ZipCode))
                        requestData["postcode"] = contact.ZipCode;
                    
                    if (!string.IsNullOrWhiteSpace(contact.Country))
                        requestData["country"] = contact.Country;
                    
                    if (!string.IsNullOrWhiteSpace(contact.TelephoneNumber))
                        requestData["phonenumber"] = contact.TelephoneNumber;
                }

                // Create the form content and make the API request
                using var content = new FormUrlEncodedContent(requestData);
                using var response = await _httpClient.PostAsync(_apiUrl!, content); // Guaranteed non-null by _isConfigured check
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("WHMCS API returned error status {StatusCode} for domain {DomainName}", 
                        response.StatusCode, domainName);
                    return false;
                }

                // Parse the response
                var jsonResponse = JsonSerializer.Deserialize<WhmcsResponse>(responseContent);

                if (jsonResponse?.Result == "success")
                {
                    _logger.LogInformation("Successfully registered domain {DomainName} via WHMCS API", domainName);
                    return true;
                }
                else
                {
                    var errorMessage = jsonResponse?.Message ?? "Unknown error";
                    _logger.LogWarning("WHMCS API returned non-success result for domain {DomainName}: {Message}", 
                        domainName, errorMessage);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while registering domain {DomainName}", domainName);
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse WHMCS API response for domain {DomainName}", domainName);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering domain {DomainName}", domainName);
                return false;
            }
        }

        /// <summary>
        /// Represents the WHMCS API response structure.
        /// </summary>
        private class WhmcsResponse
        {
            [JsonPropertyName("result")]
            public string? Result { get; set; }
            
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
