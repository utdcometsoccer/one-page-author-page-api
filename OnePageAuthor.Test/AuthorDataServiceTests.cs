using Moq;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;

namespace OnePageAuthor.Test
{
    public class AuthorDataServiceTests
    {
        private Mock<IAuthorRepository> _authorRepoMock;
        private Mock<InkStainedWretch.OnePageAuthorAPI.API.IGenericRepository<InkStainedWretch.OnePageAuthorAPI.Entities.Book>> _bookRepoMock;
        private Mock<InkStainedWretch.OnePageAuthorAPI.API.IGenericRepository<InkStainedWretch.OnePageAuthorAPI.Entities.Article>> _articleRepoMock;
        private Mock<IGenericRepository<Social>> _socialRepoMock;
        private AuthorDataService _service;

        public AuthorDataServiceTests()
        {
            _authorRepoMock = new Mock<IAuthorRepository>();
            _bookRepoMock = new Mock<InkStainedWretch.OnePageAuthorAPI.API.IGenericRepository<InkStainedWretch.OnePageAuthorAPI.Entities.Book>>();
            _articleRepoMock = new Mock<InkStainedWretch.OnePageAuthorAPI.API.IGenericRepository<InkStainedWretch.OnePageAuthorAPI.Entities.Article>>();
            _socialRepoMock = new Mock<IGenericRepository<Social>>();
            _service = new AuthorDataService(
                _authorRepoMock.Object,
                _bookRepoMock.Object,
                _articleRepoMock.Object,
                _socialRepoMock.Object);
        }

        private static List<Author> CreateTestAuthors(int count) =>
            Enumerable.Range(1, count).Select(i => new Author
            {
                id = Guid.NewGuid().ToString(),
                AuthorName = $"Author {i}",
                EmailAddress = $"author{i}@example.com",
                WelcomeText = $"Welcome {i}",
                AboutText = $"About {i}",
                CopyrightText = $"Copyright {i}",
                LanguageName = "en",
                RegionName = "US",
                TopLevelDomain = "com",
                SecondLevelDomain = $"example{i}"
            }).ToList();

