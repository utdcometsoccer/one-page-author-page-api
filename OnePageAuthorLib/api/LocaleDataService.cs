using System.Globalization;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace InkStainedWretch.OnePageAuthorAPI.API
{
    public class LocaleDataService : ILocaleDataService
    {
        private readonly ILocaleRepository _localeRepository;

        public LocaleDataService(ILocaleRepository localeRepository)
        {
            _localeRepository = localeRepository ?? throw new ArgumentNullException(nameof(localeRepository));
        }

    public async Task<List<Locale>> GetLocalesAsync(string? languageName = null, string? regionName = null)
        {
            // Default case: both null
            if (string.IsNullOrWhiteSpace(languageName) && string.IsNullOrWhiteSpace(regionName))
            {
                var culture = CultureInfo.CurrentCulture;
                var lang = culture.TwoLetterISOLanguageName;
                var region = culture.Name.Length > 3 ? culture.Name.Substring(3) : string.Empty;
                var locales = await _localeRepository.GetByLanguageAndRegionAsync(lang, region);
                if (locales != null && locales.Count > 0)
                    return locales.ToList();
                var langLocales = await _localeRepository.GetByLanguageAndRegionAsync(lang);
                if (langLocales != null && langLocales.Count > 0)
                    return langLocales.ToList();
                var allLocales = await _localeRepository.GetAllAsync();
                if (allLocales != null && allLocales.Count > 0)
                    return new List<Locale> { allLocales.First() };
                return new List<Locale>();
            }
            // Only language
            if (!string.IsNullOrWhiteSpace(languageName) && string.IsNullOrWhiteSpace(regionName))
            {
                var locales = await _localeRepository.GetByLanguageAndRegionAsync(languageName);
                var defaultLocales = locales?.Where(l => l.IsDefault).ToList() ?? new List<Locale>();
                if (defaultLocales.Count > 0)
                    return defaultLocales;
                if (locales != null && locales.Count > 0)
                    return new List<Locale> { locales.First() };
                return new List<Locale>();
            }
            // Language and region
            if (!string.IsNullOrWhiteSpace(languageName) && !string.IsNullOrWhiteSpace(regionName))
            {
                var locales = await _localeRepository.GetByLanguageAndRegionAsync(languageName, regionName);
                if (locales != null && locales.Count > 0)
                    return new List<Locale> { locales.First() };
                // Fallback to language only
                var langLocales = await _localeRepository.GetByLanguageAndRegionAsync(languageName);
                var defaultLocales = langLocales?.Where(l => l.IsDefault).ToList() ?? new List<Locale>();
                if (defaultLocales.Count > 0)
                    return defaultLocales;
                if (langLocales != null && langLocales.Count > 0)
                    return new List<Locale> { langLocales.First() };
                return new List<Locale>();
            }
            // Should not reach here
            return new List<Locale>();
        }
    }
}
