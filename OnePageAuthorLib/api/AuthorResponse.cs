namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents the response containing author data for the One Page Author API.
    /// </summary>
    public class AuthorResponse
    {
        /// <summary>
        /// The author's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The welcome text displayed on the page.
        /// </summary>
        public string Welcome { get; set; }

        /// <summary>
        /// The about text describing the author or page.
        /// </summary>
        public string AboutMe { get; set; }

        /// <summary>
        /// The URL to the author's headshot image.
        /// </summary>
        public string Headshot { get; set; }

        /// <summary>
        /// The list of books authored.
        /// </summary>
        public List<Book> Books { get; set; }

        /// <summary>
        /// The copyright text for the page.
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        /// The list of social media links.
        /// </summary>
        public List<SocialLink> Social { get; set; }

        /// <summary>
        /// The author's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The list of articles authored.
        /// </summary>
        public List<Article> Articles { get; set; }

        /// <summary>
        /// The explicitly curated featured book for the homepage hero experiment.
        /// Only populated when the author has a book with <c>IsFeaturedHeroBook = true</c>.
        /// Returns <c>null</c> when no featured book is configured, preserving the
        /// current homepage behavior for existing consumers.
        /// </summary>
        public FeaturedBookDto? FeaturedBook { get; set; }

        /// <summary>
        /// The homepage A/B experiment assignment for the current request.
        /// Defaults to <c>"control"</c> to preserve existing homepage behavior.
        /// </summary>
        public HomepageExperimentDto? Experiment { get; set; }

        /// <summary>
        /// Default constructor. Initializes lists to empty and strings to empty.
        /// </summary>
        public AuthorResponse()
        {
            Name = string.Empty;
            Welcome = string.Empty;
            AboutMe = string.Empty;
            Headshot = string.Empty;
            Books = new List<Book>();
            Copyright = string.Empty;
            Social = new List<SocialLink>();
            Email = string.Empty;
            Articles = new List<Article>();
        }

        /// <summary>
        /// Constructor that initializes all properties.
        /// </summary>
        public AuthorResponse(string name, string welcome, string aboutMe, string headshot, List<Book> books, string copyright, List<SocialLink> social, string email, List<Article> articles)
        {
            Name = name;
            Welcome = welcome;
            AboutMe = aboutMe;
            Headshot = headshot;
            Books = books ?? new List<Book>();
            Copyright = copyright;
            Social = social ?? new List<SocialLink>();
            Email = email;
            Articles = articles ?? new List<Article>();
        }
    }
}
