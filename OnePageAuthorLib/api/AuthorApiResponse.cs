namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents the response containing author data for the Author API endpoint.
    /// This matches the JSON structure specified in the API documentation.
    /// </summary>
    public class AuthorApiResponse
    {
        /// <summary>
        /// The unique identifier for the author.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The author's name.
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// The language name from the locale (e.g., "en" from "en-US").
        /// </summary>
        public string LanguageName { get; set; }

        /// <summary>
        /// The region name from the locale (e.g., "US" from "en-US").
        /// </summary>
        public string RegionName { get; set; }

        /// <summary>
        /// The author's email address.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// The welcome text displayed on the page.
        /// </summary>
        public string WelcomeText { get; set; }

        /// <summary>
        /// The about text describing the author or page.
        /// </summary>
        public string AboutText { get; set; }

        /// <summary>
        /// The URL to the author's headshot image.
        /// </summary>
        public string HeadShotURL { get; set; }

        /// <summary>
        /// The copyright text for the page.
        /// </summary>
        public string CopyrightText { get; set; }

        /// <summary>
        /// The top-level domain (e.g., "com", "org").
        /// </summary>
        public string TopLevelDomain { get; set; }

        /// <summary>
        /// The second-level domain name (e.g., "example" from "example.com").
        /// </summary>
        public string SecondLevelDomain { get; set; }

        /// <summary>
        /// The list of articles authored.
        /// </summary>
        public List<Article> Articles { get; set; }

        /// <summary>
        /// The list of books authored.
        /// </summary>
        public List<Book> Books { get; set; }

        /// <summary>
        /// The list of social media links.
        /// </summary>
        public List<SocialLink> Socials { get; set; }

        /// <summary>
        /// Default constructor. Initializes lists to empty and strings to empty.
        /// </summary>
        public AuthorApiResponse()
        {
            id = string.Empty;
            AuthorName = string.Empty;
            LanguageName = string.Empty;
            RegionName = string.Empty;
            EmailAddress = string.Empty;
            WelcomeText = string.Empty;
            AboutText = string.Empty;
            HeadShotURL = string.Empty;
            CopyrightText = string.Empty;
            TopLevelDomain = string.Empty;
            SecondLevelDomain = string.Empty;
            Articles = new List<Article>();
            Books = new List<Book>();
            Socials = new List<SocialLink>();
        }
    }
}