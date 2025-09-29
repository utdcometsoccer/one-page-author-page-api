using System.Globalization;
using System.Text.Json;
using InkStainedWretch.OnePageAuthorAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI.API;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
                                .AddUserSecrets<Program>()
                                .Build();
        // Read settings
        var configJson = File.ReadAllText(Utility.GetAbsolutePath("appsettings.json"));
        using var configDoc = JsonDocument.Parse(configJson);
        string endpointUri = config["EndpointUri"] ?? "";
        string primaryKey = config["PrimaryKey"] ?? "";
        string databaseId = config["DatabaseId"] ?? "";
        string authorDomain = configDoc.RootElement.GetProperty("AuthorDomain").GetString() ?? "";

        // Parse author domain
        var domainParts = authorDomain.Split('.');
        string secondLevelDomain = domainParts.Length > 1 ? domainParts[0] : "";
        string topLevelDomain = domainParts.Length > 1 ? domainParts[1] : "";

        // Get default culture
        var culture = CultureInfo.CurrentCulture;
        string languageName = culture.TwoLetterISOLanguageName;
        string regionName = culture.Name.Length > 3 ? culture.Name.Substring(3).ToLowerInvariant() : "";

        // Build a minimal DI container using the DI extensions
        var services = new ServiceCollection();
        services
            .AddCosmosClient(endpointUri, primaryKey)
            .AddCosmosDatabase(databaseId)
            .AddAuthorDataService();
        var provider = services.BuildServiceProvider();
        var authorDataService = provider.GetRequiredService<IAuthorDataService>();

        // Query author data service
        var response = await authorDataService.GetAuthorWithDataAsync(topLevelDomain, secondLevelDomain, languageName, regionName);
        if (response != null)
        {
            Console.WriteLine($"Name: {response.Name}");
            Console.WriteLine($"Welcome: {response.Welcome}");
            Console.WriteLine($"AboutMe: {response.AboutMe}");
            Console.WriteLine($"Headshot: {response.Headshot}");
            Console.WriteLine($"Copyright: {response.Copyright}");
            Console.WriteLine($"Email: {response.Email}");
            Console.WriteLine("Books:");
            foreach (var book in response.Books)
            {
                Console.WriteLine($"  - {book.Title}: {book.Description}");
                Console.WriteLine($"    URL: {book.Url}");
                Console.WriteLine($"    Cover: {book.Cover}");
            }
            Console.WriteLine("Articles:");
            foreach (var article in response.Articles)
            {
                Console.WriteLine($"  - {article.Title} ({article.Date:yyyy-MM-dd})");
                Console.WriteLine($"    Publication: {article.Publication}");
                Console.WriteLine($"    URL: {article.Url}");
            }
            Console.WriteLine("Socials:");
            foreach (var social in response.Social)
            {
                Console.WriteLine($"  - {social.Name}: {social.Url}");
            }
        }
        else
        {
            Console.WriteLine("No author found for the specified domain and culture.");
        }
    }
}
