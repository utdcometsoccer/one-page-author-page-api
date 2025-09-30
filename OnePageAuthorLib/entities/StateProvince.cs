namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents a state or province with its code and name.
    /// </summary>
    public class StateProvince
    {
        /// <summary>
        /// Unique identifier for the state or province.
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// State or province code (e.g., "CA" for California, "ON" for Ontario).
        /// </summary>
        public string? Code { get; set; }
        
        /// <summary>
        /// The name of the state or province (e.g., "California").
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The country code (e.g., "US", "CA", "MX").
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// The culture code associated with the state or province (e.g., "en-US").
        /// </summary>
        public string? Culture { get; set; }
    }
}