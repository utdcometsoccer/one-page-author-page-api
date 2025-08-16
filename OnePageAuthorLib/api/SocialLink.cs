namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents a social media link for the author.
    /// </summary>
    public class SocialLink
    {
        /// <summary>
        /// The name of the social media platform.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URL to the social media profile.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Default constructor. Initializes strings to empty.
        /// </summary>
        public SocialLink()
        {
            Name = string.Empty;
            Url = string.Empty;
        }

        /// <summary>
        /// Constructor that initializes all properties.
        /// </summary>
        public SocialLink(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}
