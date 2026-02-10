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
    }
}
