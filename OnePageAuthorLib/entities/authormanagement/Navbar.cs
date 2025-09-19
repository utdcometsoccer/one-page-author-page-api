namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class Navbar : AuthorManagementBase
    {
        public string brand { get; set; } = string.Empty;
        public string navigation { get; set; } = string.Empty;
        public string close { get; set; } = string.Empty;
        // navItems
        public string navItems_chooseCulture_label { get; set; } = string.Empty;
        public string navItems_chooseCulture_description { get; set; } = string.Empty;
        public string navItems_login_label { get; set; } = string.Empty;
        public string navItems_login_description { get; set; } = string.Empty;
        public string navItems_domainRegistration_label { get; set; } = string.Empty;
        public string navItems_domainRegistration_description { get; set; } = string.Empty;
        public string navItems_authorPage_label { get; set; } = string.Empty;
        public string navItems_authorPage_description { get; set; } = string.Empty;
        public string navItems_subscribe_label { get; set; } = string.Empty;
        public string navItems_subscribe_description { get; set; } = string.Empty;
        public string navItems_thankYou_label { get; set; } = string.Empty;
        public string navItems_thankYou_description { get; set; } = string.Empty;
    }
}
