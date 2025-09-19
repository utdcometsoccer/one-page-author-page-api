namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class LoginRegister : AuthorManagementBase
    {
        // loginHeader
        public string loginHeader_title { get; set; } = string.Empty;
        public string loginHeader_subtitle { get; set; } = string.Empty;
        // loginButton
        public string loginButton_label { get; set; } = string.Empty;
        // logoutButton
        public string logoutButton_label { get; set; } = string.Empty;
        // countdownIndicator
        public string countdownIndicator_text { get; set; } = string.Empty;
    }
}
