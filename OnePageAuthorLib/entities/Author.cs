namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    public class Author
    {
        /// <summary>
        /// Indicates if this Author is the default value for a domain.
        /// </summary>
        public bool IsDefault { get; set; } = false;
        /// <summary>
        /// The primary key for the Author entity.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// The top-level domain (e.g., "com", "org").
        /// </summary>
        public required string TopLevelDomain { get; set; }

        /// <summary>
        /// The second-level domain name (e.g., "example" from "example.com").
        /// </summary>
        public required string SecondLevelDomain { get; set; }

        /// <summary>
        /// The language portion of the locale (e.g., "en" from "en-US").
        /// </summary>
        public required string LanguageName { get; set; }

        /// <summary>
        /// The region name from the locale (e.g., "US" from "en-US").
        /// </summary>
        public required string RegionName { get; set; }

        /// <summary>
        /// The author's name.
        /// </summary>
        public required string AuthorName { get; set; }

        /// <summary>
        /// The welcome text displayed on the page.
        /// </summary>
        public required string WelcomeText { get; set; }

        /// <summary>
        /// The about text describing the author or page.
        /// </summary>
        public required string AboutText { get; set; }

        /// <summary>
        /// The URL to the author's headshot image. Nullable.
        /// </summary>
        public string? HeadShotURL { get; set; }

        /// <summary>
        /// The copyright text for the page.
        /// </summary>
        public required string CopyrightText { get; set; }

        /// <summary>
        /// The author's email address.
        /// </summary>
        public required string EmailAddress { get; set; }

        /// <summary>
        /// Default constructor. Initializes all properties to empty strings except HeadShotURL, which is null.
        /// </summary>
        public Author()
        {
            id = string.Empty;
            TopLevelDomain = string.Empty;
            SecondLevelDomain = string.Empty;
            LanguageName = string.Empty;
            RegionName = string.Empty;
            AuthorName = string.Empty;
            WelcomeText = string.Empty;
            AboutText = string.Empty;
            HeadShotURL = null;
            CopyrightText = string.Empty;
            EmailAddress = string.Empty;
            IsDefault = false;
        }

        /// <summary>
        /// Constructor that initializes all required properties and optionally HeadShotURL.
        /// </summary>
        public Author(
            string id,
            string topLevelDomain,
            string secondLevelDomain,
            string languageName,
            string regionName,
            string authorName,
            string welcomeText,
            string aboutText,
            string copyrightText,
            string emailAddress,
            string? headShotURL = null,
            bool isDefault = false)
        {
            this.id = id;
            TopLevelDomain = topLevelDomain;
            SecondLevelDomain = secondLevelDomain;
            LanguageName = languageName;
            RegionName = regionName;
            AuthorName = authorName;
            WelcomeText = welcomeText;
            AboutText = aboutText;
            CopyrightText = copyrightText;
            EmailAddress = emailAddress;
            HeadShotURL = headShotURL;
            IsDefault = isDefault;
        }
    }
}
