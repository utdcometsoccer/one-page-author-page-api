namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents a referral made by a user to invite another person to the platform.
    /// Tracks referral status and conversion for rewards/credits.
    /// </summary>
    public class Referral
    {
        /// <summary>
        /// Cosmos DB document id. Unique identifier for the referral.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The ID of the user who made the referral (referrer).
        /// Used as the partition key for efficient querying by referrer.
        /// </summary>
        public string ReferrerId { get; set; }

        /// <summary>
        /// The email address of the person being referred.
        /// </summary>
        public string ReferredEmail { get; set; }

        /// <summary>
        /// Unique referral code generated for this referral.
        /// Can be shared via URL or code entry.
        /// </summary>
        public string ReferralCode { get; set; }

        /// <summary>
        /// Current status of the referral.
        /// Values: "Pending", "Converted", "Expired"
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// When this referral was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When this referral was last updated (e.g., status changed to Converted).
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Optional: The user ID of the referred person once they sign up.
        /// Null until the referred email actually creates an account.
        /// </summary>
        public string? ReferredUserId { get; set; }

        /// <summary>
        /// Optional: When the referred user converted to a paid subscription.
        /// Used to track successful referrals for credit calculation.
        /// </summary>
        public DateTime? ConvertedAt { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Referral()
        {
            id = string.Empty;
            ReferrerId = string.Empty;
            ReferredEmail = string.Empty;
            ReferralCode = string.Empty;
            Status = "Pending";
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Constructor with required fields.
        /// </summary>
        public Referral(string referrerId, string referredEmail, string referralCode)
        {
            id = Guid.NewGuid().ToString();
            ReferrerId = referrerId;
            ReferredEmail = referredEmail;
            ReferralCode = referralCode;
            Status = "Pending";
            CreatedAt = DateTime.UtcNow;
        }
    }
}
