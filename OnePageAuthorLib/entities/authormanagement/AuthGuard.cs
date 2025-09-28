using System.Text.Json.Serialization;


namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    /// <summary>
    /// Represents authentication guard information for UI display when user authentication is required
    /// </summary>
    public class AuthGuard : AuthorManagementBase
    {
        /// <summary>
        /// Title to display in the authentication prompt
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Message to display explaining why authentication is required
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Label for the authentication button
        /// </summary>
        [JsonPropertyName("buttonLabel")]
        public string ButtonLabel { get; set; } = string.Empty;

        /// <summary>
        /// Default constructor
        /// </summary>
        public AuthGuard()
        {
        }

        /// <summary>
        /// Constructor with all properties
        /// </summary>
        /// <param name="title">Title for the authentication prompt</param>
        /// <param name="message">Message explaining authentication requirement</param>
        /// <param name="buttonLabel">Label for the authentication button</param>
        public AuthGuard(string title, string message, string buttonLabel)
        {
            Title = title;
            Message = message;
            ButtonLabel = buttonLabel;
        }

        /// <summary>
        /// Creates a default AuthGuard instance with standard authentication messages
        /// </summary>
        /// <returns>AuthGuard with default authentication prompt content</returns>
        public static AuthGuard CreateDefault()
        {
            return new AuthGuard
            {
                Title = "Authentication Required",
                Message = "Please sign in to access this content. You'll need to authenticate to view and manage your author information.",
                ButtonLabel = "Sign in with Microsoft"
            };
        }
    }
}