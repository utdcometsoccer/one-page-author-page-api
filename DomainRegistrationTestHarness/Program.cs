using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

partial class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder()
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
                Console.WriteLine("Domain Registration Test Harness");
                Console.WriteLine("=================================");
                Console.WriteLine($"Cosmos DB Endpoint: {Utility.MaskUrl(endpointUri)}");
                Console.WriteLine($"Cosmos DB Database: {databaseId}");
                Console.WriteLine();

                // Register Cosmos via extensions and domain registration repository
                services
                    .AddCosmosClient(endpointUri, primaryKey)
                    .AddCosmosDatabase(databaseId)
                    .AddDomainRegistrationRepository();
            })
            .Build();

        // Get the repository from DI
        var repository = host.Services.GetRequiredService<IDomainRegistrationRepository>();

        // Parse command line arguments
        string? jsonFilePath = args.Length > 0 ? args[0] : null;
        string dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        if (!string.IsNullOrEmpty(jsonFilePath))
        {
            // Use specified file
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"Error: File not found: {jsonFilePath}");
                return;
            }
            await ProcessJsonFileAsync(repository, jsonFilePath);
        }
        else
        {
            // Process all files in data folder
            if (!Directory.Exists(dataRoot))
            {
                Console.WriteLine($"Data folder not found: {dataRoot}");
                Console.WriteLine("Usage: DomainRegistrationTestHarness [path-to-json-file]");
                Console.WriteLine();
                Console.WriteLine("Or place JSON files in the 'data' folder with pattern: domain-registrations-*.json");
                return;
            }

            var jsonFiles = Directory.GetFiles(dataRoot, "domain-registrations-*.json", SearchOption.TopDirectoryOnly);
            if (jsonFiles.Length == 0)
            {
                Console.WriteLine($"No domain registration JSON files found in: {dataRoot}");
                Console.WriteLine("Expected filename pattern: domain-registrations-*.json");
                return;
            }

            Console.WriteLine($"Found {jsonFiles.Length} data file(s)");
            Console.WriteLine();

            foreach (var file in jsonFiles)
            {
                await ProcessJsonFileAsync(repository, file);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Test harness completed.");
    }

    static async Task ProcessJsonFileAsync(IDomainRegistrationRepository repository, string filePath)
    {
        Console.WriteLine($"Processing: {Path.GetFileName(filePath)}");
        Console.WriteLine(new string('-', 50));

        try
        {
            string json = await File.ReadAllTextAsync(filePath);
            var registrations = JsonSerializer.Deserialize<List<DomainRegistration>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (registrations == null || registrations.Count == 0)
            {
                Console.WriteLine("  No registrations found in file.");
                return;
            }

            Console.WriteLine($"  Found {registrations.Count} registration(s)");

            foreach (var registration in registrations)
            {
                try
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(registration.Upn))
                    {
                        Console.WriteLine($"  Skipping: Missing required UPN field");
                        continue;
                    }

                    if (registration.Domain == null ||
                        string.IsNullOrWhiteSpace(registration.Domain.SecondLevelDomain) ||
                        string.IsNullOrWhiteSpace(registration.Domain.TopLevelDomain))
                    {
                        Console.WriteLine($"  Skipping: Missing domain information for UPN {registration.Upn}");
                        continue;
                    }

                    string domainName = registration.Domain.FullDomainName;

                    // Generate new ID if not provided
                    if (string.IsNullOrWhiteSpace(registration.id))
                    {
                        registration.id = Guid.NewGuid().ToString();
                    }

                    // Set timestamp
                    registration.CreatedAt = DateTime.UtcNow;

                    // Create or update in Cosmos DB
                    var existing = await repository.GetByIdAsync(registration.id, registration.Upn);
                    
                    if (existing != null)
                    {
                        // Update existing registration
                        var updated = await repository.UpdateAsync(registration);
                        Console.WriteLine($"  Updated: {domainName} (ID: {updated.id}, Status: {updated.Status})");
                    }
                    else
                    {
                        // Create new registration
                        var created = await repository.CreateAsync(registration);
                        Console.WriteLine($"  Created: {domainName} (ID: {created.id}, Status: {created.Status})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error processing registration: {ex.Message}");
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"  JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }

        Console.WriteLine();
    }
}

// Program class needed for User Secrets generic type parameter
public partial class Program { }
