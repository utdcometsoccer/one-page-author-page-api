using Moq;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using EntityBook = InkStainedWretch.OnePageAuthorAPI.Entities.Book;
using EntityArticle = InkStainedWretch.OnePageAuthorAPI.Entities.Article;

namespace OnePageAuthor.Test
{
    /// <summary>
    /// Tests for <see cref="AuthorDataService.GetHomepageDataAsync"/> covering the
    /// featured-book hero experiment acceptance criteria.
    /// </summary>
    public class HomepageDataServiceTests
    {
        private readonly Mock<IAuthorRepository> _authorRepoMock;
        private readonly Mock<IGenericRepository<EntityBook>> _bookRepoMock;
        private readonly Mock<IGenericRepository<EntityArticle>> _articleRepoMock;
        private readonly Mock<IGenericRepository<Social>> _socialRepoMock;
        private readonly AuthorDataService _service;

        private static readonly Author TestAuthor = new Author
        {
            id = "11111111-1111-1111-1111-111111111111",
            TopLevelDomain = "com",
            SecondLevelDomain = "example",
            LanguageName = "en",
            RegionName = "US",
            AuthorName = "Jane Author",
            WelcomeText = "Welcome!",
            AboutText = "About me.",
            HeadShotURL = "https://example.com/headshot.jpg",
            CopyrightText = "© 2025",
            EmailAddress = "jane@example.com"
        };

        public HomepageDataServiceTests()
        {
            _authorRepoMock = new Mock<IAuthorRepository>();
            _bookRepoMock = new Mock<IGenericRepository<EntityBook>>();
            _articleRepoMock = new Mock<IGenericRepository<EntityArticle>>();
            _socialRepoMock = new Mock<IGenericRepository<Social>>();
            _service = new AuthorDataService(
                _authorRepoMock.Object,
                _bookRepoMock.Object,
                _articleRepoMock.Object,
                _socialRepoMock.Object);
        }

        // -------------------------------------------------------------------------
        // Helper setup methods
        // -------------------------------------------------------------------------

        private void SetupAuthorFound()
        {
            _authorRepoMock
                .Setup(r => r.GetByDomainAndLocaleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author> { TestAuthor });
        }

        private void SetupAuthorNotFound()
        {
            _authorRepoMock
                .Setup(r => r.GetByDomainAndLocaleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author>());
            _authorRepoMock
                .Setup(r => r.GetByDomainAndDefaultAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author>());
            _authorRepoMock
                .Setup(r => r.GetByDomainAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<Author>());
        }

