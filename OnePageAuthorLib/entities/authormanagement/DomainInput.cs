namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class DomainInput : AuthorManagementBase
    {
        public string legend { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string placeholder { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
        public string success { get; set; } = string.Empty;
    }
}
