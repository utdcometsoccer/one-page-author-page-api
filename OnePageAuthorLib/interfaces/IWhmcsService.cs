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
        /// Gets TLD pricing information from the WHMCS GetTLDPricing API.
        /// </summary>
        /// <param name="clientId">The client ID to retrieve pricing for (optional)</param>
        /// <param name="currencyId">The currency ID to use for pricing (optional)</param>
        /// <returns>A JsonDocument containing the pricing information</returns>
        Task<JsonDocument> GetTLDPricingAsync(string? clientId = null, int? currencyId = null);
    }
}
