using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public partial class LocaleRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_ReturnsLocale()
        {
            Guid localeId = Guid.NewGuid();
            string id = localeId.ToString();
            var locale1 = new Locale { LanguageName = "en", RegionName = "US", id = id };
            var locale2 = new Locale { LanguageName = "fr", RegionName = "FR", id = Guid.NewGuid().ToString() };
            var dataContainer = new TestGetByIdAsyncDataContainer(new List<Locale> { locale1, locale2 }, locale1.id);
            var repo = new LocaleRepository(dataContainer);
            var result = await repo.GetByIdAsync(locale1.id);
            Assert.NotNull(result);
            Assert.Equal(locale1.LanguageName, result.LanguageName);

        }

        [Fact]
        public async Task GetByLanguageAndRegionAsync_ReturnsLocales()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var locales = new List<Locale> {
                new Locale { LanguageName = "en", RegionName = "US" },
                new Locale { LanguageName = "en", RegionName = "GB" }
            };
            var iteratorMock = new Mock<FeedIterator<Locale>>();
            var responseMock = new Mock<FeedResponse<Locale>>();
            responseMock.Setup(r => r.Resource).Returns(locales);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Locale>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new LocaleRepository(cosmosMock.Object);
            var result = await repo.GetByLanguageAndRegionAsync("en", "US");
            Assert.NotNull(result);
            Assert.Contains(result, l => l.RegionName == "US");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsLocales()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var locales = new List<Locale> {
                new Locale { LanguageName = "en", RegionName = "US" },
                new Locale { LanguageName = "fr", RegionName = "FR" }
            };
            var iteratorMock = new Mock<FeedIterator<Locale>>();
            var responseMock = new Mock<FeedResponse<Locale>>();
            responseMock.Setup(r => r.Resource).Returns(locales);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Locale>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new LocaleRepository(cosmosMock.Object);
            var result = await repo.GetAllAsync();
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }
    }
}
