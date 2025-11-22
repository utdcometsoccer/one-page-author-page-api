namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class DomainRegistrationsList : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
        public string loading { get; set; } = string.Empty;
        public string empty { get; set; } = string.Empty;
        public string select { get; set; } = string.Empty;
        public string selected { get; set; } = string.Empty;
    }
}
