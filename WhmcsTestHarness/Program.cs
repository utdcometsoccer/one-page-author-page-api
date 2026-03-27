using Newtonsoft.Json;
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
        Console.WriteLine("WHMCS Domain Registration Test Harness");
        Console.WriteLine("======================================");
        Console.WriteLine();

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Keep config sources consistent between this harness and services
                // resolved via DI (e.g., WhmcsService).
                config.AddUserSecrets<Program>(optional: true);

                // Explicitly add env vars (CreateDefaultBuilder also adds this,
                // but keeping it here makes intent clear and prevents surprises
                // if defaults change).
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;

                // WHMCS Configuration
                string apiUrl = config["WHMCS_API_URL"] ?? throw new InvalidOperationException("WHMCS_API_URL is required");
                string apiIdentifier = config["WHMCS_API_IDENTIFIER"] ?? throw new InvalidOperationException("WHMCS_API_IDENTIFIER is required");
                string apiSecret = config["WHMCS_API_SECRET"] ?? throw new InvalidOperationException("WHMCS_API_SECRET is required");

                // Cosmos DB Configuration (for reading test data)
                string endpointUri = config["COSMOSDB_ENDPOINT_URI"] ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required");
                string primaryKey = config["COSMOSDB_PRIMARY_KEY"] ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required");
                string databaseId = config["COSMOSDB_DATABASE_ID"] ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

                // Log configuration (masked for security)
                Console.WriteLine($"WHMCS API URL: {Utility.MaskUrl(apiUrl)}");
                Console.WriteLine($"WHMCS API Identifier: {Utility.MaskSensitiveValue(apiIdentifier)}");
                Console.WriteLine($"Cosmos DB Endpoint: {Utility.MaskUrl(endpointUri)}");
                Console.WriteLine($"Cosmos DB Database: {databaseId}");
                Console.WriteLine();

                // Register services
                services
                    .AddCosmosClient(endpointUri, primaryKey)
                    .AddCosmosDatabase(databaseId)
                    .AddDomainRegistrationRepository()
                    .AddWhmcsService();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Get services
        var whmcsService = host.Services.GetRequiredService<IWhmcsService>();
        var domainRepository = host.Services.GetRequiredService<IDomainRegistrationRepository>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        string? clientId = configuration["WHMCS_CLIENT_ID"];

        if (!string.IsNullOrWhiteSpace(clientId))
            Console.WriteLine($"Using WHMCS client ID: {clientId}");
        else
            Console.WriteLine("WHMCS_CLIENT_ID not configured; AddOrder will be called without a clientId.");
        Console.WriteLine();

        try
        {
            // Parse command line arguments
            if (args.Length > 0)
            {
                if (args[0] == "--interactive" || args[0] == "-i")
                {
                    // Interactive mode: select from database
                    await RunInteractiveTestsAsync(whmcsService, domainRepository, logger, clientId);
                }
                else if (args[0] == "--upn" && args.Length > 1)
                {
                    // Filter by UPN from database
                    await RunTestsFromDatabaseAsync(whmcsService, domainRepository, logger, args[1], clientId);
                }
                else if (args[0] == "--domain" && args.Length > 2)
                {
                    // Specify TLD and SLD from command line
                    await RunSingleDomainTestAsync(whmcsService, domainRepository, logger, args[1], args[2], clientId);
                }
                else if (File.Exists(args[0]))
                {
                    // Use specified JSON file
                    await RunTestsFromFileAsync(whmcsService, domainRepository, logger, args[0], clientId);
                }
                else
                {
                    Console.WriteLine($"Error: Invalid argument or file not found: {args[0]}");
                    PrintUsage();
                    return;
                }
            }
            else
            {
                // Default: use test file or interactive
                string dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
                string defaultFile = Path.Combine(dataRoot, "test-domains.json");
                if (File.Exists(defaultFile))
                {
                    await RunTestsFromFileAsync(whmcsService, domainRepository, logger, defaultFile, clientId);
                }
                else
                {
                    Console.WriteLine("No default test file found. Use --interactive mode or specify options.");
                    PrintUsage();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test execution failed");
            Console.WriteLine($"\nTest execution failed: {ex.Message}");
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  WhmcsTestHarness                           - Use default test file");
        Console.WriteLine("  WhmcsTestHarness <json-file>               - Use specified JSON file");
        Console.WriteLine("  WhmcsTestHarness --interactive             - Select domain from database");
        Console.WriteLine("  WhmcsTestHarness --upn <email>             - Test all domains for a user");
        Console.WriteLine("  WhmcsTestHarness --domain <tld> <sld>      - Test specific domain");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  WhmcsTestHarness --interactive");
        Console.WriteLine("  WhmcsTestHarness --upn testuser@example.com");
        Console.WriteLine("  WhmcsTestHarness --domain com example");
    }

    static async Task RunInteractiveTestsAsync(
        IWhmcsService whmcsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string? clientId)
    {
        Console.WriteLine("Interactive Mode: Select domain registrations from database");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine();

        Console.Write("Enter User Principal Name (UPN/email) to search: ");
        string? upn = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(upn))
        {
            Console.WriteLine("Error: UPN is required.");
            return;
        }

        await RunTestsFromDatabaseAsync(whmcsService, domainRepository, logger, upn, clientId);
    }

    static async Task RunTestsFromDatabaseAsync(
        IWhmcsService whmcsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string upn,
        string? clientId)
    {
        Console.WriteLine($"Retrieving domain registrations for UPN: {upn}");
        Console.WriteLine(new string('-', 60));

        try
        {
            var registrations = (await domainRepository.GetByUserAsync(upn)).ToList();

            if (registrations == null || !registrations.Any())
            {
                Console.WriteLine($"No domain registrations found for UPN: {upn}");
                return;
            }

            Console.WriteLine($"Found {registrations.Count} domain registration(s)\n");

            int testNumber = 1;
            foreach (var registration in registrations)
            {
                await RunSingleTestAsync(whmcsService, domainRepository, logger, registration, testNumber++, clientId);
            }

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Test Summary");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Total Tests: {registrations.Count}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving domains from database for UPN {Upn}", upn);
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task RunSingleDomainTestAsync(
        IWhmcsService whmcsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string topLevelDomain,
        string secondLevelDomain,
        string? clientId)
    {
        Console.WriteLine($"Testing domain: {secondLevelDomain}.{topLevelDomain}");
        Console.WriteLine(new string('-', 60));

        try
        {
            var registration = await domainRepository.GetByDomainAsync(topLevelDomain, secondLevelDomain);

            if (registration == null)
            {
                Console.WriteLine($"Domain {secondLevelDomain}.{topLevelDomain} not found in database.");
                Console.WriteLine("Creating test registration from command line parameters...");

                // Create a minimal test registration
                registration = new DomainRegistration
                {
                    Upn = "test@example.com",
                    Domain = new Domain
                    {
                        TopLevelDomain = topLevelDomain,
                        SecondLevelDomain = secondLevelDomain
                    },
                    ContactInformation = new ContactInformation
                    {
                        FirstName = "Test",
                        LastName = "User",
                        Address = "123 Test St",
                        City = "Test City",
                        State = "CA",
                        Country = "United States",
                        ZipCode = "12345",
                        EmailAddress = "test@example.com",
                        TelephoneNumber = "+1-555-123-4567"
                    }
                };
            }

            await RunSingleTestAsync(whmcsService, domainRepository, logger, registration, 1, clientId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing domain {Domain}", $"{secondLevelDomain}.{topLevelDomain}");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task RunTestsFromFileAsync(
        IWhmcsService whmcsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string jsonFilePath,
        string? clientId)
    {
        Console.WriteLine($"Processing test file: {Path.GetFileName(jsonFilePath)}");
        Console.WriteLine(new string('-', 60));

        try
        {
            string json = await File.ReadAllTextAsync(jsonFilePath);
            var registrations = JsonConvert.DeserializeObject<List<DomainRegistration>>(json);

            if (registrations == null || registrations.Count == 0)
            {
                Console.WriteLine("No test registrations found in file.");
                return;
            }

            Console.WriteLine($"Loaded {registrations.Count} test registration(s)\n");

            int testNumber = 1;
            foreach (var registration in registrations)
            {
                await RunSingleTestAsync(whmcsService, domainRepository, logger, registration, testNumber++, clientId);
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
        IWhmcsService whmcsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        DomainRegistration registration,
        int testNumber,
        string? clientId)
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

            // Step 1: Validate registration parameters
            Console.WriteLine($"  Step 1: Validating registration parameters for {domainName}...");

            if (registration.ContactInformation == null)
            {
                Console.WriteLine("  ❌ FAILED: Missing contact information");
                return;
            }

            Console.WriteLine($"  Registrant: {registration.ContactInformation.FirstName} {registration.ContactInformation.LastName}");
            Console.WriteLine($"  Email: {registration.ContactInformation.EmailAddress}");
            Console.WriteLine($"  Location: {registration.ContactInformation.City}, {registration.ContactInformation.State}");
            Console.WriteLine($"  ✓ Registration parameters validated");

            // Step 2: Check domain availability via WHMCS DomainWhois
            Console.WriteLine($"  Step 2: Checking domain availability for {domainName} (DomainWhois)...");

            bool isAvailable = await whmcsService.CheckDomainAvailabilityAsync(domainName);

            if (isAvailable)
            {
                Console.WriteLine($"  ✅ Domain {domainName} is available");
            }
            else
            {
                Console.WriteLine($"  ⚠️  Domain {domainName} is not available or check failed (check logs for details)");
                Console.WriteLine($"  ⚠️  TEST COMPLETED WITH WARNINGS: domain availability check returned false");
                return;
            }

            // Step 3: Get TLD pricing via WHMCS GetTLDPricing
            Console.WriteLine($"  Step 3: Retrieving TLD pricing for {registration.Domain.TopLevelDomain} (GetTLDPricing)...");

            try
            {
                using var pricingDoc = await whmcsService.GetTLDPricingAsync();
                if (pricingDoc.RootElement.TryGetProperty("pricing", out var pricingProp))
                {
                    if (pricingProp.TryGetProperty(registration.Domain.TopLevelDomain, out var tldPricing))
                    {
                        Console.WriteLine($"  ✅ TLD pricing retrieved for {registration.Domain.TopLevelDomain}");
                        if (tldPricing.TryGetProperty("register", out var registerPricing))
                        {
                            Console.WriteLine($"  Registration pricing: {registerPricing}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ℹ️  TLD '{registration.Domain.TopLevelDomain}' not found in pricing response");
                    }
                }
                else
                {
                    Console.WriteLine($"  ℹ️  Pricing data not present in response");
                }
            }
            catch (Exception pricingEx)
            {
                Console.WriteLine($"  ⚠️  TLD pricing check failed: {pricingEx.Message} (continuing with order)");
            }

            // Step 4: Place domain order via WHMCS AddOrder
            // Empty nameservers are intentional here: the diagnostic harness does not manage DNS zones,
            // so WHMCS will use the registrar's default nameservers for the test order.
            Console.WriteLine($"  Step 4: Placing domain order for {domainName} (AddOrder)...");

            bool orderResult = await whmcsService.AddOrderAsync(registration, [], clientId ?? string.Empty);

            if (orderResult)
            {
                Console.WriteLine($"  ✅ WHMCS API order placed successfully for {domainName}");
                Console.WriteLine($"  ✅ TEST PASSED: {domainName} order completed successfully");
            }
            else
            {
                Console.WriteLine($"  ⚠️  WHMCS API order returned false for {domainName} (check logs for details)");
                Console.WriteLine($"  ⚠️  TEST COMPLETED WITH WARNINGS: {domainName} order returned false");
            }

            // Step 5: Verify database record
            Console.WriteLine($"  Step 5: Verifying database compatibility...");

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test failed for domain {DomainName}", registration.Domain?.FullDomainName);
            Console.WriteLine($"  ❌ TEST FAILED: {ex.Message}");
        }
    }
}
