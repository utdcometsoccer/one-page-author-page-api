using InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement;
namespace InkStainedWretch.OnePageAuthorAPI.Interfaces.Authormanagement
{
    /// <summary>
    /// Abstraction for retrieving localized author management UI text objects
    /// aggregated across multiple Cosmos DB containers for a given culture.
    /// </summary>
    public interface ILocalizationTextProvider
    {
        /// <summary>
        /// Retrieves a fully populated <see cref="LocalizationText"/> instance for the specified culture.
        /// </summary>
        /// <param name="culture">A valid culture string (e.g. "en-US").</param>
        /// <returns>A task that resolves to the localized text aggregate. Throws <see cref="ArgumentException"/> if the culture is invalid.</returns>
        Task<LocalizationText> GetLocalizationTextAsync(string culture);
    }
}
