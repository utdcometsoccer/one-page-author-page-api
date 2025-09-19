namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class SocialForm
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
        public string legend { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string save { get; set; } = string.Empty;
        public string cancel { get; set; } = string.Empty;
    }
}
