namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class ChooseCulture : AuthorManagementBase
    {    
        public string title { get; set; } = string.Empty;
        public string subtitle { get; set; } = string.Empty;
        public string legend { get; set; } = string.Empty;
        public string languageLabel { get; set; } = string.Empty;
        public string countryLabel { get; set; } = string.Empty;
        public string @continue { get; set; } = string.Empty;
        public string cancel { get; set; } = string.Empty;
        public string cookieConsent { get; set; } = string.Empty;
        public string cookiesInfo { get; set; } = string.Empty;
    }
}
