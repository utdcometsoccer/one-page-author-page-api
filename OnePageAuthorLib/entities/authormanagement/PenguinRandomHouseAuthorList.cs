namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    /// <summary>
    /// Represents localized text strings for the Penguin Random House author list interface.
    /// This class contains UI text that can be localized for different cultures/languages.
    /// Inherits from AuthorManagementBase to include Culture and id properties for localization support.
    /// </summary>
    public class PenguinRandomHouseAuthorList : AuthorManagementBase
    {
        /// <summary>
        /// The main title displayed at the top of the author list interface
        /// </summary>
        public string title { get; set; } = string.Empty;

        /// <summary>
        /// Text for the import button or action that allows importing author data
        /// </summary>
        public string import { get; set; } = string.Empty;

        /// <summary>
        /// Title text displayed in the import dialog or section
        /// </summary>
        public string importTitle { get; set; } = string.Empty;

        /// <summary>
        /// Text for the "go back" navigation button or link
        /// </summary>
        public string goBack { get; set; } = string.Empty;

        /// <summary>
        /// Message displayed when no search results are found in the author list
        /// </summary>
        public string noResults { get; set; } = string.Empty;
    }
}
