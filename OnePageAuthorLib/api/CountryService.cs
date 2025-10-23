using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for managing Country operations with business logic and validation.
    /// </summary>
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _repository;
        private readonly ILogger<CountryService> _logger;

        public CountryService(
            ICountryRepository repository,
            ILogger<CountryService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all countries for a specific language code.
        /// </summary>
        public async Task<IList<Country>> GetCountriesByLanguageAsync(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                _logger.LogWarning("GetCountriesByLanguageAsync called with null or empty language");
                return new List<Country>();
            }

            // Normalize language code to lowercase
            language = language.ToLowerInvariant();

            _logger.LogInformation("Retrieving Countries for language: {Language}", language);
            
            try
            {
                return await _repository.GetByLanguageAsync(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Countries for language: {Language}", language);
                throw;
            }
        }

        /// <summary>
        /// Gets a country by its ISO code and language.
        /// </summary>
        public async Task<Country?> GetCountryByCodeAndLanguageAsync(string code, string language)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("GetCountryByCodeAndLanguageAsync called with null or empty code");
                return null;
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                _logger.LogWarning("GetCountryByCodeAndLanguageAsync called with null or empty language");
                return null;
            }

            // Validate country code format (should be 2 letters)
            if (code.Length != 2)
            {
                _logger.LogWarning("Invalid country code format: {Code}. Expected 2-letter code.", code);
                return null;
            }

            // Normalize to uppercase for code and lowercase for language
            code = code.ToUpperInvariant();
            language = language.ToLowerInvariant();

            _logger.LogInformation("Retrieving Country with code: {Code} and language: {Language}", code, language);
            
            try
            {
                return await _repository.GetByCodeAndLanguageAsync(code, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Country with code: {Code} and language: {Language}", code, language);
                throw;
            }
        }

        /// <summary>
        /// Creates a new country.
        /// </summary>
        public async Task<Country> CreateCountryAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            // Validate required fields
            ValidateCountry(country);

            // Normalize code and language
            country.Code = country.Code!.ToUpperInvariant();
            country.Language = country.Language!.ToLowerInvariant();

            // Check if country already exists
            if (await _repository.ExistsByCodeAndLanguageAsync(country.Code, country.Language))
            {
                throw new InvalidOperationException($"Country with code '{country.Code}' and language '{country.Language}' already exists");
            }

            // Generate ID if not provided
            if (string.IsNullOrWhiteSpace(country.id))
            {
                country.id = Guid.NewGuid().ToString();
            }

            _logger.LogInformation("Creating new Country with code: {Code} and language: {Language}", country.Code, country.Language);
            
            try
            {
                return await _repository.AddAsync(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Country with code: {Code}", country.Code);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing country.
        /// </summary>
        public async Task<Country> UpdateCountryAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            ValidateCountry(country);

            // Normalize code and language
            country.Code = country.Code!.ToUpperInvariant();
            country.Language = country.Language!.ToLowerInvariant();

            _logger.LogInformation("Updating Country with ID: {Id} and code: {Code}", country.id, country.Code);
            
            try
            {
                return await _repository.UpdateAsync(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Country with ID: {Id}", country.id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a country by its ID.
        /// </summary>
        public async Task<bool> DeleteCountryAsync(string id, string language)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));

            language = language.ToLowerInvariant();
            
            _logger.LogInformation("Deleting Country with ID: {Id} and language: {Language}", id, language);
            
            try
            {
                return await _repository.DeleteAsync(id, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Country with ID: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets all country entities.
        /// </summary>
        public async Task<IList<Country>> GetAllCountriesAsync()
        {
            _logger.LogInformation("Retrieving all Countries");
            
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all Countries");
                throw;
            }
        }

        /// <summary>
        /// Deletes all country entities.
        /// </summary>
        public async Task<int> DeleteAllCountriesAsync()
        {
            _logger.LogInformation("Deleting all Countries");
            
            try
            {
                var allCountries = await _repository.GetAllAsync();
                int deletedCount = 0;

                foreach (var country in allCountries)
                {
                    if (!string.IsNullOrEmpty(country.id) && !string.IsNullOrEmpty(country.Language))
                    {
                        var deleted = await DeleteCountryAsync(country.id, country.Language);
                        if (deleted)
                        {
                            deletedCount++;
                        }
                    }
                }

                _logger.LogInformation("Deleted {DeletedCount} Countries", deletedCount);
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Countries");
                throw;
            }
        }

        private static void ValidateCountry(Country country)
        {
            if (string.IsNullOrWhiteSpace(country.Code))
                throw new ArgumentException("Country Code is required", nameof(country));

            if (string.IsNullOrWhiteSpace(country.Name))
                throw new ArgumentException("Country Name is required", nameof(country));

            if (string.IsNullOrWhiteSpace(country.Language))
                throw new ArgumentException("Country Language is required", nameof(country));

            // Validate code format (should be 2 uppercase letters)
            if (country.Code.Length != 2)
                throw new ArgumentException($"Country Code must be 2 characters (ISO 3166-1 alpha-2): {country.Code}", nameof(country));
        }
    }
}
