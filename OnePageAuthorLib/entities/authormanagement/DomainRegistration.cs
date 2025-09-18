namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class DomainRegistration : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string subtitle { get; set; } = string.Empty;
        public string submit { get; set; } = string.Empty;
        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public string address2 { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public string zipCode { get; set; } = string.Empty;
        public string emailAddress { get; set; } = string.Empty;
        public string telephoneNumber { get; set; } = string.Empty;
    }
}
