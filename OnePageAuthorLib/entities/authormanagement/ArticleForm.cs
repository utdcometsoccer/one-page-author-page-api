namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ArticleForm : AuthorManagementBase
    {
        public string legend { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string date { get; set; } = string.Empty;
        public string publication { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string save { get; set; } = string.Empty;
        public string cancel { get; set; } = string.Empty;
    }
}
