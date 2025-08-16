namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents an article authored by the author.
    /// </summary>
    public class Article
    {
        /// <summary>
        /// The title of the article.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The URL to the article.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The publication in which the article appeared.
        /// </summary>
        public string Publication { get; set; }

        /// <summary>
        /// The date of publication.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Default constructor. Initializes strings to empty.
        /// </summary>
        public Article()
        {
            Title = string.Empty;
            Url = string.Empty;
            Publication = string.Empty;
            Date = string.Empty;
        }

        /// <summary>
        /// Constructor that initializes all properties.
        /// </summary>
        public Article(string title, string url, string publication, string date)
        {
            Title = title;
            Url = url;
            Publication = publication;
            Date = date;
        }
    }
}
