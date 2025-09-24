namespace InkStainedWretch.OnePageAuthorAPI.Entities.ImageAPI
{
    /// <summary>
    /// Represents an uploaded image file stored in Azure Blob Storage.
    /// </summary>
    public class Image
    {
        /// <summary>
        /// Primary key for the image record.
        /// </summary>
        public string id { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to UserProfile.id - the owner of the image.
        /// </summary>
        public string UserProfileId { get; set; } = string.Empty;

        /// <summary>
        /// Original filename of the uploaded image.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Public URL to access the image in Azure Blob Storage.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Size of the image file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// MIME type of the image (e.g., image/jpeg, image/png).
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Azure Blob Storage container name where the image is stored.
        /// </summary>
        public string ContainerName { get; set; } = string.Empty;

        /// <summary>
        /// Azure Blob Storage blob name (unique identifier within the container).
        /// </summary>
        public string BlobName { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the image was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}