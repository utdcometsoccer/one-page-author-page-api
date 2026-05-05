using InkStainedWretch.OnePageAuthorAPI.Entities;
namespace InkStainedWretch.OnePageAuthorAPI.API
{
    public class AuthorDataService : IAuthorDataService
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly IGenericRepository<Entities.Book> _bookRepository;
        private readonly IGenericRepository<Entities.Article> _articleRepository;
        private readonly IGenericRepository<Entities.Social> _socialRepository;

        public AuthorDataService(
            IAuthorRepository authorRepository,
            IGenericRepository<Entities.Book> bookRepository,
            IGenericRepository<Entities.Article> articleRepository,
            IGenericRepository<Entities.Social> socialRepository)
        {
            _authorRepository = authorRepository;
            _bookRepository = bookRepository;
            _articleRepository = articleRepository;
            _socialRepository = socialRepository;
        }

        public async Task<AuthorResponse?> GetAuthorWithDataAsync(string topLevelDomain, string secondLevelDomain, string languageName, string? regionName = null)
        {
            var author = await ResolveAuthorAsync(topLevelDomain, secondLevelDomain, languageName, regionName);
            if (author == null)
                return null;

            var books = await _bookRepository.GetByAuthorIdAsync(Guid.Parse(author.id));
            var articles = await _articleRepository.GetByAuthorIdAsync(Guid.Parse(author.id));
            var socials = await _socialRepository.GetByAuthorIdAsync(Guid.Parse(author.id));

            return new AuthorResponse
            {
                Name = author.AuthorName,
                Welcome = author.WelcomeText,
                AboutMe = author.AboutText,
                Headshot = author.HeadShotURL ?? string.Empty,
                Books = ConvertToApiBooks(books.ToList()),
                Copyright = author.CopyrightText,
                Social = socials.Select(s => new SocialLink { Name = s.Name, Url = s.URL.ToString() }).ToList(),
                Email = author.EmailAddress,
                Articles = ConvertToApiArticles(articles.ToList())
            };
        }

        public async Task<List<AuthorApiResponse>> GetAuthorsByDomainAsync(string topLevelDomain, string secondLevelDomain)
        {
            var authors = await _authorRepository.GetByDomainAsync(topLevelDomain, secondLevelDomain);
            return await BuildAuthorApiResponsesAsync(authors);
        }

        public async Task<List<AuthorApiResponse>> GetAuthorsByEmailAsync(string emailAddress)
        {
            var authors = await _authorRepository.GetByEmailAsync(emailAddress);
            return await BuildAuthorApiResponsesAsync(authors);
        }

        public async Task<List<AuthorApiResponse>> GetAllAuthorsPagedAsync(int page, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            // Delegate paging to the repository so Cosmos DB only returns the requested page.
            var pagedAuthors = await _authorRepository.GetAllPagedAsync(page, pageSize);
            return await BuildAuthorApiResponsesAsync(pagedAuthors);
        }

        public async Task<AuthorResponse?> GetHomepageDataAsync(string topLevelDomain, string secondLevelDomain, string languageName, string? regionName = null)
        {
            // Resolve author once and fetch all related data in a single pass,
            // so author and featured book are guaranteed to come from the same entity.
            var author = await ResolveAuthorAsync(topLevelDomain, secondLevelDomain, languageName, regionName);
            if (author == null)
                return null;

            var authorGuid = Guid.Parse(author.id);
            var books = await _bookRepository.GetByAuthorIdAsync(authorGuid);
            var articles = await _articleRepository.GetByAuthorIdAsync(authorGuid);
            var socials = await _socialRepository.GetByAuthorIdAsync(authorGuid);

            var response = new AuthorResponse
            {
                Name = author.AuthorName,
                Welcome = author.WelcomeText,
                AboutMe = author.AboutText,
                Headshot = author.HeadShotURL ?? string.Empty,
                Books = ConvertToApiBooks(books.ToList()),
                Copyright = author.CopyrightText,
                Social = socials.Select(s => new SocialLink { Name = s.Name, Url = s.URL.ToString() }).ToList(),
                Email = author.EmailAddress,
                Articles = ConvertToApiArticles(articles.ToList())
            };

            // Find the explicitly curated featured hero book from the same book list.
            // Order by id for deterministic selection when multiple books are mistakenly flagged.
            // No fallback or auto-selection is performed.
            var featuredBook = books?
                .Where(b => b.IsFeaturedHeroBook)
                .OrderBy(b => b.id)
                .FirstOrDefault();

            if (featuredBook != null)
                response.FeaturedBook = MapToFeaturedBookDto(featuredBook, response.Name);

            return response;
        }

