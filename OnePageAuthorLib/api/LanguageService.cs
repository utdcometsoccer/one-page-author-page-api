using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for managing Language operations with business logic and validation.
    /// </summary>
    public class LanguageService : ILanguageService
    {
        private readonly ILanguageRepository _repository;
        private readonly ILogger<LanguageService> _logger;

        public LanguageService(
            ILanguageRepository repository,
            ILogger<LanguageService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a language by its ISO 639-1 code and request language.
        /// </summary>
        /// <param name="code">The ISO 639-1 two-letter language code (e.g., "en", "es").</param>
        /// <param name="requestLanguage">The language in which to return the name (e.g., "en", "es").</param>
        /// <returns>The matching Language entity, or null if not found.</returns>
        public async Task<Language?> GetLanguageByCodeAsync(string code, string requestLanguage)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("GetLanguageByCodeAsync called with null or empty code");
                return null;
            }

            if (string.IsNullOrWhiteSpace(requestLanguage))
            {
                _logger.LogWarning("GetLanguageByCodeAsync called with null or empty requestLanguage");
                return null;
            }

            _logger.LogInformation("Retrieving Language with code: {Code}, requestLanguage: {RequestLanguage}", code, requestLanguage);
            
            try
            {
                return await _repository.GetByCodeAndRequestLanguageAsync(code, requestLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Language with code: {Code}, requestLanguage: {RequestLanguage}", code, requestLanguage);
                throw;
            }
        }

        /// <summary>
        /// Gets all languages localized for the specified request language.
        /// </summary>
        /// <param name="requestLanguage">The language in which to return names (e.g., "en", "es").</param>
        /// <returns>List of Language entities for the specified request language.</returns>
        public async Task<IList<Language>> GetLanguagesByRequestLanguageAsync(string requestLanguage)
        {
            if (string.IsNullOrWhiteSpace(requestLanguage))
            {
                _logger.LogWarning("GetLanguagesByRequestLanguageAsync called with null or empty requestLanguage");
                return new List<Language>();
            }

            // Validate language code format (should be 2 letters, or extended format like zh-CN)
            var normalizedLanguage = requestLanguage.ToLowerInvariant();
            if (normalizedLanguage.Length < 2)
            {
                _logger.LogWarning("Invalid language code format: {RequestLanguage}. Expected at least 2-letter code.", requestLanguage);
                return new List<Language>();
            }

            _logger.LogInformation("Retrieving Languages for requestLanguage: {RequestLanguage}", requestLanguage);
            
            try
            {
                return await _repository.GetByRequestLanguageAsync(normalizedLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Languages for requestLanguage: {RequestLanguage}", requestLanguage);
                throw;
            }
        }

        /// <summary>
        /// Validates if a language code is valid and exists.
        /// </summary>
        /// <param name="code">The ISO 639-1 code to validate.</param>
        /// <param name="requestLanguage">The request language context.</param>
        /// <returns>True if the code is valid and exists, false otherwise.</returns>
        public async Task<bool> ValidateLanguageCodeAsync(string code, string requestLanguage)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(requestLanguage))
            {
                _logger.LogWarning("ValidateLanguageCodeAsync called with null or empty parameters");
                return false;
            }

            _logger.LogInformation("Validating Language code: {Code}, requestLanguage: {RequestLanguage}", code, requestLanguage);
            
            try
            {
                return await _repository.ExistsByCodeAsync(code, requestLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Language code: {Code}, requestLanguage: {RequestLanguage}", code, requestLanguage);
                throw;
            }
        }

        /// <summary>
        /// Creates a new language.
        /// </summary>
        /// <param name="language">The Language entity to create.</param>
        /// <returns>The created Language entity.</returns>
        public async Task<Language> CreateLanguageAsync(Language language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            _logger.LogInformation("Creating new Language with code: {Code}", language.Code);
            
            try
            {
                language.id = Guid.NewGuid().ToString();
                return await _repository.AddAsync(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Language with code: {Code}", language.Code);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing language.
        /// </summary>
        /// <param name="language">The Language entity to update.</param>
        /// <returns>The updated Language entity.</returns>
        public async Task<Language> UpdateLanguageAsync(Language language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            _logger.LogInformation("Updating Language with id: {Id}", language.id);
            
            try
            {
                return await _repository.UpdateAsync(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Language with id: {Id}", language.id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a language by its ID.
        /// </summary>
        /// <param name="id">The ID of the Language to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        public async Task<bool> DeleteLanguageAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("DeleteLanguageAsync called with null or empty id");
                return false;
            }

            _logger.LogInformation("Deleting Language with id: {Id}", id);
            
            try
            {
                return await _repository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Language with id: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets all language entities.
        /// </summary>
        /// <returns>List of all Language entities.</returns>
        public async Task<IList<Language>> GetAllLanguagesAsync()
        {
            _logger.LogInformation("Retrieving all Languages");
            
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all Languages");
                throw;
            }
        }

        /// <summary>
        /// Deletes all language entities.
        /// </summary>
        /// <returns>The number of entities deleted.</returns>
        public async Task<int> DeleteAllLanguagesAsync()
        {
            _logger.LogWarning("DeleteAllLanguagesAsync called - this will delete all languages");
            
            try
            {
                var allLanguages = await _repository.GetAllAsync();
                int count = 0;
                
                foreach (var language in allLanguages)
                {
                    if (language.id != null && await _repository.DeleteAsync(language.id))
                    {
                        count++;
                    }
                }

                _logger.LogInformation("Deleted {Count} languages", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Languages");
                throw;
            }
        }
    }
}
