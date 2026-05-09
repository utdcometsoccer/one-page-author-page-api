namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents an explicitly curated featured book for the homepage hero experiment.
    /// Only populated when the author has a book with <c>IsFeaturedHeroBook = true</c>.
    /// </summary>
    public class FeaturedBookDto
    {
        /// <summary>
        /// The title of the featured book.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional subtitle for the featured book.
        /// </summary>
        public string? Subtitle { get; set; }

        /// <summary>
        /// The author's display name.
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;

        /// <summary>
        /// The description of the featured book.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// URL to the cover image.
        /// </summary>
        public string CoverImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Alt text for the cover image. Falls back to the book title if not explicitly set.
        /// </summary>
        public string CoverImageAlt { get; set; } = string.Empty;

        /// <summary>
        /// Label for the primary call-to-action button (e.g., "Buy Now").
        /// </summary>
        public string PrimaryCtaLabel { get; set; } = string.Empty;

        /// <summary>
        /// URL for the primary call-to-action button.
        /// </summary>
        public string PrimaryCtaUrl { get; set; } = string.Empty;

        /// <summary>
        /// Optional label for a secondary call-to-action button (e.g., "Learn More").
        /// </summary>
        public string? SecondaryCtaLabel { get; set; }

        /// <summary>
        /// Optional URL for the secondary call-to-action button.
        /// </summary>
        public string? SecondaryCtaUrl { get; set; }

        /// <summary>
        /// Available formats for the book (e.g., "Hardcover", "Paperback", "eBook").
        /// </summary>
        public IReadOnlyList<string> Formats { get; set; } = Array.Empty<string>();
    }
}
