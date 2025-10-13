namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ChooseSubscription : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string selected { get; set; } = string.Empty;
        public string @continue { get; set; } = string.Empty;
        public string note { get; set; } = string.Empty;
    }
}
