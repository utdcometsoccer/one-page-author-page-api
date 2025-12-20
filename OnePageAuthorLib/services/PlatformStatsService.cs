using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using Microsoft.Azure.Cosmos;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    /// <summary>
    /// Service for managing platform statistics with caching support.
    /// </summary>
    public class PlatformStatsService : IPlatformStatsService
    {
        private readonly IPlatformStatsRepository _statsRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly IGenericRepository<Entities.Book> _bookRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ILogger<PlatformStatsService> _logger;
        
        // In-memory cache
        private static PlatformStats? _cachedStats;
        private static DateTime _cacheTimestamp = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
        private static readonly object _cacheLock = new object();

        public PlatformStatsService(
            IPlatformStatsRepository statsRepository,
            IAuthorRepository authorRepository,
            IGenericRepository<Entities.Book> bookRepository,
            ICountryRepository countryRepository,
            ILogger<PlatformStatsService> logger)
        {
            _statsRepository = statsRepository ?? throw new ArgumentNullException(nameof(statsRepository));
            _authorRepository = authorRepository ?? throw new ArgumentNullException(nameof(authorRepository));
            _bookRepository = bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));
            _countryRepository = countryRepository ?? throw new ArgumentNullException(nameof(countryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current platform statistics with caching.
        /// Returns cached value if available and not expired (1 hour TTL).
        /// </summary>
        public async Task<PlatformStats> GetPlatformStatsAsync()
        {
            lock (_cacheLock)
            {
                // Check if cache is still valid
                if (_cachedStats != null && DateTime.UtcNow - _cacheTimestamp < CacheDuration)
                {
                    _logger.LogInformation("Returning cached platform stats (age: {Age} minutes)", 
                        (DateTime.UtcNow - _cacheTimestamp).TotalMinutes);
                    return _cachedStats;
                }
            }

            _logger.LogInformation("Cache miss or expired, fetching platform stats from database");

            try
            {
                var stats = await _statsRepository.GetCurrentStatsAsync();
                
                if (stats == null)
                {
                    _logger.LogWarning("No platform stats found in database, returning default values");
                    stats = new PlatformStats();
                }

                // Update cache
                lock (_cacheLock)
                {
                    _cachedStats = stats;
                    _cacheTimestamp = DateTime.UtcNow;
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching platform stats from database");
                
                // Return cached stats if available, even if expired
                lock (_cacheLock)
                {
                    if (_cachedStats != null)
                    {
                        _logger.LogInformation("Returning stale cached stats due to error");
                        return _cachedStats;
                    }
                }

                // Return default if no cache available
                return new PlatformStats();
            }
        }

        /// <summary>
        /// Computes and updates platform statistics by querying various data sources.
        /// This method should be called periodically (e.g., nightly) to refresh stats.
        /// </summary>
        public async Task<PlatformStats> ComputeAndUpdateStatsAsync()
        {
            _logger.LogInformation("Computing platform statistics...");

            try
            {
                // Count active authors by querying all authors
                var activeAuthors = await CountActiveAuthorsAsync();
                _logger.LogInformation("Active authors: {Count}", activeAuthors);

                // Count books published
                var booksPublished = await CountBooksAsync();
                _logger.LogInformation("Books published: {Count}", booksPublished);

                // Count countries served
                var countriesServed = await CountCountriesAsync();
                _logger.LogInformation("Countries served: {Count}", countriesServed);

                // Create updated stats
                var stats = new PlatformStats
                {
                    id = "current",
                    ActiveAuthors = activeAuthors,
                    BooksPublished = booksPublished,
                    TotalRevenue = 0, // TODO: Calculate from Stripe data
                    AverageRating = 4.8, // TODO: Calculate from user ratings
                    CountriesServed = countriesServed,
                    LastUpdated = DateTime.UtcNow.ToString("O")
                };

                // Save to database
                var savedStats = await _statsRepository.UpsertStatsAsync(stats);
                _logger.LogInformation("Platform statistics updated successfully");

                // Update cache
                lock (_cacheLock)
                {
                    _cachedStats = savedStats;
                    _cacheTimestamp = DateTime.UtcNow;
                }

                return savedStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing platform statistics");
                throw;
            }
        }

        private async Task<int> CountActiveAuthorsAsync()
        {
            try
            {
                // Access the container from the repository
                var repository = _authorRepository as GenericRepository<Author>;
                if (repository == null)
                {
                    _logger.LogWarning("Cannot cast author repository to GenericRepository, returning 0");
                    return 0;
                }

                var container = repository.GetType()
                    .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(repository) as IDataContainer;

                if (container == null)
                {
                    _logger.LogWarning("Cannot access container from repository, returning 0");
                    return 0;
                }

                var queryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                using var iterator = container.GetItemQueryIterator<int>(queryDefinition);
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting authors");
                return 0;
            }
        }

        private async Task<int> CountBooksAsync()
        {
            try
            {
                // Access the container from the repository
                var repository = _bookRepository as GenericRepository<Entities.Book>;
                if (repository == null)
                {
                    _logger.LogWarning("Cannot cast book repository to GenericRepository, returning 0");
                    return 0;
                }

                var container = repository.GetType()
                    .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(repository) as IDataContainer;

                if (container == null)
                {
                    _logger.LogWarning("Cannot access container from repository, returning 0");
                    return 0;
                }

                var queryDefinition = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                using var iterator = container.GetItemQueryIterator<int>(queryDefinition);
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting books");
                return 0;
            }
        }

        private async Task<int> CountCountriesAsync()
        {
            try
            {
                // Access the container from the repository
                var repository = _countryRepository as StringGenericRepository<Country>;
                if (repository == null)
                {
                    _logger.LogWarning("Cannot cast country repository to StringGenericRepository, returning 0");
                    return 0;
                }

                var container = repository.GetType()
                    .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(repository) as IDataContainer;

                if (container == null)
                {
                    _logger.LogWarning("Cannot access container from repository, returning 0");
                    return 0;
                }

                // Get distinct country codes
                var queryDefinition = new QueryDefinition("SELECT DISTINCT VALUE c.Code FROM c");
                var countries = new HashSet<string>();
                using var iterator = container.GetItemQueryIterator<string>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var code in response)
                    {
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            countries.Add(code);
                        }
                    }
                }
                
                return countries.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting countries");
                return 0;
            }
        }
    }
}
