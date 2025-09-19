namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ImageManager : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string loading { get; set; } = string.Empty;
        public string select { get; set; } = string.Empty;
        public string delete { get; set; } = string.Empty;
        public string refresh { get; set; } = string.Empty;
    }
}
