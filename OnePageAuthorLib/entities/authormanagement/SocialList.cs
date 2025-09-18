namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class SocialList
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string edit { get; set; } = string.Empty;
        public string delete { get; set; } = string.Empty;
        public string addSocial { get; set; } = string.Empty;
    }
}
