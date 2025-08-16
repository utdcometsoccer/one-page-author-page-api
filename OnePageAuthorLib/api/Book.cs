namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents a book authored by the author.
    /// </summary>
    public class Book
    {
        /// <summary>
        /// The title of the book.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The description of the book.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The URL to the book.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The URL to the book's cover image.
        /// </summary>
        public string Cover { get; set; }

        /// <summary>
        /// Default constructor. Initializes strings to empty.
        /// </summary>
        public Book()
        {
            Title = string.Empty;
            Description = string.Empty;
            Url = string.Empty;
            Cover = string.Empty;
        }

        /// <summary>
        /// Constructor that initializes all properties.
        /// </summary>
        public Book(string title, string description, string url, string cover)
        {
            Title = title;
            Description = description;
            Url = url;
            Cover = cover;
        }
    }
}
