using Microsoft.Azure.Cosmos;
using Moq;
using InkStainedWretch.OnePageAuthorAPI.NoSQL;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public class AuthorRepositoryTests
    {
        [Fact]
        public void ThrowsOnNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new AuthorRepository((IDataContainer)null!));
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsAuthor()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var authorId = Guid.NewGuid();
            var author = new Author
            {
                id = authorId.ToString(),
                AuthorName = "Test Author",
                TopLevelDomain = "com",
                SecondLevelDomain = "example",
                LanguageName = "en",
                RegionName = "us",
                WelcomeText = "Welcome!",
                AboutText = "About me.",
                CopyrightText = "Copyright",
                EmailAddress = "test@example.com"
            };
            cosmosMock.Setup(c => c.ReadItemAsync<Author>(It.IsAny<string>(), It.IsAny<PartitionKey>())).Returns(Task.FromResult(author));
            var repo = new AuthorRepository(cosmosMock.Object);
            var result = await repo.GetByIdAsync(authorId);
            cosmosMock.Verify(c => c.ReadItemAsync<Author>(It.IsAny<string>(), It.IsAny<PartitionKey>()), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(author.AuthorName, result.AuthorName);
        }

        [Fact]
        public async Task GetByDomainAndLocaleAsync_ReturnsAuthors()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var authors = new List<Author> {
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A1",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "a1@example.com"
                },
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A2",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "a2@example.com"
                }
            };
            var iteratorMock = new Mock<FeedIterator<Author>>();
            var responseMock = new Mock<FeedResponse<Author>>();
            responseMock.Setup(r => r.Resource).Returns(authors);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Author>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new AuthorRepository(cosmosMock.Object);
            var result = await repo.GetByDomainAndLocaleAsync("tld", "sld", "en", "us");
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByDomainAndDefaultAsync_ReturnsAuthors()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var authors = new List<Author> {
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A1",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "a1@example.com",
                    IsDefault = true
                },
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A2",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "a2@example.com"
                }
            };
            var iteratorMock = new Mock<FeedIterator<Author>>();
            var responseMock = new Mock<FeedResponse<Author>>();
            responseMock.Setup(r => r.Resource).Returns(authors);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Author>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new AuthorRepository(cosmosMock.Object);
            var result = await repo.GetByDomainAndDefaultAsync("tld", "sld");
            Assert.NotNull(result);
            Assert.Contains(result, a => a.IsDefault);
        }

        [Fact]
        public async Task GetByDomainAsync_ReturnsAuthors()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var authors = new List<Author> {
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A1",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "a1@example.com"
                },
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A2",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "a2@example.com"
                }
            };
            var iteratorMock = new Mock<FeedIterator<Author>>();
            var responseMock = new Mock<FeedResponse<Author>>();
            responseMock.Setup(r => r.Resource).Returns(authors);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Author>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new AuthorRepository(cosmosMock.Object);
            var result = await repo.GetByDomainAsync("tld", "sld");
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsAuthors()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var authors = new List<Author> {
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "A1",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About me.",
                    CopyrightText = "Copyright",
                    EmailAddress = "author@example.com"
                }
            };
            var iteratorMock = new Mock<FeedIterator<Author>>();
            var responseMock = new Mock<FeedResponse<Author>>();
            responseMock.Setup(r => r.Resource).Returns(authors);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);
            cosmosMock.Setup(c => c.GetItemQueryIterator<Author>(It.IsAny<QueryDefinition>(), null, null)).Returns(iteratorMock.Object);
            var repo = new AuthorRepository(cosmosMock.Object);
            var result = await repo.GetByEmailAsync("author@example.com");
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("author@example.com", result[0].EmailAddress);
        }

        [Fact]
        public async Task GetAllPagedAsync_ReturnsPagedAuthors()
        {
            var cosmosMock = new Mock<IDataContainer>();
            var authors = new List<Author> {
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "Alice",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "alice",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About Alice.",
                    CopyrightText = "Copyright",
                    EmailAddress = "alice@example.com"
                },
                new Author {
                    id = Guid.NewGuid().ToString(),
                    AuthorName = "Bob",
                    TopLevelDomain = "com",
                    SecondLevelDomain = "bob",
                    LanguageName = "en",
                    RegionName = "us",
                    WelcomeText = "Welcome!",
                    AboutText = "About Bob.",
                    CopyrightText = "Copyright",
                    EmailAddress = "bob@example.com"
                }
            };
            QueryDefinition? capturedQuery = null;
            var iteratorMock = new Mock<FeedIterator<Author>>();
            var responseMock = new Mock<FeedResponse<Author>>();
            responseMock.Setup(r => r.Resource).Returns(authors);
            iteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(responseMock.Object);
            cosmosMock
                .Setup(c => c.GetItemQueryIterator<Author>(It.IsAny<QueryDefinition>(), null, null))
                .Callback<QueryDefinition, string?, QueryRequestOptions?>((q, _, __) => capturedQuery = q)
                .Returns(iteratorMock.Object);
            var repo = new AuthorRepository(cosmosMock.Object);
            var result = await repo.GetAllPagedAsync(1, 10);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            cosmosMock.Verify(c => c.GetItemQueryIterator<Author>(It.IsAny<QueryDefinition>(), null, null), Times.Once);
            Assert.NotNull(capturedQuery);
            Assert.Contains("ORDER BY c.AuthorName, c.id", capturedQuery.QueryText);
        }
    }
}
