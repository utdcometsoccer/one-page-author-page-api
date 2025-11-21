namespace InkStainedWretch.OnePageAuthorLib.API.Wikipedia
{
    /// <summary>
    /// Service interface for interacting with Wikipedia APIs
    /// </summary>
    public interface IWikipediaService
    {
        /// <summary>
        /// Gets structured facts about a person from Wikipedia
        /// </summary>
        /// <param name="personName">Name of the person to search for</param>
        /// <param name="language">Wikipedia language code (e.g., "en", "es", "fr")</param>
        /// <returns>Structured information about the person</returns>
        Task<WikipediaPersonFactsResponse> GetPersonFactsAsync(string personName, string language = "en");
    }
}
