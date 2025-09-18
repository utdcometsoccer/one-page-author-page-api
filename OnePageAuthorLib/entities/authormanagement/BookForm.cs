namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class BookForm : AuthorManagementBase
    {
        public string legend { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string cover { get; set; } = string.Empty;
        public string chooseImage { get; set; } = string.Empty;
        public string close { get; set; } = string.Empty;
        public string save { get; set; } = string.Empty;
        public string cancel { get; set; } = string.Empty;
    }
}
