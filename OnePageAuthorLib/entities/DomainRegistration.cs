using Newtonsoft.Json;

namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents a domain registration request entity stored in Cosmos DB.
    /// </summary>
    public class DomainRegistration
    {
        /// <summary>
        /// Cosmos DB document id (case-sensitive). If not provided on create, it will be generated.
        /// </summary>
        [JsonProperty("id")]
        public string? id { get; set; }

        /// <summary>
        /// The user's User Principal Name (partition key). This associates the registration with the authenticated user.
        /// </summary>
        [JsonProperty("upn")]
        public string Upn { get; set; } = string.Empty;

        /// <summary>
        /// Domain information for the registration request.
        /// </summary>
        [JsonProperty("domain")]
        public Domain Domain { get; set; } = new();

        /// <summary>
        /// Contact information for the domain registration.
        /// </summary>
        [JsonProperty("contactInformation")]
        public ContactInformation ContactInformation { get; set; } = new();

        /// <summary>
        /// Timestamp when the registration request was created.
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status of the domain registration request.
        /// </summary>
        [JsonProperty("status")]
        public DomainRegistrationStatus Status { get; set; } = DomainRegistrationStatus.Pending;

        /// <summary>
        /// Initializes an empty domain registration.
        /// </summary>
        public DomainRegistration() { }

        /// <summary>
        /// Initializes a domain registration with required fields.
        /// </summary>
        /// <param name="upn">User Principal Name</param>
        /// <param name="domain">Domain information</param>
        /// <param name="contactInformation">Contact information</param>
        public DomainRegistration(string upn, Domain domain, ContactInformation contactInformation)
        {
            Upn = upn;
            Domain = domain;
            ContactInformation = contactInformation;
        }
    }

    /// <summary>
    /// Represents domain information for registration.
    /// </summary>
    public class Domain
    {
        /// <summary>
        /// Top-level domain (e.g., "com", "org", "net").
        /// </summary>
        [JsonProperty("topLevelDomain")]
        public string TopLevelDomain { get; set; } = string.Empty;

        /// <summary>
        /// Second-level domain name (e.g., "example" in "example.com").
        /// </summary>
        [JsonProperty("secondLevelDomain")]
        public string SecondLevelDomain { get; set; } = string.Empty;

        /// <summary>
        /// Gets the full domain name.
        /// </summary>
        public string FullDomainName => $"{SecondLevelDomain}.{TopLevelDomain}";
    }

    /// <summary>
    /// Represents contact information for domain registration.
    /// </summary>
    public class ContactInformation
    {
        /// <summary>
        /// First name of the contact.
        /// </summary>
        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the contact.
        /// </summary>
        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Primary address line.
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Secondary address line (optional).
        /// </summary>
        [JsonProperty("address2")]
        public string? Address2 { get; set; }

        /// <summary>
        /// City name.
        /// </summary>
        [JsonProperty("city")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// State or province.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Country name.
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// ZIP or postal code.
        /// </summary>
        [JsonProperty("zipCode")]
        public string ZipCode { get; set; } = string.Empty;

        /// <summary>
        /// Email address.
        /// </summary>
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// Telephone number.
        /// </summary>
        [JsonProperty("telephoneNumber")]
        public string TelephoneNumber { get; set; } = string.Empty;
    }

    /// <summary>
    /// Status of a domain registration request.
    /// </summary>
    public enum DomainRegistrationStatus
    {
        /// <summary>
        /// Registration request is pending processing.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Registration is in progress.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Registration completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Registration failed.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Registration was cancelled.
        /// </summary>
        Cancelled = 4
    }
}