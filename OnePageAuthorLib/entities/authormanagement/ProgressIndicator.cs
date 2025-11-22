namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ProgressIndicator : AuthorManagementBase
    {
        public string step { get; set; } = string.Empty;
        public string of { get; set; } = string.Empty;
        public string domainInput { get; set; } = string.Empty;
        public string contactInfo { get; set; } = string.Empty;
        public string selectPlan { get; set; } = string.Empty;
        public string payment { get; set; } = string.Empty;
    }
}