        private void SetupEmptyRelatedRepos()
        {
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Book>());
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Article>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());
        }

        [Fact]
        public async Task GetAuthorWithDataAsync_ReturnsNull_WhenNoAuthorFound()
        {
            _authorRepoMock.Setup(r => r.GetByDomainAndLocaleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author>());
            var result = await _service.GetAuthorWithDataAsync("com", "example", "en");
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAuthorWithDataAsync_ReturnsAuthorResponse_WhenAuthorFound()
        {
            var authorId = Guid.NewGuid().ToString();
            var author = new InkStainedWretch.OnePageAuthorAPI.Entities.Author
            {
                id = authorId,
                TopLevelDomain = "com",
                SecondLevelDomain = "example",
                LanguageName = "en",
                RegionName = "US",
                AuthorName = "Test Author",
                WelcomeText = "Welcome!",
                AboutText = "About me.",
                HeadShotURL = "http://headshot.url",
                CopyrightText = "Copyright",
                EmailAddress = "author@example.com"
            };
            _authorRepoMock.Setup(r => r.GetByDomainAndLocaleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author> { author });
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Book> {
                    new InkStainedWretch.OnePageAuthorAPI.Entities.Book { Title = "Book1", Description = "Desc1", URL = new Uri("http://book1.url"), Cover = new Uri("http://cover1.url") }
                });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Article> {
                    new InkStainedWretch.OnePageAuthorAPI.Entities.Article { Title = "Art1", Date = DateTime.Now, Publication = "Pub1", URL = new Uri("http://art1.url") }
                });
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social> {
                    new Social { Name = "Twitter", URL = new Uri("http://twitter.com") }
                });
            var result = await _service.GetAuthorWithDataAsync("com", "example", "en");
            Assert.NotNull(result);
            Assert.Equal("Test Author", result.Name);
            Assert.Single(result.Books);
            Assert.Single(result.Articles);
            Assert.Single(result.Social);
        }

        [Fact]
        public void ConvertToApiBooks_ReturnsCorrectApiBooks()
        {
            var books = new List<InkStainedWretch.OnePageAuthorAPI.Entities.Book> {
                new InkStainedWretch.OnePageAuthorAPI.Entities.Book { Title = "Book1", Description = "Desc1", URL = new Uri("http://book1.url"), Cover = new Uri("http://cover1.url") },
                new InkStainedWretch.OnePageAuthorAPI.Entities.Book { Title = "Book2", Description = "Desc2", URL = new Uri("https://example.com"), Cover = new Uri("https://example.com") }
            };
            var apiBooks = AuthorDataService.ConvertToApiBooks(books);
            Assert.Equal(2, apiBooks.Count);
            Assert.Equal("Book1", apiBooks[0].Title);
            Assert.Equal("https://example.com/", apiBooks[1].Url);
        }

        [Fact]
        public void ConvertToApiArticles_ReturnsCorrectApiArticles()
        {
            var articles = new List<InkStainedWretch.OnePageAuthorAPI.Entities.Article> {
                new InkStainedWretch.OnePageAuthorAPI.Entities.Article { Title = "Art1", Date = new DateTime(2020,1,1), Publication = "Pub1", URL = new Uri("http://art1.url") },
                new InkStainedWretch.OnePageAuthorAPI.Entities.Article { Title = "Art2", Date = new DateTime(2021,2,2), Publication = "Pub2", URL = new Uri("https://example.com") }
            };
            var apiArticles = AuthorDataService.ConvertToApiArticles(articles);
            Assert.Equal(2, apiArticles.Count);
            Assert.Equal("2020-01-01", apiArticles[0].Date);
            Assert.Equal("https://example.com/", apiArticles[1].Url);
        }

        [Fact]
        public async Task GetAuthorsByDomainAsync_ReturnsEmptyList_WhenNoAuthorsFound()
        {
            _authorRepoMock.Setup(r => r.GetByDomainAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author>());
            
            var result = await _service.GetAuthorsByDomainAsync("com", "example");
            
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAuthorsByDomainAsync_ReturnsAuthorApiResponses_WhenAuthorsFound()
        {
            var authorId1 = Guid.NewGuid().ToString();
            var authorId2 = Guid.NewGuid().ToString();
            
            var authors = new List<Author>
            {
                new Author
                {
                    id = authorId1,
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "en",
                    RegionName = "US",
                    AuthorName = "Test Author 1",
                    WelcomeText = "Welcome 1!",
                    AboutText = "About author 1.",
                    HeadShotURL = "https://example.com/headshot1.jpg",
                    CopyrightText = "Copyright 1",
                    EmailAddress = "author1@example.com"
                },
                new Author
                {
                    id = authorId2,
                    TopLevelDomain = "com",
                    SecondLevelDomain = "example",
                    LanguageName = "es",
                    RegionName = "MX",
                    AuthorName = "Test Author 2",
                    WelcomeText = "Bienvenido!",
                    AboutText = "Acerca del autor 2.",
                    HeadShotURL = "https://example.com/headshot2.jpg",
                    CopyrightText = "Derechos de autor 2",
                    EmailAddress = "author2@example.com"
                }
            };

            _authorRepoMock.Setup(r => r.GetByDomainAsync("com", "example"))
                .ReturnsAsync(authors);
            
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Book>());
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Article>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            var result = await _service.GetAuthorsByDomainAsync("com", "example");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            
            var author1 = result.FirstOrDefault(a => a.id == authorId1);
            var author2 = result.FirstOrDefault(a => a.id == authorId2);
            
            Assert.NotNull(author1);
            Assert.Equal("Test Author 1", author1.AuthorName);
            Assert.Equal("en", author1.LanguageName);
            Assert.Equal("US", author1.RegionName);
            Assert.Equal("author1@example.com", author1.EmailAddress);
            Assert.Equal("com", author1.TopLevelDomain);
            Assert.Equal("example", author1.SecondLevelDomain);
            
            Assert.NotNull(author2);
            Assert.Equal("Test Author 2", author2.AuthorName);
            Assert.Equal("es", author2.LanguageName);
            Assert.Equal("MX", author2.RegionName);
            Assert.Equal("author2@example.com", author2.EmailAddress);
        }

        [Fact]
        public async Task GetAuthorsByEmailAsync_ReturnsEmptyList_WhenNoAuthorsFound()
        {
            _authorRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Author>());

            var result = await _service.GetAuthorsByEmailAsync("nobody@example.com");

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAuthorsByEmailAsync_ReturnsAuthorApiResponses_WhenAuthorsFound()
        {
            var authorId = Guid.NewGuid().ToString();
            const string email = "author@example.com";
            var author = new Author
            {
                id = authorId,
                TopLevelDomain = "com",
                SecondLevelDomain = "example",
                LanguageName = "en",
                RegionName = "US",
                AuthorName = "Email Author",
                WelcomeText = "Welcome!",
                AboutText = "About me.",
                CopyrightText = "Copyright",
                EmailAddress = email
            };
            _authorRepoMock.Setup(r => r.GetByEmailAsync(email))
                .ReturnsAsync(new List<Author> { author });
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Book>());
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<InkStainedWretch.OnePageAuthorAPI.Entities.Article>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            var result = await _service.GetAuthorsByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(authorId, result[0].id);
            Assert.Equal("Email Author", result[0].AuthorName);
            Assert.Equal(email, result[0].EmailAddress);
        }

        [Fact]
        public async Task GetAllAuthorsPagedAsync_ReturnsEmptyList_WhenNoAuthorsExist()
        {
            _authorRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Author>());

            var result = await _service.GetAllAuthorsPagedAsync(1);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAuthorsPagedAsync_ReturnsFirstPage_WhenAuthorsExist()
        {
            var authors = CreateTestAuthors(25);
            _authorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(authors);
            SetupEmptyRelatedRepos();

            var result = await _service.GetAllAuthorsPagedAsync(1);

            Assert.Equal(10, result.Count);
            Assert.Equal("Author 1", result[0].AuthorName);
            Assert.Equal("Author 10", result[9].AuthorName);
        }

        [Fact]
        public async Task GetAllAuthorsPagedAsync_ReturnsSecondPage_WhenAuthorsExist()
        {
            var authors = CreateTestAuthors(25);
            _authorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(authors);
            SetupEmptyRelatedRepos();

            var result = await _service.GetAllAuthorsPagedAsync(2);

            Assert.Equal(10, result.Count);
            Assert.Equal("Author 11", result[0].AuthorName);
            Assert.Equal("Author 20", result[9].AuthorName);
        }

        [Fact]
        public async Task GetAllAuthorsPagedAsync_ReturnsLastPartialPage_WhenBeyondFullPages()
        {
            var authors = CreateTestAuthors(25);
            _authorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(authors);
            SetupEmptyRelatedRepos();

            var result = await _service.GetAllAuthorsPagedAsync(3);

            Assert.Equal(5, result.Count);
            Assert.Equal("Author 21", result[0].AuthorName);
            Assert.Equal("Author 25", result[4].AuthorName);
        }

        [Fact]
        public async Task GetAllAuthorsPagedAsync_ReturnsEmptyList_WhenPageExceedsTotalAuthors()
        {
            var authors = CreateTestAuthors(5);
            _authorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(authors);

            var result = await _service.GetAllAuthorsPagedAsync(2);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
