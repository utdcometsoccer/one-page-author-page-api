namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ErrorPage : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string devMessage { get; set; } = string.Empty;
        public string details { get; set; } = string.Empty;
        public string tryAgain { get; set; } = string.Empty;
        public string userMessage { get; set; } = string.Empty;
        public string @return { get; set; } = string.Empty;
    }
}
