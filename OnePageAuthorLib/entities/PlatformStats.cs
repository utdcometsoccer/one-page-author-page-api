namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Represents aggregated platform statistics for the landing page.
    /// This entity stores pre-computed values that are updated periodically.
    /// </summary>
    public class PlatformStats
    {
        /// <summary>
        /// The unique identifier for the stats record.
        /// Use a constant value like "current" to maintain a single record.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Total number of registered authors on the platform.
        /// </summary>
        public int ActiveAuthors { get; set; }

        /// <summary>
        /// Total number of books published on the platform.
        /// </summary>
        public int BooksPublished { get; set; }

        /// <summary>
        /// Total revenue earned by authors in USD.
        /// </summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>
        /// Platform average satisfaction rating (0.0 to 5.0).
        /// </summary>
        public double AverageRating { get; set; }

        /// <summary>
        /// Number of countries with active users.
        /// </summary>
        public int CountriesServed { get; set; }

        /// <summary>
        /// ISO 8601 timestamp of when the stats were last updated.
        /// </summary>
        public string LastUpdated { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PlatformStats()
        {
            id = "current";
            ActiveAuthors = 0;
            BooksPublished = 0;
            TotalRevenue = 0;
            AverageRating = 0.0;
            CountriesServed = 0;
            LastUpdated = DateTime.UtcNow.ToString("O");
        }

        /// <summary>
        /// Constructor with all properties.
        /// </summary>
        public PlatformStats(
            string id,
            int activeAuthors,
            int booksPublished,
            decimal totalRevenue,
            double averageRating,
            int countriesServed,
            string lastUpdated)
        {
            this.id = id;
            ActiveAuthors = activeAuthors;
            BooksPublished = booksPublished;
            TotalRevenue = totalRevenue;
            AverageRating = averageRating;
            CountriesServed = countriesServed;
            LastUpdated = lastUpdated;
        }
    }
}
