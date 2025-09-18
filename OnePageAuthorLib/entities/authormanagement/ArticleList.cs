namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ArticleList : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string date { get; set; } = string.Empty;
        public string publication { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string edit { get; set; } = string.Empty;
        public string delete { get; set; } = string.Empty;
        public string addArticle { get; set; } = string.Empty;
    }
}
