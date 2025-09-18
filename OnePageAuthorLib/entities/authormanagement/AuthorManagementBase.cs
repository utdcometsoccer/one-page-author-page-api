namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public abstract class AuthorManagementBase
    {
        public string id { get; set; } = System.Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
    }
}
