using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.API;
using Book = InkStainedWretch.OnePageAuthorAPI.Entities.Book;
using Article = InkStainedWretch.OnePageAuthorAPI.Entities.Article;
using Microsoft.Extensions.Configuration;

IConfiguration config = new ConfigurationBuilder()
.AddUserSecrets<Program>()
.Build();

// Read Cosmos DB settings from appsettings.json

string endpointUri = config["EndpointUri"] ?? throw new InvalidOperationException("EndpointUri is not set.");
string primaryKey = config["PrimaryKey"] ?? throw new InvalidOperationException("PrimaryKey is not set.");
string databaseId = config["DatabaseId"] ?? throw new InvalidOperationException("DatabaseId is not set.");

// Build a DI container using new extensions and resolve repositories
var services = new ServiceCollection();
services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddAuthorRepositories();

var provider = services.BuildServiceProvider();
var authorRepository = provider.GetRequiredService<InkStainedWretch.OnePageAuthorAPI.NoSQL.AuthorRepository>();
var bookRepository = provider.GetRequiredService<InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<Book>>();
var articleRepository = provider.GetRequiredService<InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<Article>>();
var socialRepository = provider.GetRequiredService<InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<Social>>();

// Use the source data folder relative to the project directory
string dataRoot = Utility.GetDataRoot();
if (!Directory.Exists(dataRoot))
{
    Console.WriteLine($"Data folder not found: {dataRoot}");
    return;
}

var jsonFiles = Directory.GetFiles(dataRoot, "author-data-*.json", SearchOption.AllDirectories);
var authors = new List<Author>();
var books = new List<Book>();
var articles = new List<Article>();
var socials = new List<Social>();

foreach (var file in jsonFiles)
{
    try
    {
        string json = File.ReadAllText(file);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Extract folder and filename info
        var dirParts = file.Replace(dataRoot, "").Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
        string topLevelDomain = dirParts.Length > 0 ? dirParts[0] : "";
        string secondLevelDomain = dirParts.Length > 1 ? dirParts[1] : "";
        string fileName = Path.GetFileNameWithoutExtension(file);
        string[] nameParts = fileName.Replace("author-data-", "").Split('-');
        string languageName = nameParts.Length > 0 ? nameParts[0] : "";
        string regionName = nameParts.Length > 1 ? nameParts[1] : "";

        // Map JSON fields to Author properties
        var author = new Author
        {
            id = Guid.NewGuid().ToString(),
            TopLevelDomain = topLevelDomain,
            SecondLevelDomain = secondLevelDomain,
            LanguageName = languageName,
            RegionName = regionName,
            AuthorName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
            WelcomeText = root.TryGetProperty("welcome", out var welcomeProp) ? welcomeProp.GetString() ?? "" : "",
            AboutText = root.TryGetProperty("aboutMe", out var aboutProp) ? aboutProp.GetString() ?? "" : "",
            HeadShotURL = root.TryGetProperty("headshot", out var headshotProp) ? headshotProp.GetString() : null,
            CopyrightText = root.TryGetProperty("copyright", out var copyrightProp) ? copyrightProp.GetString() ?? "" : "",
            EmailAddress = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : ""
        };
        authors.Add(author);

        // Extract books
        if (root.TryGetProperty("books", out var booksProp) && booksProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var bookElem in booksProp.EnumerateArray())
            {
                var book = new Book
                {
                    id = Guid.NewGuid().ToString(),
                    AuthorID = author.id,
                    Title = bookElem.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "",
                    Description = bookElem.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "",
                    URL = bookElem.TryGetProperty("url", out var urlProp) && Uri.TryCreate(urlProp.GetString(), UriKind.Absolute, out var urlVal) ? urlVal : new Uri("https://example.com"),
                    Cover = bookElem.TryGetProperty("cover", out var coverProp) && Uri.TryCreate(coverProp.GetString(), UriKind.RelativeOrAbsolute, out var coverVal) ? coverVal : new Uri("https://example.com/cover.jpg")
                };
                books.Add(book);
            }
        }
        // Extract articles
        if (root.TryGetProperty("articles", out var articlesProp) && articlesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var articleElem in articlesProp.EnumerateArray())
            {
                var article = new Article
                {
                    id = Guid.NewGuid().ToString(),
                    AuthorID = author.id,
                    Title = articleElem.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "",
                    Date = articleElem.TryGetProperty("date", out var dateProp) && DateTime.TryParse(dateProp.GetString(), out var dateVal) ? dateVal : DateTime.MinValue,
                    Publication = articleElem.TryGetProperty("publication", out var pubProp) ? pubProp.GetString() ?? "" : "",
                    URL = articleElem.TryGetProperty("url", out var urlProp) && Uri.TryCreate(urlProp.GetString(), UriKind.Absolute, out var urlVal) ? urlVal : new Uri("https://example.com")
                };
                articles.Add(article);
            }
        }

        // Extract socials
        if (root.TryGetProperty("social", out var socialProp) && socialProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var socialElem in socialProp.EnumerateArray())
            {
                var social = new Social
                {
                    id = Guid.NewGuid().ToString(),
                    AuthorID = author.id,
                    Name = socialElem.TryGetProperty("name", out var nameProp2) ? nameProp2.GetString() ?? "" : "",
                    URL = socialElem.TryGetProperty("url", out var urlProp2) && Uri.TryCreate(urlProp2.GetString(), UriKind.Absolute, out var urlVal2) ? urlVal2 : new Uri("https://example.com")
                };
                socials.Add(social);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing file {file}: {ex.Message}");
    }


    // Save all parsed data to Cosmos DB
    await SaveAuthorData(
        authorRepository ?? throw new InvalidOperationException("AuthorRepository is null."),
        bookRepository ?? throw new InvalidOperationException("BookRepository is null."),
        articleRepository ?? throw new InvalidOperationException("ArticleRepository is null."),
        socialRepository ?? throw new InvalidOperationException("SocialRepository is null."),
        authors,
        books,
        articles,
        socials);

    // Print all data after processing
    foreach (var author in authors)
    {
        WriteAuthorData(books, articles, socials, author);
    }
}

