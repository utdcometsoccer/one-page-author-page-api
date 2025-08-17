using System.Globalization;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Moq;

namespace OnePageAuthor.Test
{
    public class LocaleDataServiceTests
    {
        private readonly Mock<ILocaleRepository> _repoMock;
        private readonly LocaleDataService _service;

        public LocaleDataServiceTests()
        {
            _repoMock = new Mock<ILocaleRepository>();
            _service = new LocaleDataService(_repoMock.Object);
        }

        [Fact]
        public async Task GetLocalesAsync_DefaultCulture_ReturnsLocalesByLangAndRegion()
        {
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var region = CultureInfo.CurrentCulture.Name.Length > 3 ? CultureInfo.CurrentCulture.Name.Substring(3) : string.Empty;
            var expected = new List<Locale> { new Locale { LanguageName = lang, RegionName = region } };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, region)).ReturnsAsync(expected);

            var result = await _service.GetLocalesAsync();
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetLocalesAsync_DefaultCulture_FallbackToLanguage()
        {
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var region = CultureInfo.CurrentCulture.Name.Length > 3 ? CultureInfo.CurrentCulture.Name.Substring(3) : string.Empty;
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, region)).ReturnsAsync(new List<Locale>());
            var langLocales = new List<Locale> { new Locale { LanguageName = lang } };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, null)).ReturnsAsync(langLocales);

            var result = await _service.GetLocalesAsync();
            Assert.Equal(langLocales, result);
        }

        [Fact]
        public async Task GetLocalesAsync_DefaultCulture_FallbackToFirst()
        {
            var lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var region = CultureInfo.CurrentCulture.Name.Length > 3 ? CultureInfo.CurrentCulture.Name.Substring(3) : string.Empty;
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, region)).ReturnsAsync(new List<Locale>());
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, null)).ReturnsAsync(new List<Locale>());
            var allLocales = new List<Locale> { new Locale { LanguageName = "en" } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(allLocales);

            var result = await _service.GetLocalesAsync();
            Assert.Single(result);
            Assert.Equal(allLocales.First(), result.First());
        }

        [Fact]
        public async Task GetLocalesAsync_OnlyLanguage_ReturnsDefaultLocales()
        {
            var lang = "en";
            var locales = new List<Locale> {
                new Locale { LanguageName = lang, IsDefault = true },
                new Locale { LanguageName = lang, IsDefault = false }
            };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, null)).ReturnsAsync(locales);

            var result = await _service.GetLocalesAsync(lang);
            Assert.Single(result);
            Assert.True(result.First().IsDefault);
        }

        [Fact]
        public async Task GetLocalesAsync_OnlyLanguage_FallbackToFirst()
        {
            var lang = "en";
            var locales = new List<Locale> {
                new Locale { LanguageName = lang, IsDefault = false }
            };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, null)).ReturnsAsync(locales);

            var result = await _service.GetLocalesAsync(lang);
            Assert.Single(result);
            Assert.Equal(locales.First(), result.First());
        }

        [Fact]
        public async Task GetLocalesAsync_LanguageAndRegion_ReturnsFirst()
        {
            var lang = "en";
            var region = "US";
            var locales = new List<Locale> {
                new Locale { LanguageName = lang, RegionName = region }
            };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, region)).ReturnsAsync(locales);

            var result = await _service.GetLocalesAsync(lang, region);
            Assert.Single(result);
            Assert.Equal(locales.First(), result.First());
        }

        [Fact]
        public async Task GetLocalesAsync_LanguageAndRegion_FallbackToDefault()
        {
            var lang = "en";
            var region = "US";
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, region)).ReturnsAsync(new List<Locale>());
            var langLocales = new List<Locale> {
                new Locale { LanguageName = lang, IsDefault = true },
                new Locale { LanguageName = lang, IsDefault = false }
            };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, null)).ReturnsAsync(langLocales);

            var result = await _service.GetLocalesAsync(lang, region);
            Assert.Single(result);
            Assert.True(result.First().IsDefault);
        }

        [Fact]
        public async Task GetLocalesAsync_LanguageAndRegion_FallbackToFirst()
        {
            var lang = "en";
            var region = "US";
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, region)).ReturnsAsync(new List<Locale>());
            var langLocales = new List<Locale> {
                new Locale { LanguageName = lang, IsDefault = false }
            };
            _repoMock.Setup(r => r.GetByLanguageAndRegionAsync(lang, null)).ReturnsAsync(langLocales);

            var result = await _service.GetLocalesAsync(lang, region);
            Assert.Single(result);
            Assert.Equal(langLocales.First(), result.First());
        }
    }
}
