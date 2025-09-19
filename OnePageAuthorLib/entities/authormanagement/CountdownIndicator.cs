namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class CountdownIndicator
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
        public string redirecting { get; set; } = string.Empty;
    }
}
