namespace InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI
{
    /// <summary>
    /// Represents an image storage plan/tier with a friendly name and a monthly cost.
    /// </summary>
    public class ImageStorageTier
    {
        /// <summary>
        /// The primary key for the ImageStorageTier entity.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// A friendly display name for the tier (e.g., "Free", "Pro", "Enterprise").
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Total monthly cost of the tier in US dollars.
        /// </summary>
        public decimal CostInDollars { get; set; }

        /// <summary>
        /// Storage capacity included in the tier, in gigabytes.
        /// </summary>
        public decimal StorageInGB { get; set; }

        /// <summary>
        /// Monthly bandwidth allowance in gigabytes.
        /// </summary>
        public decimal BandwidthInGB { get; set; }
    }
}
