using System.Text.Json;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

partial class Program
{
    static async Task Main()
    {
        using (IHost host = Host.CreateDefaultBuilder()
                                .ConfigureServices(services =>
                                {
                                    var config = new ConfigurationBuilder()
                                        .AddUserSecrets<Program>()
                                        .AddEnvironmentVariables()
                                        .Build();
                                    
                                    // Standardize configuration keys to match other applications
                                    string endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? config["EndpointUri"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
                                    string primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? config["PrimaryKey"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
                                    string databaseId = config["COSMOSDB_DATABASE_ID"] ?? config["DatabaseId"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

                                    // Log configuration (masked for security)
                                    Console.WriteLine("Starting Language Seeding...");
                                    Console.WriteLine($"Cosmos DB Endpoint configured: {Utility.MaskUrl(endpointUri)}");
                                    Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

                                    // Register Cosmos via extensions and domain services
                                    services
                                        .AddCosmosClient(endpointUri, primaryKey)
                                        .AddCosmosDatabase(databaseId)
                                        .AddLanguageRepository();
                                })
                                .Build())
        {
            // Get the Language repository from DI
            var languageRepository = host.Services.GetRequiredService<ILanguageRepository>();

            // Enumerate data folder for languages-*.json files
            string dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            if (!Directory.Exists(dataRoot))
            {
                Console.WriteLine($"Data folder not found: {dataRoot}");
                return;
            }

            var jsonFiles = Directory.GetFiles(dataRoot, "languages-*.json", SearchOption.TopDirectoryOnly);
            
            if (jsonFiles.Length == 0)
            {
                Console.WriteLine("No language data files found.");
                return;
            }

            Console.WriteLine($"Found {jsonFiles.Length} language data files");

            foreach (var file in jsonFiles)
            {
                var fileName = Path.GetFileName(file);
                Console.WriteLine($"Processing file: {fileName}");

                try
                {
                    string json = File.ReadAllText(file);
                    var languages = JsonSerializer.Deserialize<List<Language>>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (languages == null || languages.Count == 0)
                    {
                        Console.WriteLine($"No languages found in file: {fileName}");
                        continue;
                    }

                    foreach (var language in languages)
                    {
                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(language.Code) || 
                            string.IsNullOrWhiteSpace(language.Name) || 
                            string.IsNullOrWhiteSpace(language.RequestLanguage))
                        {
                            Console.WriteLine($"Skipping invalid language entry in {fileName}");
                            continue;
                        }

                        // Normalize to lowercase for consistency
                        language.Code = language.Code.ToLowerInvariant();
                        language.RequestLanguage = language.RequestLanguage.ToLowerInvariant();

                        // Check if this language already exists (idempotent)
                        var existingLanguage = await languageRepository.GetByCodeAndRequestLanguageAsync(
                            language.Code, 
                            language.RequestLanguage);

                        if (existingLanguage != null)
                        {
                            Console.WriteLine($"Skipping {language.Code} for {language.RequestLanguage} - already exists");
                            continue;
                        }

                        // Create a new GUID for the id
                        language.id = Guid.NewGuid().ToString();

                        try
                        {
                            await languageRepository.AddAsync(language);
                            Console.WriteLine($"Added language: {language.Code} ({language.Name}) for {language.RequestLanguage}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to add language {language.Code} for {language.RequestLanguage}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
                }
            }

            Console.WriteLine("Language seeding completed.");
        }
    }
}

// Program class needed for User Secrets generic type parameter
public partial class Program { }
