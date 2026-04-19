using System.Text.Json;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Interface for WHMCS API integration for domain registration operations.
    /// </summary>
    public interface IWhmcsService
    {
        /// <summary>
        /// Gets a value indicating whether the WHMCS integration is fully configured.
        /// When <c>false</c>, calling any API method will throw <see cref="InvalidOperationException"/>.
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Registers a domain using the WHMCS DomainRegister API.
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <returns>True if the registration was successful, false otherwise</returns>
        Task<bool> RegisterDomainAsync(DomainRegistration domainRegistration);

        /// <summary>
        /// Updates the name servers for a registered domain using the WHMCS DomainUpdateNameservers API.
        /// </summary>
        /// <param name="domainName">The fully qualified domain name</param>
        /// <param name="nameServers">Array of name server hostnames (must provide at least 2, maximum 5)</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        Task<bool> UpdateNameServersAsync(string domainName, string[] nameServers);

        /// <summary>
        /// Gets TLD pricing information from the WHMCS GetTLDPricing API.
        /// </summary>
        /// <param name="clientId">The client ID to retrieve pricing for (optional)</param>
        /// <param name="currencyId">The currency ID to use for pricing (optional)</param>
        /// <returns>A JsonDocument containing the pricing information</returns>
        Task<JsonDocument> GetTLDPricingAsync(string? clientId = null, int? currencyId = null);

        /// <summary>
        /// Checks domain availability using the WHMCS DomainWhois API.
        /// </summary>
        /// <param name="domainName">The fully qualified domain name to check</param>
        /// <returns>True if the domain is available for registration, false if the domain is already registered or otherwise unavailable.</returns>
        /// <exception cref="InvalidOperationException">Thrown when WHMCS integration is not configured, or when the WHMCS API returns a non-success result.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="domainName"/> is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the WHMCS API fails or returns a non-2xx status code.</exception>
        /// <exception cref="System.Text.Json.JsonException">Thrown when the WHMCS API response cannot be parsed as JSON.</exception>
        Task<bool> CheckDomainAvailabilityAsync(string domainName);

        /// <summary>
        /// Places a domain registration order using the WHMCS AddOrder API.
        /// AddOrder automatically triggers domain registration; no separate DomainRegister call is needed.
        /// Name servers may be included in the same request. Blank entries are silently filtered out,
        /// and at most 5 name servers are sent (any additional entries beyond the 5th are dropped).
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <param name="nameServers">Name servers to configure on the domain (0–5 entries; blank entries are ignored)</param>
        /// <param name="clientId">
        /// The WHMCS client ID to place the order for. Must be a non-empty string containing a positive integer.
        /// This is a required configuration value (set via <c>WHMCS_CLIENT_ID</c>).
        /// </param>
        /// <returns>True if the order was placed successfully, false otherwise</returns>
        /// <exception cref="InkStainedWretch.OnePageAuthorAPI.API.WhmcsConfigurationException">
        /// Thrown when <paramref name="clientId"/> is null, empty, or not a valid positive integer.
        /// This is a non-retryable configuration failure.
        /// </exception>
        Task<bool> AddOrderAsync(DomainRegistration domainRegistration, string[] nameServers, string clientId);
    }
}
