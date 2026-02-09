using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

partial class Program
{
    private static bool _registerEnabled;
    private static bool _confirmEnabled;
    private static string? _confirmDomain;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Google Cloud Platform Domain Registration Test");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        var remainingArgs = ParseGlobalFlags(args);

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Ensure user-secrets are available even when ASPNETCORE_ENVIRONMENT != Development.
                // Environment variables remain the highest precedence.
                config.AddUserSecrets<Program>(optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var config = context.Configuration;

                // Google Cloud Platform Configuration
                string projectId = config["GOOGLE_CLOUD_PROJECT_ID"] ?? throw new InvalidOperationException("GOOGLE_CLOUD_PROJECT_ID is required");
                string location = config["GOOGLE_DOMAINS_LOCATION"] ?? "global";
                
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

                if (_registerEnabled)
                {
                    Console.WriteLine("*** LIVE REGISTRATION ENABLED ***");
                    Console.WriteLine("You are running with --register. This can attempt to purchase domains and may incur charges.");
                    Console.WriteLine("Use --confirm (and optionally --confirm-domain <domain>) to proceed with a live registration call.");
                    Console.WriteLine();
                }

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

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            await ValidateGoogleApplicationDefaultCredentialsAsync(host.Services.GetRequiredService<IConfiguration>(), logger);

            // Get services
            var googleDomainsService = host.Services.GetRequiredService<IGoogleDomainsService>();
            var domainRepository = host.Services.GetRequiredService<IDomainRegistrationRepository>();

            // Parse command line arguments
            if (remainingArgs.Length > 0)
            {
                if (remainingArgs[0] == "--help" || remainingArgs[0] == "-h" || remainingArgs[0] == "-?")
                {
                    PrintUsage();
                    return;
                }

                if (remainingArgs[0] == "--interactive" || remainingArgs[0] == "-i")
                {
                    // Interactive mode: select from database
                    await RunInteractiveTestsAsync(googleDomainsService, domainRepository, logger);
                }
                else if (remainingArgs[0] == "--upn" && remainingArgs.Length > 1)
                {
                    // Filter by UPN from database
                    await RunTestsFromDatabaseAsync(googleDomainsService, domainRepository, logger, remainingArgs[1]);
                }
                else if (remainingArgs[0] == "--domain" && remainingArgs.Length > 2)
                {
                    // Specify TLD and SLD from command line
                    await RunSingleDomainTestAsync(googleDomainsService, domainRepository, logger, remainingArgs[1], remainingArgs[2]);
                }
                else if (File.Exists(remainingArgs[0]))
                {
                    // Use specified JSON file
                    await RunTestsFromFileAsync(googleDomainsService, domainRepository, logger, remainingArgs[0]);
                }
                else
                {
                    Console.WriteLine($"Error: Invalid argument or file not found: {remainingArgs[0]}");
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
                    await RunTestsFromFileAsync(googleDomainsService, domainRepository, logger, defaultFile);
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

    static async Task ValidateGoogleApplicationDefaultCredentialsAsync(IConfiguration config, ILogger logger)
    {
        // Allow users/CI to bypass credential validation if desired.
        // Example: set SKIP_GOOGLE_ADC_VALIDATION=true
        string? skip = Environment.GetEnvironmentVariable("SKIP_GOOGLE_ADC_VALIDATION");
        if (string.Equals(skip, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(skip, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(skip, "yes", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Skipping Google ADC validation due to SKIP_GOOGLE_ADC_VALIDATION");
            return;
        }

        // Note: user-secrets are provided via IConfiguration, not OS env vars.
        // Read from IConfiguration so user-secrets work, then mirror into the process env var
        // because Google ADC checks the environment variable.
        string? credentialsPath = config["GOOGLE_APPLICATION_CREDENTIALS"];
        if (!string.IsNullOrWhiteSpace(credentialsPath) && !File.Exists(credentialsPath))
        {
            throw new InvalidOperationException(
                "GOOGLE_APPLICATION_CREDENTIALS is set but the file does not exist. " +
                $"Path: {credentialsPath}");
        }

        if (!string.IsNullOrWhiteSpace(credentialsPath))
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
        }

        try
        {
            // This will throw if ADC isn't configured.
            _ = await GoogleCredential.GetApplicationDefaultAsync();
            logger.LogInformation("Google Application Default Credentials detected.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Application Default Credentials (ADC) were not found");

            throw new InvalidOperationException(
                "Google Application Default Credentials (ADC) are not configured. " +
                "Configure one of the following before running tests:\n" +
                "1) Local dev (recommended): install Google Cloud SDK and run: gcloud auth application-default login\n" +
                "2) Service account JSON: set GOOGLE_APPLICATION_CREDENTIALS to the path of a service account key JSON file\n" +
                "See: https://cloud.google.com/docs/authentication/external/set-up-adc",
                ex);
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  GoogleDomainRegistrationTest                           - Use default test file");
        Console.WriteLine("  GoogleDomainRegistrationTest <json-file>               - Use specified JSON file");
        Console.WriteLine("  GoogleDomainRegistrationTest --interactive             - Select domain from database");
        Console.WriteLine("  GoogleDomainRegistrationTest --upn <email>             - Test all domains for a user");
        Console.WriteLine("  GoogleDomainRegistrationTest --domain <tld> <sld>      - Test specific domain");
        Console.WriteLine();
        Console.WriteLine("Global flags:");
        Console.WriteLine("  --register                       - Enable live RegisterDomainAsync call (still requires --confirm)");
        Console.WriteLine("  --confirm                        - Required to actually attempt a live registration");
        Console.WriteLine("  --confirm-domain <domain>        - Optional safety: only register when domain matches (e.g. mytestdomain.com)");
        Console.WriteLine("  --help                           - Show help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  GoogleDomainRegistrationTest --interactive");
        Console.WriteLine("  GoogleDomainRegistrationTest --upn testuser@example.com");
        Console.WriteLine("  GoogleDomainRegistrationTest --domain com example");
        Console.WriteLine("  GoogleDomainRegistrationTest --domain com mythrowawaydomain --register --confirm");
        Console.WriteLine("  GoogleDomainRegistrationTest --domain com mythrowawaydomain --register --confirm --confirm-domain mythrowawaydomain.com");
    }

    private static string[] ParseGlobalFlags(string[] args)
    {
        var remaining = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--register":
                    _registerEnabled = true;
                    continue;

                case "--confirm":
                    _confirmEnabled = true;
                    continue;

                case "--confirm-domain":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --confirm-domain requires a value (e.g. mydomain.com)");
                        PrintUsage();
                        Environment.ExitCode = 2;
                        return Array.Empty<string>();
                    }

                    _confirmDomain = args[i + 1];
                    i++;
                    continue;

                default:
                    remaining.Add(arg);
                    continue;
            }
        }

        return remaining.ToArray();
    }

    private static bool ShouldAttemptLiveRegistration(string domainName)
    {
        if (!_registerEnabled)
        {
            return false;
        }

        if (!_confirmEnabled)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_confirmDomain) &&
            !string.Equals(_confirmDomain.Trim(), domainName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool PromptForLiveRegistrationConfirmation(string domainName)
    {
        Console.WriteLine();
        Console.WriteLine("*** LIVE REGISTRATION CONFIRMATION REQUIRED ***");
        Console.WriteLine("This will call Google Cloud Domains RegisterDomain and may incur charges.");
        Console.WriteLine($"Domain: {domainName}");
        Console.Write("Type REGISTER to proceed: ");
        var input = Console.ReadLine();
        return string.Equals(input?.Trim(), "REGISTER", StringComparison.OrdinalIgnoreCase);
    }

    static async Task RunInteractiveTestsAsync(
        IGoogleDomainsService googleDomainsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger)
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

        await RunTestsFromDatabaseAsync(googleDomainsService, domainRepository, logger, upn);
    }

    static async Task RunTestsFromDatabaseAsync(
        IGoogleDomainsService googleDomainsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string upn)
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
                await RunSingleTestAsync(googleDomainsService, domainRepository, logger, registration, testNumber++);
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
        IGoogleDomainsService googleDomainsService,
        IDomainRegistrationRepository domainRepository,
        ILogger logger,
        string topLevelDomain,
        string secondLevelDomain)
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

            await RunSingleTestAsync(googleDomainsService, domainRepository, logger, registration, 1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing domain {Domain}", $"{secondLevelDomain}.{topLevelDomain}");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task RunTestsFromFileAsync(
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

            var doLiveRegistration = ShouldAttemptLiveRegistration(domainName);

            if (_registerEnabled && !_confirmEnabled)
            {
                Console.WriteLine("  ⚠️  NOTE: --register was provided but --confirm was not. Skipping live registration.");
            }

            if (_registerEnabled && _confirmEnabled && !string.IsNullOrWhiteSpace(_confirmDomain) &&
                !string.Equals(_confirmDomain.Trim(), domainName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  ⚠️  NOTE: --confirm-domain '{_confirmDomain}' does not match '{domainName}'. Skipping live registration.");
            }

            if (doLiveRegistration)
            {
                // Extra safety if user did not use --confirm-domain.
                if (string.IsNullOrWhiteSpace(_confirmDomain) && !PromptForLiveRegistrationConfirmation(domainName))
                {
                    Console.WriteLine("  ⚠️  Live registration cancelled by user input. Continuing in dry run mode.");
                    Console.WriteLine($"  ✓ Registration parameters validated");
                }
                else
                {
                    Console.WriteLine($"  Step 2b: LIVE registration attempt for {domainName}...");
                    var registrationStarted = await googleDomainsService.RegisterDomainAsync(registration);

                    if (!registrationStarted)
                    {
                        Console.WriteLine("  ❌ FAILED: Live registration call failed (see logs for details)");
                        return;
                    }

                    Console.WriteLine("  ✓ Live registration call started (Google returns a long-running operation; completion is asynchronous)");
                }
            }
            else
            {
                Console.WriteLine($"  ✓ Registration parameters validated");
                Console.WriteLine($"  ⚠️  NOTE: Actual registration not performed (dry run mode)");
                Console.WriteLine("  Tip: add --register --confirm to attempt a live RegisterDomain call");
            }

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
