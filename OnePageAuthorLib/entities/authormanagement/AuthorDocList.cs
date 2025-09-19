namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class AuthorDocList : AuthorManagementBase
    {
        public string title { get; set; } = string.Empty;
        public string topWork { get; set; } = string.Empty;
        public string birthDate { get; set; } = string.Empty;
        public string import { get; set; } = string.Empty;
        public string importTitle { get; set; } = string.Empty;
        public string goBack { get; set; } = string.Empty;
    }
}
