namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents an invitation sent to an author to create a Microsoft account linked to their domain(s).
    /// </summary>
    public class AuthorInvitation
    {
        /// <summary>
        /// Cosmos DB id (case-sensitive). Unique identifier for the invitation.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the author being invited.
        /// </summary>
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// The domain name that will be linked to the author's account (e.g., "example.com").
        /// </summary>
        [Obsolete("Use DomainNames instead. This property is kept for backward compatibility.")]
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// The domain names that will be linked to the author's account (e.g., ["example.com", "author-site.com"]).
        /// </summary>
        public List<string> DomainNames { get; set; } = new List<string>();

        /// <summary>
        /// The date and time when the invitation was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The status of the invitation (Pending, Accepted, Expired, etc.).
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// The date and time when the invitation was accepted (UTC), if applicable.
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>
        /// The user's Object ID (OID) once they accept the invitation and create an account.
        /// </summary>
        public string? UserOid { get; set; }

        /// <summary>
        /// The date and time when the invitation expires (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);

        /// <summary>
        /// Optional notes or additional information about the invitation.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// The date and time when the invitation was last updated (UTC).
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// The date and time when the invitation email was last sent (UTC).
        /// </summary>
        public DateTime? LastEmailSentAt { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthorInvitation() { }

        /// <summary>
        /// Constructor that initializes the invitation with required properties (single domain - backward compatibility).
        /// </summary>
        public AuthorInvitation(string emailAddress, string domainName, string? notes = null)
        {
            id = Guid.NewGuid().ToString();
            EmailAddress = emailAddress;
            DomainName = domainName;
            DomainNames = new List<string> { domainName };
            Notes = notes;
            CreatedAt = DateTime.UtcNow;
            Status = "Pending";
            ExpiresAt = DateTime.UtcNow.AddDays(30);
        }

        /// <summary>
        /// Constructor that initializes the invitation with required properties (multiple domains).
        /// </summary>
        public AuthorInvitation(string emailAddress, List<string> domainNames, string? notes = null)
        {
            id = Guid.NewGuid().ToString();
            EmailAddress = emailAddress;
            DomainNames = domainNames ?? new List<string>();
            // For backward compatibility, set DomainName to first domain
            DomainName = domainNames?.FirstOrDefault() ?? string.Empty;
            Notes = notes;
            CreatedAt = DateTime.UtcNow;
            Status = "Pending";
            ExpiresAt = DateTime.UtcNow.AddDays(30);
        }
    }
}
