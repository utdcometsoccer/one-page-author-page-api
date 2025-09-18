namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class AuthorRegistration : AuthorManagementBase
    {
        public string authorListTitle { get; set; } = string.Empty;
        public string languageLabel { get; set; } = string.Empty;
        public string regionLabel { get; set; } = string.Empty;
        public string editAuthor { get; set; } = string.Empty;
        public string deleteAuthor { get; set; } = string.Empty;
        public string addAuthor { get; set; } = string.Empty;
        public string @continue { get; set; } = string.Empty;
    }
}
