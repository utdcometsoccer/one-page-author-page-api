using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

partial class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Google Cloud Platform Domain Registration Test");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var config = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .AddEnvironmentVariables()
                    .Build();

                // Google Cloud Platform Configuration
                string projectId = config["GOOGLE_CLOUD_PROJECT_ID"] ?? throw new InvalidOperationException("GOOGLE_CLOUD_PROJECT_ID is required");
                string location = config["GOOGLE_DOMAINS_LOCATION"] ?? "us-central1";
                
                // Cosmos DB Configuration (for reading test data)
                string endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
                string primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
                string databaseId = config["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

                // Log configuration (masked for security)
                Console.WriteLine($"Google Cloud Project ID: {projectId}");
                Console.WriteLine($"Google Domains Location: {location}");
                Console.WriteLine($"Cosmos DB Endpoint: {Utility.MaskUrl(endpointUri)}");
                Console.WriteLine($"Cosmos DB Database: {databaseId}");
                Console.WriteLine();

                // Register services
                services
                    .AddCosmosClient(endpointUri, primaryKey)
                    .AddCosmosDatabase(databaseId)
                    .AddDomainRegistrationRepository()
                    .AddGoogleDomainsService();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Get services
        var googleDomainsService = host.Services.GetRequiredService<IGoogleDomainsService>();
        var domainRepository = host.Services.GetRequiredService<IDomainRegistrationRepository>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
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
                await RunTestsAsync(googleDomainsService, domainRepository, logger, jsonFilePath);
            }
            else
            {
                // Use default test file
                string defaultFile = Path.Combine(dataRoot, "test-domains.json");
                if (!File.Exists(defaultFile))
                {
                    Console.WriteLine($"Error: Default test file not found: {defaultFile}");
                    Console.WriteLine("Usage: GoogleDomainRegistrationTest [path-to-json-file]");
                    return;
                }
                await RunTestsAsync(googleDomainsService, domainRepository, logger, defaultFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test execution failed");
            Console.WriteLine($"\nTest execution failed: {ex.Message}");
        }
    }

    static async Task RunTestsAsync(
        IGoogleDomainsService googleDomainsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string jsonFilePath)
    {
        Console.WriteLine($"Processing test file: {Path.GetFileName(jsonFilePath)}");
        Console.WriteLine(new string('-', 60));

        try
        {
            string json = await File.ReadAllTextAsync(jsonFilePath);
            var registrations = JsonSerializer.Deserialize<List<DomainRegistration>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (registrations == null || registrations.Count == 0)
            {
                Console.WriteLine("No test registrations found in file.");
                return;
            }

            Console.WriteLine($"Loaded {registrations.Count} test registration(s)\n");

            int testNumber = 1;
            foreach (var registration in registrations)
            {
                await RunSingleTestAsync(googleDomainsService, domainRepository, logger, registration, testNumber++);
            }

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Test Summary");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Total Tests: {registrations.Count}");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON parsing error");
            Console.WriteLine($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing test file");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task RunSingleTestAsync(
        IGoogleDomainsService googleDomainsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        DomainRegistration registration,
        int testNumber)
    {
        Console.WriteLine($"\nTest {testNumber}: {registration.Domain?.FullDomainName ?? "Unknown"}");
        Console.WriteLine(new string('-', 60));

        try
        {
            // Validate required fields
            if (registration.Domain == null ||
                string.IsNullOrWhiteSpace(registration.Domain.SecondLevelDomain) ||
                string.IsNullOrWhiteSpace(registration.Domain.TopLevelDomain))
            {
                Console.WriteLine("  ❌ FAILED: Missing domain information");
                return;
            }

            string domainName = registration.Domain.FullDomainName;

            // Test 1: Check domain availability
            Console.WriteLine($"  Step 1: Checking availability for {domainName}...");
            bool isAvailable = await googleDomainsService.IsDomainAvailableAsync(domainName);
            Console.WriteLine($"  Result: {(isAvailable ? "✓ Available" : "✗ Not Available")}");

            if (!isAvailable)
            {
                Console.WriteLine($"  ⚠️  SKIPPED: Domain {domainName} is not available for registration");
                return;
            }

            // Test 2: Attempt registration (DRY RUN)
            Console.WriteLine($"  Step 2: Testing registration parameters for {domainName}...");
            
            // Validate contact information
            if (registration.ContactInformation == null)
            {
                Console.WriteLine("  ❌ FAILED: Missing contact information");
                return;
            }

            Console.WriteLine($"  Registrant: {registration.ContactInformation.FirstName} {registration.ContactInformation.LastName}");
            Console.WriteLine($"  Email: {registration.ContactInformation.EmailAddress}");
            Console.WriteLine($"  Location: {registration.ContactInformation.City}, {registration.ContactInformation.State}");

            // Note: We won't actually call RegisterDomainAsync as it would create real registrations
            // Instead, we verify the data structure and report readiness
            Console.WriteLine($"  ✓ Registration parameters validated");
            Console.WriteLine($"  ⚠️  NOTE: Actual registration not performed (dry run mode)");

            // Test 3: Verify database record could be created
            Console.WriteLine($"  Step 3: Verifying database compatibility...");
            
            // Check if record already exists in DB
            var existing = await domainRepository.GetByDomainAsync(
                registration.Domain.TopLevelDomain, 
                registration.Domain.SecondLevelDomain);
            if (existing != null)
            {
                Console.WriteLine($"  ℹ️  Domain already exists in database (ID: {existing.id})");
            }
            else
            {
                Console.WriteLine($"  ✓ Domain is new and ready for database insertion");
            }

            Console.WriteLine($"  ✅ TEST PASSED: {domainName} is ready for Google Cloud registration");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test failed for domain {DomainName}", registration.Domain?.FullDomainName);
            Console.WriteLine($"  ❌ TEST FAILED: {ex.Message}");
        }
    }
}

// Program class needed for User Secrets generic type parameter
public partial class Program { }
