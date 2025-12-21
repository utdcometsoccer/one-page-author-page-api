using Newtonsoft.Json;

namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents a lead captured from landing pages, blogs, or other sources.
    /// </summary>
    public class Lead
    {
        /// <summary>
        /// Cosmos DB document id (case-sensitive). If not provided on create, it will be generated.
        /// </summary>
        [JsonProperty("id")]
        public string? id { get; set; }

        /// <summary>
        /// Email address of the lead (required, validated).
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name of the lead (optional).
        /// </summary>
        [JsonProperty("firstName")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Source of the lead (e.g., landing_page, blog, exit_intent, newsletter).
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Lead magnet identifier (e.g., author-success-kit, marketing-guide).
        /// </summary>
        [JsonProperty("leadMagnet")]
        public string? LeadMagnet { get; set; }

        /// <summary>
        /// UTM source tracking parameter.
        /// </summary>
        [JsonProperty("utmSource")]
        public string? UtmSource { get; set; }

        /// <summary>
        /// UTM medium tracking parameter.
        /// </summary>
        [JsonProperty("utmMedium")]
        public string? UtmMedium { get; set; }

        /// <summary>
        /// UTM campaign tracking parameter.
        /// </summary>
        [JsonProperty("utmCampaign")]
        public string? UtmCampaign { get; set; }

        /// <summary>
        /// HTTP referrer header value.
        /// </summary>
        [JsonProperty("referrer")]
        public string? Referrer { get; set; }

        /// <summary>
        /// Locale/culture code (e.g., en-US, fr-CA, es-MX).
        /// </summary>
        [JsonProperty("locale")]
        public string Locale { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the request (for rate limiting and analytics).
        /// </summary>
        [JsonProperty("ipAddress")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Consent tracking - indicates user has consented to data collection (GDPR compliance).
        /// </summary>
        [JsonProperty("consentGiven")]
        public bool ConsentGiven { get; set; } = false;

        /// <summary>
        /// Timestamp when the lead was created.
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the lead was last updated.
        /// </summary>
        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status of email service integration (e.g., pending, synced, failed).
        /// </summary>
        [JsonProperty("emailServiceStatus")]
        public string EmailServiceStatus { get; set; } = "pending";

        /// <summary>
        /// Email domain extracted from email address (used as partition key).
        /// </summary>
        [JsonProperty("emailDomain")]
        public string EmailDomain { get; set; } = string.Empty;

        /// <summary>
        /// Initializes an empty lead.
        /// </summary>
        public Lead() { }

        /// <summary>
        /// Initializes a lead with required fields.
        /// </summary>
        /// <param name="email">Email address</param>
        /// <param name="source">Lead source</param>
        /// <param name="locale">Locale code</param>
        public Lead(string email, string source, string locale)
        {
            Email = email;
            Source = source;
            Locale = locale;
            EmailDomain = ExtractEmailDomain(email);
        }

        /// <summary>
        /// Extracts the domain part from an email address.
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Domain part of the email (e.g., "example.com")</returns>
        public static string ExtractEmailDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "unknown";

            var parts = email.Split('@');
            return parts.Length == 2 ? parts[1].ToLowerInvariant() : "unknown";
        }
    }

    /// <summary>
    /// Valid sources for lead capture.
    /// </summary>
    public static class LeadSource
    {
        public const string LandingPage = "landing_page";
        public const string Blog = "blog";
        public const string ExitIntent = "exit_intent";
        public const string Newsletter = "newsletter";

        public static readonly string[] ValidSources = new[]
        {
            LandingPage,
            Blog,
            ExitIntent,
            Newsletter
        };

        public static bool IsValid(string source)
        {
            return ValidSources.Contains(source);
        }
    }
}
