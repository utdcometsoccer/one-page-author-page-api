using System.Globalization;
using InkStainedWretch.OnePageAuthorAPI.Interfaces.Authormanagement;
using Microsoft.Azure.Cosmos;


namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    
    /// <summary>
    /// Default implementation of <see cref="ILocalizationTextProvider"/> that aggregates
    /// localized UI fragments from individual Cosmos DB containers into a single
    /// <see cref="LocalizationText"/> object for a requested culture.
    /// </summary>
    public class LocalizationTextProvider : ILocalizationTextProvider
    {
        private readonly Database _database;

        /// <summary>
        /// Creates a new <see cref="LocalizationTextProvider"/>.
        /// </summary>
        /// <param name="database">The Cosmos <see cref="Database"/> instance used to resolve containers.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="database"/> is null.</exception>
        public LocalizationTextProvider(Database database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
    /// Retrieves localized text values for the specified culture across all
    /// registered author-management containers. Resolution order per segment:
    /// 1. Exact culture (e.g. en-GB)
    /// 2. First document whose Culture begins with the same language code + '-' (e.g. en-US if en-GB missing)
    /// 3. Neutral language (exact language only, e.g. en) if stored as such
    /// 4. Empty placeholder object (never null) with Culture set to requested specific culture
        /// </summary>
        /// <param name="culture">Culture code (e.g. "en-US"). Must be a valid .NET culture.</param>
        /// <returns>A populated <see cref="LocalizationText"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="culture"/> is null/empty or invalid.</exception>
        public async Task<LocalizationText> GetLocalizationTextAsync(string culture)
        {
            if (string.IsNullOrWhiteSpace(culture) || !IsValidCulture(culture))
                throw new ArgumentException($"Invalid culture string: {culture}", nameof(culture));

            var result = new LocalizationText();
            var specific = culture;
            var language = GetNeutralCulture(culture) ?? culture; // language code

            result.AuthorRegistration = await QueryLanguageResolutionAsync<AuthorRegistration>("AuthorRegistration", specific, language);
            result.LoginRegister = await QueryLanguageResolutionAsync<LoginRegister>("LoginRegister", specific, language);
            result.ThankYou = await QueryLanguageResolutionAsync<ThankYou>("ThankYou", specific, language);
            result.Navbar = await QueryLanguageResolutionAsync<Navbar>("Navbar", specific, language);
            result.DomainRegistration = await QueryLanguageResolutionAsync<DomainRegistration>("DomainRegistration", specific, language);
            result.ErrorPage = await QueryLanguageResolutionAsync<ErrorPage>("ErrorPage", specific, language);
            result.ImageManager = await QueryLanguageResolutionAsync<ImageManager>("ImageManager", specific, language);
            result.Checkout = await QueryLanguageResolutionAsync<Checkout>("Checkout", specific, language);
            result.BookList = await QueryLanguageResolutionAsync<BookList>("BookList", specific, language);
            result.BookForm = await QueryLanguageResolutionAsync<BookForm>("BookForm", specific, language);
            result.ArticleForm = await QueryLanguageResolutionAsync<ArticleForm>("ArticleForm", specific, language);
            // Add additional POCOs as needed

            return result;
        }

        /// <summary>
        /// Determines whether the provided culture string maps to a valid <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The culture identifier to validate.</param>
        /// <returns><c>true</c> if valid; otherwise <c>false</c>.</returns>
        private static bool IsValidCulture(string culture)
        {
            try
            {
                var _ = CultureInfo.GetCultureInfo(culture);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }

        /// <summary>
        /// Queries a specific container for a single localized document matching the supplied culture partition key.
        /// Returns a new empty instance if none is found.
        /// </summary>
        /// <typeparam name="T">The POCO type stored in the container.</typeparam>
        /// <param name="containerName">Name of the Cosmos container.</param>
        /// <param name="partitionKey">The culture partition key (e.g. en-US).</param>
        /// <returns>The found document or a new instance with Culture set.</returns>
        private async Task<T> QueryContainerAsync<T>(string containerName, string partitionKey) where T : AuthorManagementBase, new()
        {
            var container = _database.GetContainer(containerName);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Culture = @culture").WithParameter("@culture", partitionKey);
            var iterator = container.GetItemQueryIterator<T>(query);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var item in response)
                {
                    return item;
                }
            }
            return new T { Culture = partitionKey };
        }

        /// <summary>
        /// <summary>
        /// Attempts resolution by: exact culture -> first matching language-region -> neutral language -> empty.
        /// </summary>
        private async Task<T> QueryLanguageResolutionAsync<T>(string containerName, string specific, string language) where T : AuthorManagementBase, new()
        {
            // 1. Exact culture
            var exact = await QueryContainerAsync<T>(containerName, specific);
            if (!IsEmpty(exact)) return exact;

            // 2. Any document whose Culture starts with language- (other than the specific tried)
            var container = _database.GetContainer(containerName);
            var langPrefix = language + "-";
            var langQuery = new QueryDefinition("SELECT * FROM c WHERE STARTSWITH(c.Culture, @pfx)").WithParameter("@pfx", langPrefix);
            var iterator = container.GetItemQueryIterator<T>(langQuery, requestOptions: new QueryRequestOptions { MaxItemCount = 5 });
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                foreach (var candidate in page)
                {
                    if (!candidate.Culture.Equals(specific, StringComparison.OrdinalIgnoreCase))
                    {
                        candidate.Culture = specific; // normalize to requested
                        return candidate;
                    }
                }
            }

            // 3. Neutral language (if stored as such)
            var neutral = await QueryContainerAsync<T>(containerName, language);
            if (!IsEmpty(neutral))
            {
                neutral.Culture = specific;
                return neutral;
            }

            // 4. Empty placeholder with requested culture
            return new T { Culture = specific };
        }

        private static bool IsEmpty(AuthorManagementBase item)
        {
            // Reflection-based check: any public instance string property (excluding id, Culture) with non-empty value marks it non-empty
            var props = item.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var p in props)
            {
                if (p.Name is nameof(AuthorManagementBase.id) or nameof(AuthorManagementBase.Culture)) continue;
                if (p.PropertyType == typeof(string))
                {
                    var val = p.GetValue(item) as string;
                    if (!string.IsNullOrEmpty(val)) return false; // not empty
                }
            }
            return true; // all empty
        }

        private static string? GetNeutralCulture(string culture)
        {
            var parts = culture.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 1 ? parts[0] : null;
        }
    }
}
