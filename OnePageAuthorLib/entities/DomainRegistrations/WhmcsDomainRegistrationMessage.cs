using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.Entities.DomainRegistrations
{
    /// <summary>
    /// Message payload enqueued to the Azure Service Bus queue for WHMCS domain registration operations.
    /// The VM-hosted worker service dequeues this message and calls the WHMCS REST API from
    /// a static IP address.
    /// </summary>
    public class WhmcsDomainRegistrationMessage
    {
        /// <summary>
        /// Unique identifier for this message.
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The domain registration entity to be processed via WHMCS.
        /// </summary>
        public DomainRegistration DomainRegistration { get; set; } = new();

        /// <summary>
        /// Azure DNS name servers retrieved after the DNS zone was created.
        /// These will be set on the domain in WHMCS after successful registration.
        /// May be empty if the DNS zone was not yet available.
        /// </summary>
        public string[] NameServers { get; set; } = [];

        /// <summary>
        /// UTC timestamp when this message was enqueued.
        /// </summary>
        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    }
}
