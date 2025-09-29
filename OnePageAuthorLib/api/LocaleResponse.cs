using System.Text.Json.Serialization;
namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Represents localized strings for the One Page Author API UI.
    /// </summary>
    public class LocaleResponse
    {
        /// <summary>
        /// Welcome text.
        /// </summary>
        [JsonPropertyName("welcome")]
        public string Welcome { get; set; }
        /// <summary>
        /// About Me text.
        /// </summary>
        [JsonPropertyName("aboutMe")]
        public string AboutMe { get; set; }
        /// <summary>
        /// My Books label.
        /// </summary>
        [JsonPropertyName("myBooks")]
        public string MyBooks { get; set; }
        /// <summary>
        /// Loading text.
        /// </summary>
        [JsonPropertyName("loading")]
        public string Loading { get; set; }
        /// <summary>
        /// Email prompt text.
        /// </summary>
        [JsonPropertyName("emailPrompt")]
        public string EmailPrompt { get; set; }
        /// <summary>
        /// Contact Me label.
        /// </summary>
        [JsonPropertyName("contactMe")]
        public string ContactMe { get; set; }
        /// <summary>
        /// Email link text.
        /// </summary>
        [JsonPropertyName("emailLinkText")]
        public string EmailLinkText { get; set; }
        /// <summary>
        /// No email fallback text.
        /// </summary>
        [JsonPropertyName("noEmail")]
        public string NoEmail { get; set; }
        /// <summary>
        /// Switch to Light Theme label.
        /// </summary>
        [JsonPropertyName("switchToLight")]
        public string SwitchToLight { get; set; }
        /// <summary>
        /// Switch to Dark Theme label.
        /// </summary>
        [JsonPropertyName("switchToDark")]
        public string SwitchToDark { get; set; }
        /// <summary>
        /// Articles label.
        /// </summary>
        [JsonPropertyName("articles")]
        public string Articles { get; set; }

        /// <summary>
        /// Default constructor. Initializes all properties to empty strings.
        /// </summary>
        public LocaleResponse()
        {
            Welcome = string.Empty;
            AboutMe = string.Empty;
            MyBooks = string.Empty;
            Loading = string.Empty;
            EmailPrompt = string.Empty;
            ContactMe = string.Empty;
            EmailLinkText = string.Empty;
            NoEmail = string.Empty;
            SwitchToLight = string.Empty;
            SwitchToDark = string.Empty;
            Articles = string.Empty;
        }

        /// <summary>
        /// Constructor that initializes all properties.
        /// </summary>
        public LocaleResponse(string welcome, string aboutMe, string myBooks, string loading, string emailPrompt, string contactMe, string emailLinkText, string noEmail, string switchToLight, string switchToDark, string articles)
        {
            Welcome = welcome;
            AboutMe = aboutMe;
            MyBooks = myBooks;
            Loading = loading;
            EmailPrompt = emailPrompt;
            ContactMe = contactMe;
            EmailLinkText = emailLinkText;
            NoEmail = noEmail;
            SwitchToLight = switchToLight;
            SwitchToDark = switchToDark;
            Articles = articles;
        }
    }
}
