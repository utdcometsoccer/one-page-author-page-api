namespace InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI
{
    /// <summary>
    /// Represents a user's storage and bandwidth usage tracking.
    /// Note: Tier assignment is now handled by Entra ID roles in JWT tokens.
    /// </summary>
    public class ImageStorageUsage
    {
        /// <summary>
        /// Primary key for the usage record (typically the UserProfileId).
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to UserProfile.id
        /// </summary>
        public string UserProfileId { get; set; } = string.Empty;

        /// <summary>
        /// Current storage used by the user in bytes.
        /// </summary>
        public long StorageUsedInBytes { get; set; } = 0;

        /// <summary>
        /// Current bandwidth used by the user in bytes for the current billing period.
        /// </summary>
        public long BandwidthUsedInBytes { get; set; } = 0;

        /// <summary>
        /// Last time the usage was updated.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
