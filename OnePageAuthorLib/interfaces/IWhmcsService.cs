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
        /// <returns>True if the domain is available for registration, false if unavailable or the check failed</returns>
        Task<bool> CheckDomainAvailabilityAsync(string domainName);

        /// <summary>
        /// Places a domain registration order using the WHMCS AddOrder API.
        /// AddOrder automatically triggers domain registration; no separate DomainRegister call is needed.
        /// Name servers may be included in the same request. Blank entries are silently filtered out,
        /// and at most 5 name servers are sent (any additional entries beyond the 5th are dropped).
        /// </summary>
        /// <param name="domainRegistration">The domain registration information</param>
        /// <param name="nameServers">Name servers to configure on the domain (0–5 entries; blank entries are ignored)</param>
        /// <param name="clientId">The WHMCS client ID to place the order for (optional)</param>
        /// <returns>True if the order was placed successfully, false otherwise</returns>
        Task<bool> AddOrderAsync(DomainRegistration domainRegistration, string[] nameServers, string? clientId = null);
    }
}
