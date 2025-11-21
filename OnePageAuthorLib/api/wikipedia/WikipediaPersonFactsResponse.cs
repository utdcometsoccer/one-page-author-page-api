namespace InkStainedWretch.OnePageAuthorLib.API.Wikipedia
{
    /// <summary>
    /// Response model containing structured facts about a person from Wikipedia
    /// </summary>
    public class WikipediaPersonFactsResponse
    {
        /// <summary>
        /// Title of the Wikipedia page
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Short description of the person
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Extract/introduction text about the person
        /// </summary>
        public string Extract { get; set; } = string.Empty;

        /// <summary>
        /// Lead paragraph from MediaWiki API (plain text)
        /// </summary>
        public string LeadParagraph { get; set; } = string.Empty;

        /// <summary>
        /// Thumbnail image information
        /// </summary>
        public ThumbnailInfo? Thumbnail { get; set; }

        /// <summary>
        /// Canonical URL to the Wikipedia page
        /// </summary>
        public string CanonicalUrl { get; set; } = string.Empty;

        /// <summary>
        /// Language code of the Wikipedia page
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thumbnail image information
    /// </summary>
    public class ThumbnailInfo
    {
        /// <summary>
        /// URL to the thumbnail image
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Width of the thumbnail in pixels
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the thumbnail in pixels
        /// </summary>
        public int Height { get; set; }
    }
}
