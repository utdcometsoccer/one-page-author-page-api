namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class LoginRegister : AuthorManagementBase
    {
        public string loginHeaderTitle { get; set; } = string.Empty;
        public string loginHeaderSubtitle { get; set; } = string.Empty;
        public string loginButtonLabel { get; set; } = string.Empty;
        public string logoutButtonLabel { get; set; } = string.Empty;
        public string countdownIndicatorText { get; set; } = string.Empty;
    }
}
