namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class DomainInput
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
        public string label { get; set; } = string.Empty;
        public string placeholder { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
        public string success { get; set; } = string.Empty;
    }
}
