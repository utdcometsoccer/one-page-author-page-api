namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents a country with its ISO code and localized name.
    /// </summary>
    public class Country
    {
        /// <summary>
        /// Unique identifier for the country.
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// ISO 3166-1 alpha-2 country code (e.g., "US", "CA", "MX").
        /// </summary>
        public string? Code { get; set; }
        
        /// <summary>
        /// The localized name of the country (e.g., "United States").
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The language code associated with the country name (e.g., "en", "es", "fr").
        /// </summary>
        public string? Language { get; set; }
    }
}
