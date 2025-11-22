namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ImageManager : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string loading { get; set; } = string.Empty;
        public string select { get; set; } = string.Empty;
        public string delete { get; set; } = string.Empty;
        public string refresh { get; set; } = string.Empty;
        public string uploadTitle { get; set; } = string.Empty;
        public string uploadText { get; set; } = string.Empty;
        public string uploadButton { get; set; } = string.Empty;
        public string supportedFormats { get; set; } = string.Empty;
        public string uploading { get; set; } = string.Empty;
        public string uploadSuccess { get; set; } = string.Empty;
    }
}
