namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ErrorBoundary : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string resetButton { get; set; } = string.Empty;
    }
}
