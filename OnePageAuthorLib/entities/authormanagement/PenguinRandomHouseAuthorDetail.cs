namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    public class PenguinRandomHouseAuthorDetail
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Culture { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string score { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public string domain { get; set; } = string.Empty;
        public string titleField { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string authorFirst { get; set; } = string.Empty;
        public string authorLast { get; set; } = string.Empty;
        public string photoCredit { get; set; } = string.Empty;
        public string onTour { get; set; } = string.Empty;
        public string seriesAuthor { get; set; } = string.Empty;
        public string seriesIsbn { get; set; } = string.Empty;
        public string seriesCount { get; set; } = string.Empty;
        public string keywordId { get; set; } = string.Empty;
        public string save { get; set; } = string.Empty;
        public string cancel { get; set; } = string.Empty;
    }
}
