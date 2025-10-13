using Microsoft.Azure.Cosmos;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.NoSQL
{
    /// <summary>
    /// Repository for StateProvince entities using string-based IDs, supports querying by code and name.
    /// </summary>
    public class StringStateProvinceRepository : StringGenericRepository<StateProvince>, IStringStateProvinceRepository
    {
        /// <summary>
        /// Initializes a new instance of the StringStateProvinceRepository class.
        /// </summary>
        /// <param name="container">The Cosmos DB container for StateProvince entities.</param>
        public StringStateProvinceRepository(Container container) : base(container)
        {
        }

        /// <summary>
        /// Gets a state or province by its ISO 3166-2 code.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code (e.g., "US-CA" for California).</param>
        /// <returns>The matching StateProvince entity, or null if not found.</returns>
        public async Task<StateProvince?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Code = @code")
                .WithParameter("@code", code);

            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Gets states or provinces by name (partial match, case-insensitive).
        /// </summary>
        /// <param name="name">The name or partial name of the state/province.</param>
        /// <returns>List of matching StateProvince entities.</returns>
        public async Task<IList<StateProvince>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new List<StateProvince>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE CONTAINS(UPPER(c.Name), UPPER(@name))")
                .WithParameter("@name", name);

            var results = new List<StateProvince>();
            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Gets all states or provinces for a specific country code.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <returns>List of StateProvince entities for the specified country.</returns>
        public async Task<IList<StateProvince>> GetByCountryAsync(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return new List<StateProvince>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Country = @countryCode")
                .WithParameter("@countryCode", countryCode.ToUpperInvariant());

            var results = new List<StateProvince>();
            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Checks if a state or province code exists in the database.
        /// </summary>
        /// <param name="code">The ISO 3166-2 code to check.</param>
        /// <returns>True if the code exists, false otherwise.</returns>
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var query = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.Code = @code")
                .WithParameter("@code", code);

            using var iterator = _container.GetItemQueryIterator<int>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault() > 0;
            }

            return false;
        }

        /// <summary>
        /// Gets states or provinces by culture code.
        /// </summary>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified culture.</returns>
        public async Task<IList<StateProvince>> GetByCultureAsync(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture))
                return new List<StateProvince>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Culture = @culture")
                .WithParameter("@culture", culture);

            var results = new List<StateProvince>();
            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Gets states or provinces by country code and culture.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <returns>List of StateProvince entities for the specified country and culture.</returns>
        public async Task<IList<StateProvince>> GetByCountryAndCultureAsync(string countryCode, string culture)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(culture))
                return new List<StateProvince>();

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Country = @countryCode AND c.Culture = @culture")
                .WithParameter("@countryCode", countryCode.ToUpperInvariant())
                .WithParameter("@culture", culture);

            var results = new List<StateProvince>();
            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }

        /// <summary>
        /// Gets a specific state or province by country code, culture, and code.
        /// </summary>
        /// <param name="countryCode">The two-letter country code (e.g., "US", "CA").</param>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <param name="code">The state/province code (e.g., "CA" for California, "ON" for Ontario).</param>
        /// <returns>The matching StateProvince entity, or null if not found.</returns>
        public async Task<StateProvince?> GetByCountryCultureAndCodeAsync(string countryCode, string culture, string code)
        {
            if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(culture) || string.IsNullOrWhiteSpace(code))
                return null;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Country = @countryCode AND c.Culture = @culture AND c.StateProvinceCode = @code")
                .WithParameter("@countryCode", countryCode.ToUpperInvariant())
                .WithParameter("@culture", culture)
                .WithParameter("@code", code.ToUpperInvariant());

            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Gets a specific state or province by culture and code (across all countries).
        /// </summary>
        /// <param name="culture">The culture code (e.g., "en-US", "fr-CA").</param>
        /// <param name="code">The state/province code (e.g., "CA" for California, "ON" for Ontario).</param>
        /// <returns>The matching StateProvince entity, or null if not found. If multiple matches exist across countries, returns the first one found.</returns>
        public async Task<StateProvince?> GetByCultureAndCodeAsync(string culture, string code)
        {
            if (string.IsNullOrWhiteSpace(culture) || string.IsNullOrWhiteSpace(code))
                return null;

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Culture = @culture AND c.StateProvinceCode = @code")
                .WithParameter("@culture", culture)
                .WithParameter("@code", code.ToUpperInvariant());

            using var iterator = _container.GetItemQueryIterator<StateProvince>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }
    }
}