async Task SaveAuthorData(InkStainedWretch.OnePageAuthorAPI.NoSQL.AuthorRepository authorRepository, InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<Book> bookRepository, InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<Article> articleRepository, InkStainedWretch.OnePageAuthorAPI.NoSQL.GenericRepository<Social> socialRepository, List<Author> authors, List<Book> books, List<Article> articles, List<Social> socials)
{
    foreach (var author in authors)
    {
        await authorRepository.AddAsync(author);
    }
    foreach (var book in books)
    {
        await bookRepository.AddAsync(book);
    }
    foreach (var article in articles)
    {
        await articleRepository.AddAsync(article);
    }
    foreach (var social in socials)
    {
        await socialRepository.AddAsync(social);
    }
}

void WriteAuthorData(List<Book> books, List<Article> articles, List<Social> socials, Author author)
{
    Console.WriteLine($"Author: {author.AuthorName} (id: {author.id})");
    Console.WriteLine($"TopLevelDomain: {author.TopLevelDomain}, SecondLevelDomain: {author.SecondLevelDomain}, Language: {author.LanguageName}, Region: {author.RegionName}");
    Console.WriteLine($"WelcomeText: {author.WelcomeText}");
    Console.WriteLine($"AboutText: {author.AboutText}");
    Console.WriteLine($"HeadShotURL: {author.HeadShotURL}");
    Console.WriteLine($"CopyrightText: {author.CopyrightText}");
    Console.WriteLine($"EmailAddress: {author.EmailAddress}");

    var authorBooks = books.Where(b => b.AuthorID == author.id).ToList();
    if (authorBooks.Count > 0)
    {
        Console.WriteLine("Books:");
        foreach (var book in authorBooks)
        {
            Console.WriteLine($"  - {book.Title}: {book.Description}");
            Console.WriteLine($"    URL: {book.URL}");
            Console.WriteLine($"    Cover: {book.Cover}");
        }
    }

    var authorArticles = articles.Where(a => a.AuthorID == author.id).ToList();
    if (authorArticles.Count > 0)
    {
        Console.WriteLine("Articles:");
        foreach (var article in authorArticles)
        {
            Console.WriteLine($"  - {article.Title} ({article.Date:yyyy-MM-dd})");
            Console.WriteLine($"    Publication: {article.Publication}");
            Console.WriteLine($"    URL: {article.URL}");
        }
    }

    var authorSocials = socials.Where(s => s.AuthorID == author.id).ToList();
    if (authorSocials.Count > 0)
    {
        Console.WriteLine("Socials:");
        foreach (var social in authorSocials)
        {
            Console.WriteLine($"  - {social.Name}: {social.URL}");
        }
    }

    Console.WriteLine(new string('=', 60));
}
