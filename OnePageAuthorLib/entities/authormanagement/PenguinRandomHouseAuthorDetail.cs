namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    /// <summary>
    /// Represents localized text strings for the Penguin Random House author detail interface.
    /// This class contains UI text and field labels that can be localized for different cultures/languages.
    /// Used when displaying detailed information about a specific author from Penguin Random House.
    /// Inherits from AuthorManagementBase to include Culture and id properties for localization support.
    /// </summary>
    public class PenguinRandomHouseAuthorDetail : AuthorManagementBase
    {
        /// <summary>
        /// The main title displayed at the top of the author detail page
        /// </summary>
        public string title { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's name field
        /// </summary>
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's relevance or ranking score field
        /// </summary>
        public string score { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's profile URL or website field
        /// </summary>
        public string url { get; set; } = string.Empty;

        /// <summary>
        /// Label for the domain or publisher field associated with the author
        /// </summary>
        public string domain { get; set; } = string.Empty;

        /// <summary>
        /// Label for the title field (possibly for author's works or professional title)
        /// </summary>
        public string titleField { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's biography or description field
        /// </summary>
        public string description { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's first name field
        /// </summary>
        public string authorFirst { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's last name field
        /// </summary>
        public string authorLast { get; set; } = string.Empty;

        /// <summary>
        /// Label for the author's photo credit information field
        /// </summary>
        public string photoCredit { get; set; } = string.Empty;

        /// <summary>
        /// Label for indicating if the author is currently on tour
        /// </summary>
        public string onTour { get; set; } = string.Empty;

        /// <summary>
        /// Label for the series author field (when author writes book series)
        /// </summary>
        public string seriesAuthor { get; set; } = string.Empty;

        /// <summary>
        /// Label for the series ISBN field (identifier for book series)
        /// </summary>
        public string seriesIsbn { get; set; } = string.Empty;

        /// <summary>
        /// Label for the series count field (number of books in a series)
        /// </summary>
        public string seriesCount { get; set; } = string.Empty;

        /// <summary>
        /// Label for the keyword ID field used for categorization or search
        /// </summary>
        public string keywordId { get; set; } = string.Empty;

        /// <summary>
        /// Text for the save button or action
        /// </summary>
        public string save { get; set; } = string.Empty;

        /// <summary>
        /// Text for the cancel button or action
        /// </summary>
        public string cancel { get; set; } = string.Empty;
    }
}
