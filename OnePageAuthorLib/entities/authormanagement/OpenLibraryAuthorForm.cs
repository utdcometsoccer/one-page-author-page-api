namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class OpenLibraryAuthorForm : AuthorManagementBase
    {
        public string legend { get; set; } = string.Empty;
        public string key { get; set; } = string.Empty;
        public string text { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string alternateNames { get; set; } = string.Empty;
        public string birthDate { get; set; } = string.Empty;
        public string topWork { get; set; } = string.Empty;
        public string workCount { get; set; } = string.Empty;
        public string topSubjects { get; set; } = string.Empty;
        public string version { get; set; } = string.Empty;
        public string save { get; set; } = string.Empty;
        public string cancel { get; set; } = string.Empty;
    }
}