        /// <summary>
        /// Resolves the best-matching <see cref="Author"/> for the given domain and locale
        /// using the standard 4-step fallback chain.
        /// </summary>
        private async Task<Entities.Author?> ResolveAuthorAsync(string topLevelDomain, string secondLevelDomain, string languageName, string? regionName)
        {
            // 1. Try full match (TLD, SLD, language, region)
            var authors = await _authorRepository.GetByDomainAndLocaleAsync(topLevelDomain, secondLevelDomain, languageName, regionName ?? "");
            authors ??= new List<Entities.Author>();
            var author = authors.FirstOrDefault();

            // 2. If not found, try match without region
            if (author == null)
            {
                authors = await _authorRepository.GetByDomainAndLocaleAsync(topLevelDomain, secondLevelDomain, languageName, "");
                authors ??= new List<Entities.Author>();
                author = authors.FirstOrDefault();
            }

            // 3. If not found, try first default author for TLD and SLD
            if (author == null)
            {
                authors = await _authorRepository.GetByDomainAndDefaultAsync(topLevelDomain, secondLevelDomain);
                authors ??= new List<Entities.Author>();
                author = authors.FirstOrDefault(a => a.IsDefault);
            }

            // 4. If not found, try first author for TLD and SLD
            if (author == null)
            {
                authors = await _authorRepository.GetByDomainAsync(topLevelDomain, secondLevelDomain);
                authors ??= new List<Entities.Author>();
                author = authors.FirstOrDefault();
            }

            return author;
        }

        private static FeaturedBookDto MapToFeaturedBookDto(Entities.Book book, string authorName)
        {
            return new FeaturedBookDto
            {
                Title = book.Title,
                Subtitle = book.Subtitle,
                AuthorName = authorName,
                Description = book.Description,
                CoverImageUrl = book.Cover?.ToString() ?? string.Empty,
                CoverImageAlt = book.CoverImageAlt ?? book.Title,
                PrimaryCtaLabel = book.PrimaryCtaLabel ?? string.Empty,
                PrimaryCtaUrl = book.URL?.ToString() ?? string.Empty,
                SecondaryCtaLabel = book.SecondaryCtaLabel,
                SecondaryCtaUrl = book.SecondaryCtaUrl,
                Formats = book.Formats != null ? (IReadOnlyList<string>)book.Formats.AsReadOnly() : Array.Empty<string>()
            };
        }

        private async Task<List<AuthorApiResponse>> BuildAuthorApiResponsesAsync(IList<Entities.Author> authors)
        {
            if (authors == null || !authors.Any())
                return new List<AuthorApiResponse>();

            var authorApiResponses = new List<AuthorApiResponse>();

            foreach (var author in authors)
            {
                var books = await _bookRepository.GetByAuthorIdAsync(Guid.Parse(author.id));
                var articles = await _articleRepository.GetByAuthorIdAsync(Guid.Parse(author.id));
                var socials = await _socialRepository.GetByAuthorIdAsync(Guid.Parse(author.id));

                authorApiResponses.Add(new AuthorApiResponse
                {
                    id = author.id,
                    AuthorName = author.AuthorName,
                    LanguageName = author.LanguageName,
                    RegionName = author.RegionName,
                    EmailAddress = author.EmailAddress,
                    WelcomeText = author.WelcomeText,
                    AboutText = author.AboutText,
                    HeadShotURL = author.HeadShotURL ?? string.Empty,
                    CopyrightText = author.CopyrightText,
                    TopLevelDomain = author.TopLevelDomain,
                    SecondLevelDomain = author.SecondLevelDomain,
                    Articles = ConvertToApiArticles(articles.ToList()),
                    Books = ConvertToApiBooks(books.ToList()),
                    Socials = socials.Select(s => new SocialLink { Name = s.Name, Url = s.URL.ToString() }).ToList()
                });
            }

            return authorApiResponses;
        }

        public static List<API.Book> ConvertToApiBooks(List<Entities.Book> entityBooks)
        {
            var apiBooks = new List<API.Book>();
            foreach (var entity in entityBooks)
            {
                apiBooks.Add(new API.Book
                {
                    Title = entity.Title,
                    Description = entity.Description,
                    Url = entity.URL?.ToString() ?? string.Empty,
                    Cover = entity.Cover?.ToString() ?? string.Empty
                });
            }
            return apiBooks;
        }

        public static List<API.Article> ConvertToApiArticles(List<Entities.Article> entityArticles)
        {
            var apiArticles = new List<API.Article>();
            foreach (var entity in entityArticles)
            {
                apiArticles.Add(new API.Article
                {
                    Title = entity.Title,
                    Date = entity.Date.ToString("yyyy-MM-dd"),
                    Publication = entity.Publication,
                    Url = entity.URL?.ToString() ?? string.Empty
                });
            }
            return apiArticles;
        }
    }
}
