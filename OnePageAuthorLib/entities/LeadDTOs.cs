using System.ComponentModel.DataAnnotations;

namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Request DTO for creating a new lead.
    /// </summary>
    public class CreateLeadRequest
    {
        /// <summary>
        /// Email address (required, validated).
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// First name (optional).
        /// </summary>
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? FirstName { get; set; }

        /// <summary>
        /// Lead source (required, must be one of: landing_page, blog, exit_intent, newsletter).
        /// </summary>
        [Required(ErrorMessage = "Source is required")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Lead magnet identifier (optional).
        /// </summary>
        [StringLength(100, ErrorMessage = "Lead magnet cannot exceed 100 characters")]
        public string? LeadMagnet { get; set; }

        /// <summary>
        /// UTM source tracking parameter (optional).
        /// </summary>
        [StringLength(200, ErrorMessage = "UTM source cannot exceed 200 characters")]
        public string? UtmSource { get; set; }

        /// <summary>
        /// UTM medium tracking parameter (optional).
        /// </summary>
        [StringLength(200, ErrorMessage = "UTM medium cannot exceed 200 characters")]
        public string? UtmMedium { get; set; }

        /// <summary>
        /// UTM campaign tracking parameter (optional).
        /// </summary>
        [StringLength(200, ErrorMessage = "UTM campaign cannot exceed 200 characters")]
        public string? UtmCampaign { get; set; }

        /// <summary>
        /// HTTP referrer header value (optional).
        /// </summary>
        [StringLength(500, ErrorMessage = "Referrer cannot exceed 500 characters")]
        public string? Referrer { get; set; }

        /// <summary>
        /// Locale/culture code (required, e.g., en-US, fr-CA).
        /// </summary>
        [Required(ErrorMessage = "Locale is required")]
        [StringLength(10, ErrorMessage = "Locale cannot exceed 10 characters")]
        public string Locale { get; set; } = string.Empty;

        /// <summary>
        /// Indicates user consent for data collection (GDPR compliance).
        /// Default is false; should be explicitly set to true when user consents.
        /// </summary>
        public bool ConsentGiven { get; set; } = false;
    }

    /// <summary>
    /// Response DTO for lead creation.
    /// </summary>
    public class CreateLeadResponse
    {
        /// <summary>
        /// Lead ID (Cosmos DB document id).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Status of the lead creation (created or existing).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable message about the operation result.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Status values for lead creation response.
    /// </summary>
    public static class LeadCreationStatus
    {
        public const string Created = "created";
        public const string Existing = "existing";
    }
}
