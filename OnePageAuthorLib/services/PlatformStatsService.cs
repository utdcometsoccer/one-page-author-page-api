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
    /// Note: Uses static in-memory cache shared across all service instances to minimize database calls.
    /// </summary>
    public class PlatformStatsService : IPlatformStatsService
    {
        private readonly IPlatformStatsRepository _statsRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly IGenericRepository<Entities.Book> _bookRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ILogger<PlatformStatsService> _logger;
        
        // Static in-memory cache shared across all service instances (intentional design for simplicity)
        private static PlatformStats? _cachedStats;
        private static DateTime _cacheTimestamp = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
        private static readonly SemaphoreSlim _cacheSemaphore = new SemaphoreSlim(1, 1);

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
            // Check cache without lock first for performance
            if (_cachedStats != null && DateTime.UtcNow - _cacheTimestamp < CacheDuration)
            {
                _logger.LogInformation("Returning cached platform stats (age: {Age} minutes)", 
                    (DateTime.UtcNow - _cacheTimestamp).TotalMinutes);
                return _cachedStats;
            }

            // Use semaphore for async-compatible locking to prevent concurrent DB calls
            await _cacheSemaphore.WaitAsync();
            try
            {
                // Double-check cache after acquiring lock
                if (_cachedStats != null && DateTime.UtcNow - _cacheTimestamp < CacheDuration)
                {
                    _logger.LogInformation("Returning cached platform stats after lock (age: {Age} minutes)", 
                        (DateTime.UtcNow - _cacheTimestamp).TotalMinutes);
                    return _cachedStats;
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
                    UpdateCache(stats);

                    return stats;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching platform stats from database");
                    
                    // Return cached stats if available, even if expired
                    if (_cachedStats != null)
                    {
                        _logger.LogInformation("Returning stale cached stats due to error");
                        return _cachedStats;
                    }

                    // Return default if no cache available
                    return new PlatformStats();
                }
            }
            finally
            {
                _cacheSemaphore.Release();
            }
        }

        /// <summary>
        /// Updates the static cache in a centralized location.
        /// </summary>
        private static void UpdateCache(PlatformStats stats)
        {
            _cachedStats = stats;
            _cacheTimestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Clears the static cache. Intended for testing purposes only.
        /// </summary>
        internal static void ClearCache()
        {
            _cachedStats = null;
            _cacheTimestamp = DateTime.MinValue;
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
                // Count active authors
                var activeAuthors = await CountItemsAsync(_authorRepository, "Authors");
                _logger.LogInformation("Active authors: {Count}", activeAuthors);

                // Count books published
                var booksPublished = await CountItemsAsync(_bookRepository, "Books");
                _logger.LogInformation("Books published: {Count}", booksPublished);

                // Count countries served
                var countriesServed = await CountCountriesAsync();
                _logger.LogInformation("Countries served: {Count}", countriesServed);

                // Create updated stats
                // Note: TotalRevenue and AverageRating are not yet computed from real data
                var stats = new PlatformStats
                {
                    id = "current",
                    ActiveAuthors = activeAuthors,
                    BooksPublished = booksPublished,
                    TotalRevenue = 0, // TODO: Calculate from Stripe subscription data
                    AverageRating = 0, // TODO: Calculate from user ratings system
                    CountriesServed = countriesServed,
                    LastUpdated = DateTime.UtcNow.ToString("O")
                };

                // Save to database
                var savedStats = await _statsRepository.UpsertStatsAsync(stats);
                _logger.LogInformation("Platform statistics updated successfully");

                // Update cache
                UpdateCache(savedStats);

                return savedStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing platform statistics");
                throw;
            }
        }

        /// <summary>
        /// Helper method to count items using reflection to access container.
        /// This is a workaround to avoid changing repository interfaces.
        /// </summary>
        private async Task<int> CountItemsAsync<T>(T repository, string containerName)
        {
            try
            {
                // Try to get _container field via reflection
                var containerField = repository?.GetType()
                    .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (containerField == null)
                {
                    _logger.LogWarning("Cannot access _container field from {RepositoryType}, returning 0", 
                        repository?.GetType().Name ?? "null");
                    return 0;
                }

                var containerValue = containerField.GetValue(repository);
                
                // Handle both IDataContainer and Container types
                Container? container = null;
                if (containerValue is IDataContainer dataContainer)
                {
                    // Extract Container from IDataContainer wrapper
                    var wrapperContainerField = dataContainer.GetType()
                        .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    container = wrapperContainerField?.GetValue(dataContainer) as Container;
                }
                else if (containerValue is Container directContainer)
                {
                    container = directContainer;
                }

                if (container == null)
                {
                    _logger.LogWarning("Cannot extract Container from {RepositoryType}, returning 0", 
                        repository?.GetType().Name ?? "null");
                    return 0;
                }

                // Use COUNT query for efficiency
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
                _logger.LogError(ex, "Error counting items in {ContainerName}", containerName);
                return 0;
            }
        }

        private async Task<int> CountCountriesAsync()
        {
            try
            {
                // CountryRepository has direct Container access
                var containerField = _countryRepository.GetType()
                    .GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (containerField == null)
                {
                    _logger.LogWarning("Cannot access _container field from CountryRepository, returning 0");
                    return 0;
                }

                var container = containerField.GetValue(_countryRepository) as Container;
                if (container == null)
                {
                    _logger.LogWarning("Container from CountryRepository is null, returning 0");
                    return 0;
                }

                // Get distinct country codes
                var queryDefinition = new QueryDefinition("SELECT DISTINCT VALUE c.Code FROM c");
                var countries = new HashSet<string>();
                using var iterator = container.GetItemQueryIterator<string>(queryDefinition);
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var code in response.Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        countries.Add(code);
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
