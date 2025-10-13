namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class SocialList : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string edit { get; set; } = string.Empty;
        public string delete { get; set; } = string.Empty;
        public string addSocial { get; set; } = string.Empty;
    }
}
