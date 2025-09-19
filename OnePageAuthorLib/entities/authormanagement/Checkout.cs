namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class Checkout : AuthorManagementBase
    {
        public string selectPlan { get; set; } = string.Empty;
        public string formTitle { get; set; } = string.Empty;
        public string errorMessage { get; set; } = string.Empty;
        public string buttonLabel { get; set; } = string.Empty;
    }
}
