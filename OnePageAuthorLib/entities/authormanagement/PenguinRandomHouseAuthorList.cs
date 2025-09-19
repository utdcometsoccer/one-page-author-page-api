namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class PenguinRandomHouseAuthorList
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string import { get; set; } = string.Empty;
        public string importTitle { get; set; } = string.Empty;
        public string goBack { get; set; } = string.Empty;
        public string noResults { get; set; } = string.Empty;
    }
}
