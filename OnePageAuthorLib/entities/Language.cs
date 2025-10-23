namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents a language with its ISO 639-1 code and localized name.
    /// </summary>
    public class Language
    {
        /// <summary>
        /// Unique identifier for the language.
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// ISO 639-1 two-letter language code (e.g., "en", "es", "fr", "ar", "zh").
        /// </summary>
        public string? Code { get; set; }
        
        /// <summary>
        /// The name of the language in the requested language (e.g., "English" for en-US, "Inglés" for es-MX).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The language code for which this name is localized (e.g., "en", "es").
        /// This is the partition key for the Cosmos DB container.
        /// </summary>
        public string? RequestLanguage { get; set; }
    }
}
