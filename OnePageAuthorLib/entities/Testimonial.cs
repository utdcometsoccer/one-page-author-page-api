namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Entity representing a testimonial for the landing page.
    /// </summary>
    public class Testimonial
    {
        /// <summary>
        /// Unique identifier for the testimonial.
        /// </summary>
        public string id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the person providing the testimonial.
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;

        /// <summary>
        /// Title or description of the person (e.g., "Mystery Novelist").
        /// </summary>
        public string AuthorTitle { get; set; } = string.Empty;

        /// <summary>
        /// The testimonial quote text.
        /// </summary>
        public string Quote { get; set; } = string.Empty;

        /// <summary>
        /// Rating from 1-5 stars.
        /// </summary>
        public int Rating { get; set; } = 5;

        /// <summary>
        /// Optional URL to the author's photo.
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Whether this testimonial is featured.
        /// </summary>
        public bool Featured { get; set; } = false;

        /// <summary>
        /// Timestamp when the testimonial was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Locale for the testimonial (e.g., "en-US", "es-ES").
        /// </summary>
        public string Locale { get; set; } = "en-US";
    }
}
