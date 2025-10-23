using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

Console.WriteLine("=== Country Data Seeder ===");
Console.WriteLine("Starting Country data seeding process...");

// Build configuration from user secrets and environment variables
IConfiguration config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

// Read Cosmos DB settings
string endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? config["EndpointUri"] 
    ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
string primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? config["PrimaryKey"] 
    ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
string databaseId = config["COSMOSDB_DATABASE_ID"] ?? config["DatabaseId"] 
    ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

// Log configuration (masked for security)
Console.WriteLine($"Cosmos DB Endpoint: {Utility.MaskUrl(endpointUri)}");
Console.WriteLine($"Cosmos DB Database: {databaseId}");
Console.WriteLine();

// Build DI container
var services = new ServiceCollection();
services
    .AddCosmosClient(endpointUri, primaryKey)
    .AddCosmosDatabase(databaseId)
    .AddCountryRepository()
    .AddCountryServices()
    .AddLogging(builder => builder.AddConsole());

var provider = services.BuildServiceProvider();
var countryService = provider.GetRequiredService<ICountryService>();

// Get data directory
string dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
if (!Directory.Exists(dataRoot))
{
    Console.WriteLine($"ERROR: Data folder not found: {dataRoot}");
    return 1;
}

// Find all country JSON files
var jsonFiles = Directory.GetFiles(dataRoot, "countries-*.json", SearchOption.TopDirectoryOnly);
if (jsonFiles.Length == 0)
{
    Console.WriteLine($"ERROR: No country data files found in: {dataRoot}");
    return 1;
}

Console.WriteLine($"Found {jsonFiles.Length} country data file(s) to process");
Console.WriteLine();

int totalProcessed = 0;
int totalCreated = 0;
int totalSkipped = 0;
int totalErrors = 0;

foreach (var file in jsonFiles)
{
    var fileName = Path.GetFileName(file);
    Console.WriteLine($"Processing file: {fileName}");
    
    // Extract language from filename (e.g., "countries-en.json" -> "en")
    var languageMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"countries-(.+)\.json");
    if (!languageMatch.Success)
    {
        Console.WriteLine($"  WARNING: Could not extract language from filename: {fileName}");
        continue;
    }
    
    string language = languageMatch.Groups[1].Value.ToLowerInvariant();
    Console.WriteLine($"  Language: {language}");
    
    try
    {
        // Read and parse JSON file
        string json = File.ReadAllText(file);
        var countries = JsonSerializer.Deserialize<List<CountryData>>(json);
        
        if (countries == null || countries.Count == 0)
        {
            Console.WriteLine($"  WARNING: No countries found in file");
            continue;
        }
        
        Console.WriteLine($"  Found {countries.Count} countries in file");
        
        // Process each country (idempotent - check if exists before creating)
        foreach (var countryData in countries)
        {
            try
            {
                // Check if country already exists
                var existing = await countryService.GetCountryByCodeAndLanguageAsync(countryData.Code, language);
                
                if (existing != null)
                {
                    // Country already exists - skip
                    totalSkipped++;
                }
                else
                {
                    // Create new country
                    var country = new Country
                    {
                        Code = countryData.Code.ToUpperInvariant(),
                        Name = countryData.Name,
                        Language = language
                    };
                    
                    await countryService.CreateCountryAsync(country);
                    totalCreated++;
                    Console.WriteLine($"    ✓ Created: {country.Code} - {country.Name}");
                }
                
                totalProcessed++;
            }
            catch (Exception ex)
            {
                totalErrors++;
                Console.WriteLine($"    ✗ Error processing {countryData.Code}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"  Completed processing {fileName}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ERROR: Failed to process file {fileName}: {ex.Message}");
        Console.WriteLine();
        totalErrors++;
    }
}

// Print summary
Console.WriteLine("=== Seeding Summary ===");
Console.WriteLine($"Total countries processed: {totalProcessed}");
Console.WriteLine($"Created: {totalCreated}");
Console.WriteLine($"Skipped (already exist): {totalSkipped}");
Console.WriteLine($"Errors: {totalErrors}");
Console.WriteLine();

if (totalErrors > 0)
{
    Console.WriteLine("Seeding completed with errors.");
    return 1;
}
else
{
    Console.WriteLine("Seeding completed successfully!");
    return 0;
}

// Helper class for JSON deserialization
public class CountryData
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// Program class for user secrets
public partial class Program { }