        private void SetupEmptyRelatedRepos()
        {
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook>());
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());
        }

        private static EntityBook CreateRegularBook(string title = "Regular Book") =>
            new EntityBook
            {
                id = Guid.NewGuid().ToString(),
                AuthorID = TestAuthor.id,
                Title = title,
                Description = "A fine book.",
                URL = new Uri("https://buy.example.com/book"),
                Cover = new Uri("https://covers.example.com/book.jpg"),
                IsFeaturedHeroBook = false
            };

        private static EntityBook CreateFeaturedBook() =>
            new EntityBook
            {
                id = Guid.NewGuid().ToString(),
                AuthorID = TestAuthor.id,
                Title = "The Featured Novel",
                Subtitle = "A Story Worth Telling",
                Description = "An amazing novel.",
                URL = new Uri("https://buy.example.com/featured"),
                Cover = new Uri("https://covers.example.com/featured.jpg"),
                IsFeaturedHeroBook = true,
                CoverImageAlt = "Cover of The Featured Novel",
                PrimaryCtaLabel = "Buy Now",
                SecondaryCtaLabel = "Learn More",
                SecondaryCtaUrl = "https://example.com/learn",
                Formats = new List<string> { "Hardcover", "eBook" }
            };

        // -------------------------------------------------------------------------
        // Test: No featured book configured
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_NoFeaturedBook_FeaturedBookIsNull()
        {
            // Arrange
            SetupAuthorFound();
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook> { CreateRegularBook() });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.FeaturedBook);
        }

        [Fact]
        public async Task GetHomepageDataAsync_NoFeaturedBook_ExistingBooksStillReturned()
        {
            // Arrange
            SetupAuthorFound();
            var books = new List<EntityBook> { CreateRegularBook("Book A"), CreateRegularBook("Book B") };
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(books);
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Books.Count);
            Assert.Null(result.FeaturedBook);
        }

        [Fact]
        public async Task GetHomepageDataAsync_NoBooksAtAll_FeaturedBookIsNull()
        {
            // Arrange
            SetupAuthorFound();
            SetupEmptyRelatedRepos();

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.FeaturedBook);
        }

        // -------------------------------------------------------------------------
        // Test: Featured book configured
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_FeaturedBookConfigured_FeaturedBookPopulated()
        {
            // Arrange
            SetupAuthorFound();
            var featured = CreateFeaturedBook();
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook> { CreateRegularBook(), featured });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FeaturedBook);
            Assert.Equal("The Featured Novel", result.FeaturedBook.Title);
            Assert.Equal("A Story Worth Telling", result.FeaturedBook.Subtitle);
            Assert.Equal("Jane Author", result.FeaturedBook.AuthorName);
            Assert.Equal("An amazing novel.", result.FeaturedBook.Description);
            Assert.Equal("https://covers.example.com/featured.jpg", result.FeaturedBook.CoverImageUrl);
            Assert.Equal("Cover of The Featured Novel", result.FeaturedBook.CoverImageAlt);
            Assert.Equal("Buy Now", result.FeaturedBook.PrimaryCtaLabel);
            Assert.Equal("https://buy.example.com/featured", result.FeaturedBook.PrimaryCtaUrl);
            Assert.Equal("Learn More", result.FeaturedBook.SecondaryCtaLabel);
            Assert.Equal("https://example.com/learn", result.FeaturedBook.SecondaryCtaUrl);
            Assert.Equal(new[] { "Hardcover", "eBook" }, result.FeaturedBook.Formats);
        }

        [Fact]
        public async Task GetHomepageDataAsync_FeaturedBookConfigured_FullBooksListStillReturned()
        {
            // Arrange
            SetupAuthorFound();
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook> { CreateRegularBook("Book A"), CreateRegularBook("Book B"), CreateFeaturedBook() });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Books.Count);
            Assert.NotNull(result.FeaturedBook);
        }

        [Fact]
        public async Task GetHomepageDataAsync_OnlyFeaturedBookPresent_FeaturedBookPopulated()
        {
            // Arrange
            SetupAuthorFound();
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook> { CreateFeaturedBook() });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FeaturedBook);
            Assert.Equal("The Featured Novel", result.FeaturedBook.Title);
        }

        // -------------------------------------------------------------------------
        // Test: CoverImageAlt fallback to Title
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_FeaturedBookWithoutAlt_CoverImageAltFallsBackToTitle()
        {
            // Arrange
            SetupAuthorFound();
            var featured = new EntityBook
            {
                id = Guid.NewGuid().ToString(),
                AuthorID = TestAuthor.id,
                Title = "My Great Book",
                Description = "Desc",
                URL = new Uri("https://example.com"),
                Cover = new Uri("https://example.com/cover.jpg"),
                IsFeaturedHeroBook = true,
                CoverImageAlt = null  // not explicitly set
            };
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook> { featured });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result?.FeaturedBook);
            Assert.Equal("My Great Book", result.FeaturedBook.CoverImageAlt);
        }

        // -------------------------------------------------------------------------
        // Test: Author not found
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_AuthorNotFound_ReturnsNull()
        {
            // Arrange
            SetupAuthorNotFound();

            // Act
            var result = await _service.GetHomepageDataAsync("com", "unknown", "en");

            // Assert
            Assert.Null(result);
        }

        // -------------------------------------------------------------------------
        // Test: Misconfigured featured book (no valid books)
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_MultipleNonFeaturedBooks_NoFeaturedBookReturned()
        {
            // Arrange — all books have IsFeaturedHeroBook = false; none qualifies
            SetupAuthorFound();
            _bookRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityBook>
                {
                    CreateRegularBook("Book 1"),
                    CreateRegularBook("Book 2"),
                    CreateRegularBook("Book 3")
                });
            _articleRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<EntityArticle>());
            _socialRepoMock.Setup(r => r.GetByAuthorIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Social>());

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.FeaturedBook);
        }

        // -------------------------------------------------------------------------
        // Test: Backward compatibility — existing fields intact
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_AuthorData_ExistingFieldsPreserved()
        {
            // Arrange
            SetupAuthorFound();
            SetupEmptyRelatedRepos();

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert — all existing AuthorResponse fields are still correct
            Assert.NotNull(result);
            Assert.Equal("Jane Author", result.Name);
            Assert.Equal("Welcome!", result.Welcome);
            Assert.Equal("About me.", result.AboutMe);
            Assert.Equal("https://example.com/headshot.jpg", result.Headshot);
            Assert.Equal("© 2025", result.Copyright);
            Assert.Equal("jane@example.com", result.Email);
        }

        // -------------------------------------------------------------------------
        // Test: Experiment field starts null (set by caller, not by service)
        // -------------------------------------------------------------------------

        [Fact]
        public async Task GetHomepageDataAsync_ExperimentFieldIsNull_ServiceDoesNotSetIt()
        {
            // Arrange — experiment assignment is the function's responsibility
            SetupAuthorFound();
            SetupEmptyRelatedRepos();

            // Act
            var result = await _service.GetHomepageDataAsync("com", "example", "en");

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Experiment);
        }
    }
}
