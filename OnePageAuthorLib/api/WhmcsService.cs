using System.Text;
using System.Text.Json;
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
        private readonly string _apiUrl;
        private readonly string _apiIdentifier;
        private readonly string _apiSecret;

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

            _apiUrl = configuration["WHMCS_API_URL"] ?? throw new InvalidOperationException("WHMCS_API_URL configuration is required");
            _apiIdentifier = configuration["WHMCS_API_IDENTIFIER"] ?? throw new InvalidOperationException("WHMCS_API_IDENTIFIER configuration is required");
            _apiSecret = configuration["WHMCS_API_SECRET"] ?? throw new InvalidOperationException("WHMCS_API_SECRET configuration is required");
        }

        /// <summary>
        /// Registers a domain using the WHMCS DomainRegister API.
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <returns>True if the registration was successful, false otherwise</returns>
        public async Task<bool> RegisterDomainAsync(DomainRegistration domainRegistration)
        {
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
                    { "identifier", _apiIdentifier },
                    { "secret", _apiSecret },
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

                // Create the form content
                var content = new FormUrlEncodedContent(requestData);

                // Make the API request
                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("WHMCS API response: {StatusCode}, Content: {Content}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("WHMCS API returned error status {StatusCode} for domain {DomainName}: {Content}", 
                        response.StatusCode, domainName, responseContent);
                    return false;
                }

                // Parse the response
                var jsonResponse = JsonSerializer.Deserialize<WhmcsResponse>(responseContent);

                if (jsonResponse?.result == "success")
                {
                    _logger.LogInformation("Successfully registered domain {DomainName} via WHMCS API", domainName);
                    return true;
                }
                else
                {
                    var errorMessage = jsonResponse?.message ?? "Unknown error";
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
            public string? result { get; set; }
            public string? message { get; set; }
        }
    }
}
