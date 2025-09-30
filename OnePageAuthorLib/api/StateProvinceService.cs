using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    /// <summary>
    /// Service for managing StateProvince operations with business logic and validation.
    /// </summary>
    public class StateProvinceService : IStateProvinceService
    {
        private readonly IStateProvinceRepository _repository;
        private readonly ILogger<StateProvinceService> _logger;

        public StateProvinceService(
            IStateProvinceRepository repository,
            ILogger<StateProvinceService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a state or province by its ISO 3166-2 code.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code (e.g., "US-CA" for California).</param>
        /// <returns>The matching StateProvince entity, or null if not found.</returns>
        public async Task<StateProvince?> GetStateProvinceByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("GetStateProvinceByCodeAsync called with null or empty code");
                return null;
            }

            _logger.LogInformation("Retrieving StateProvince with code: {Code}", code);
            
            try
            {
                return await _repository.GetByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving StateProvince with code: {Code}", code);
                throw;
            }
        }

        /// <summary>
        /// Gets states or provinces by name (partial match, case-insensitive).
        /// </summary>
        /// <param name="name">The name or partial name of the state/province.</param>
        /// <returns>List of matching StateProvince entities.</returns>
        public async Task<IList<StateProvince>> SearchStateProvincesByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("SearchStateProvincesByNameAsync called with null or empty name");
                return new List<StateProvince>();
            }

            _logger.LogInformation("Searching StateProvinces with name containing: {Name}", name);
            
            try
            {
                return await _repository.GetByNameAsync(name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching StateProvinces with name: {Name}", name);
                throw;
            }
        }

        /// <summary>
        /// Gets all states or provinces for a specific country code.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <returns>List of StateProvince entities for the specified country.</returns>
        public async Task<IList<StateProvince>> GetStateProvincesByCountryAsync(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                _logger.LogWarning("GetStateProvincesByCountryAsync called with null or empty countryCode");
                return new List<StateProvince>();
            }

            // Validate country code format (should be 2 letters)
            if (countryCode.Length != 2)
            {
                _logger.LogWarning("Invalid country code format: {CountryCode}. Expected 2-letter code.", countryCode);
                return new List<StateProvince>();
            }

            _logger.LogInformation("Retrieving StateProvinces for country: {CountryCode}", countryCode);
            
            try
            {
                return await _repository.GetByCountryAsync(countryCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving StateProvinces for country: {CountryCode}", countryCode);
                throw;
            }
        }

        /// <summary>
        /// Validates if a state or province code is valid and exists.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code to validate.</param>
        /// <returns>True if the code is valid and exists, false otherwise.</returns>
        public async Task<bool> ValidateStateProvinceCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogDebug("ValidateStateProvinceCodeAsync called with null or empty code");
                return false;
            }

            // Validate ISO 3166-2 format (should contain a dash)
            if (!code.Contains('-'))
            {
                _logger.LogDebug("Invalid StateProvince code format: {Code}. Expected ISO 3166-2 format (e.g., US-CA).", code);
                return false;
            }

            _logger.LogDebug("Validating StateProvince code: {Code}", code);
            
            try
            {
                return await _repository.ExistsByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating StateProvince code: {Code}", code);
                return false;
            }
        }

        /// <summary>
        /// Creates a new state or province.
        /// </summary>
        /// <param name="stateProvince">The StateProvince entity to create.</param>
        /// <returns>The created StateProvince entity.</returns>
        public async Task<StateProvince> CreateStateProvinceAsync(StateProvince stateProvince)
        {
            if (stateProvince == null)
                throw new ArgumentNullException(nameof(stateProvince));

            // Validate required fields
            ValidateStateProvince(stateProvince);

            // Check if code already exists
            if (await _repository.ExistsByCodeAsync(stateProvince.Code!))
            {
                throw new InvalidOperationException($"StateProvince with code '{stateProvince.Code}' already exists");
            }

            // Generate ID if not provided
            if (string.IsNullOrWhiteSpace(stateProvince.id))
            {
                stateProvince.id = Guid.NewGuid().ToString();
            }

            _logger.LogInformation("Creating new StateProvince with code: {Code}", stateProvince.Code);
            
            try
            {
                return await _repository.AddAsync(stateProvince);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating StateProvince with code: {Code}", stateProvince.Code);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing state or province.
        /// </summary>
        /// <param name="stateProvince">The StateProvince entity to update.</param>
        /// <returns>The updated StateProvince entity.</returns>
        public async Task<StateProvince> UpdateStateProvinceAsync(StateProvince stateProvince)
        {
            if (stateProvince == null)
                throw new ArgumentNullException(nameof(stateProvince));

            ValidateStateProvince(stateProvince);

            _logger.LogInformation("Updating StateProvince with ID: {Id} and code: {Code}", stateProvince.id, stateProvince.Code);
            
            try
            {
                return await _repository.UpdateAsync(stateProvince);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating StateProvince with ID: {Id}", stateProvince.id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a state or province by its ID.
        /// </summary>
        /// <param name="id">The ID of the StateProvince to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        public async Task<bool> DeleteStateProvinceAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
            
            // Convert string ID to Guid if possible
            if (!Guid.TryParse(id, out var guidId))
                throw new ArgumentException("ID must be a valid Guid", nameof(id));

            _logger.LogInformation("Deleting StateProvince with ID: {Id}", id);
            
            try
            {
                return await _repository.DeleteAsync(guidId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting StateProvince with ID: {Id}", id);
                return false;
            }
        }

        private static void ValidateStateProvince(StateProvince stateProvince)
        {
            if (string.IsNullOrWhiteSpace(stateProvince.Code))
                throw new ArgumentException("StateProvince Code is required", nameof(stateProvince));

            if (string.IsNullOrWhiteSpace(stateProvince.Name))
                throw new ArgumentException("StateProvince Name is required", nameof(stateProvince));

            // Validate code format (should be 2-3 uppercase letters)
            if (stateProvince.Code.Length < 2 || stateProvince.Code.Length > 3)
                throw new ArgumentException($"StateProvince Code must be 2-3 characters (e.g., CA, TX, QC): {stateProvince.Code}", nameof(stateProvince));
        }

        /// <summary>
        /// Gets states or provinces by culture code.
        /// </summary>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified culture.</returns>
        public async Task<IList<StateProvince>> GetStateProvincesByCultureAsync(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
            {
                _logger.LogWarning("GetStateProvincesByCultureAsync called with null or empty culture");
                return new List<StateProvince>();
            }

            // Validate culture format (should be language-region, e.g., "en-US")
            if (!culture.Contains('-') || culture.Length < 5)
            {
                _logger.LogWarning("Invalid culture code format: {Culture}. Expected format like 'en-US'.", culture);
                return new List<StateProvince>();
            }

            _logger.LogInformation("Retrieving StateProvinces for culture: {Culture}", culture);
            
            try
            {
                return await _repository.GetByCultureAsync(culture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving StateProvinces for culture: {Culture}", culture);
                throw;
            }
        }

        /// <summary>
        /// Gets states or provinces by country code and culture.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified country and culture.</returns>
        public async Task<IList<StateProvince>> GetStateProvincesByCountryAndCultureAsync(string countryCode, string culture)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                _logger.LogWarning("GetStateProvincesByCountryAndCultureAsync called with null or empty countryCode");
                return new List<StateProvince>();
            }

            if (string.IsNullOrWhiteSpace(culture))
            {
                _logger.LogWarning("GetStateProvincesByCountryAndCultureAsync called with null or empty culture");
                return new List<StateProvince>();
            }

            // Validate country code format (should be 2 letters)
            if (countryCode.Length != 2)
            {
                _logger.LogWarning("Invalid country code format: {CountryCode}. Expected 2-letter code.", countryCode);
                return new List<StateProvince>();
            }

            // Validate culture format (should be language-region, e.g., "en-US")
            if (!culture.Contains('-') || culture.Length < 5)
            {
                _logger.LogWarning("Invalid culture code format: {Culture}. Expected format like 'en-US'.", culture);
                return new List<StateProvince>();
            }

            _logger.LogInformation("Retrieving StateProvinces for country: {CountryCode} and culture: {Culture}", countryCode, culture);
            
            try
            {
                return await _repository.GetByCountryAndCultureAsync(countryCode, culture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving StateProvinces for country: {CountryCode} and culture: {Culture}", countryCode, culture);
                throw;
            }
        }
    }
}