namespace InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI
{
    /// <summary>
    /// Represents a user's membership/assignment to an ImageStorageTier.
    /// </summary>
    public class ImageStorageTierMembership
    {
        /// <summary>
        /// Primary key for the membership record.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to ImageStorageTier.id
        /// </summary>
        public string TierId { get; set; } = string.Empty;

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
    }
}
