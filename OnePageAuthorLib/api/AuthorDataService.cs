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
            var authors = await _authorRepository.GetByDomainAndLocaleAsync(topLevelDomain, secondLevelDomain, languageName, regionName ?? "");
            var author = authors.FirstOrDefault();
            if (author == null)
                return null;

            var books = await _bookRepository.GetByAuthorIdAsync(Guid.Parse(author.id));
            var articles = await _articleRepository.GetByAuthorIdAsync(Guid.Parse(author.id));
            var socials = await _socialRepository.GetByAuthorIdAsync(Guid.Parse(author.id));

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
            return response;
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
