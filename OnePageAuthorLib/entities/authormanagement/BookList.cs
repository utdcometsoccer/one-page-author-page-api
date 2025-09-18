namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class BookList : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string edit { get; set; } = string.Empty;
        public string delete { get; set; } = string.Empty;
        public string addBook { get; set; } = string.Empty;
        public string importOpenLibrary { get; set; } = string.Empty;
        public string importGoogleBooks { get; set; } = string.Empty;
        public string importPenguinBooks { get; set; } = string.Empty;
    }
}
