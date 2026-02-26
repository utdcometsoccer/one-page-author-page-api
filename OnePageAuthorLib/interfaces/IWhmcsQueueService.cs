using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations;

namespace InkStainedWretch.OnePageAuthorAPI.Interfaces
{
    /// <summary>
    /// Enqueues WHMCS domain registration operations to the Azure Service Bus queue
    /// so that they can be executed from the VM with a static IP address.
    /// </summary>
    public interface IWhmcsQueueService
    {
        /// <summary>
        /// Sends a domain registration message to the Service Bus queue.
        /// The worker service running on the VM will dequeue this message and
        /// call the WHMCS REST API.
        /// </summary>
        /// <param name="registration">Domain registration to process via WHMCS.</param>
        /// <param name="nameServers">
        /// Azure DNS name servers for the domain zone (may be empty if the DNS zone
        /// was not yet available when the message was enqueued).
        /// </param>
        /// <returns>A task that completes when the message has been sent.</returns>
        Task EnqueueDomainRegistrationAsync(DomainRegistration registration, string[] nameServers);
    }
}
