using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public class LocaleRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_ReturnsLocale()
        {
            var containerMock = new Mock<Container>();
            var iteratorMock = new Mock<FeedIterator<Locale>>();
            var locale = new Locale { LanguageName = "en", RegionName = "US" };
            var responseMock = new Mock<FeedResponse<Locale>>();
            responseMock.Setup(r => r.Resource).Returns(new List<Locale> { locale });
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);
            containerMock.Setup(c => c.GetItemQueryIterator<Locale>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new LocaleRepository(containerMock.Object);
            var result = await repo.GetByIdAsync("1");
            Assert.NotNull(result);
            Assert.Equal(locale.LanguageName, result.LanguageName);
        }

        [Fact]
        public async Task GetByLanguageAndRegionAsync_ReturnsLocales()
        {
            var containerMock = new Mock<Container>();
            var iteratorMock = new Mock<FeedIterator<Locale>>();
            var locales = new List<Locale> {
                new Locale { LanguageName = "en", RegionName = "US" },
                new Locale { LanguageName = "en", RegionName = "GB" }
            };
            var responseMock = new Mock<FeedResponse<Locale>>();
            responseMock.Setup(r => r.Resource).Returns(locales);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);
            containerMock.Setup(c => c.GetItemQueryIterator<Locale>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new LocaleRepository(containerMock.Object);
            var result = await repo.GetByLanguageAndRegionAsync("en", "US");
            Assert.NotNull(result);
            Assert.Contains(result, l => l.RegionName == "US");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsLocales()
        {
            var containerMock = new Mock<Container>();
            var iteratorMock = new Mock<FeedIterator<Locale>>();
            var locales = new List<Locale> {
                new Locale { LanguageName = "en", RegionName = "US" },
                new Locale { LanguageName = "fr", RegionName = "FR" }
            };
            var responseMock = new Mock<FeedResponse<Locale>>();
            responseMock.Setup(r => r.Resource).Returns(locales);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(default)).ReturnsAsync(responseMock.Object);
            containerMock.Setup(c => c.GetItemQueryIterator<Locale>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new LocaleRepository(containerMock.Object);
            var result = await repo.GetAllAsync();
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }
    }
}
