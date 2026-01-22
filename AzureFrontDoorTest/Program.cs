using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

partial class Program
{
    private class TestDomain
    {
        public string DomainName { get; set; } = string.Empty;
        public string Upn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("Azure Front Door Domain Integration Test");
        Console.WriteLine("========================================");
        Console.WriteLine();

        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var config = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .AddEnvironmentVariables()
                    .Build();

                // Azure Configuration
                string subscriptionId = config["AZURE_SUBSCRIPTION_ID"] ?? throw new InvalidOperationException("AZURE_SUBSCRIPTION_ID is required");
                string resourceGroupName = config["AZURE_RESOURCE_GROUP_NAME"] ?? throw new InvalidOperationException("AZURE_RESOURCE_GROUP_NAME is required");
                string frontDoorProfileName = config["AZURE_FRONTDOOR_PROFILE_NAME"] ?? throw new InvalidOperationException("AZURE_FRONTDOOR_PROFILE_NAME is required");

                // Log configuration (masked for security)
                Console.WriteLine($"Azure Subscription ID: {Utility.MaskSensitiveValue(subscriptionId)}");
                Console.WriteLine($"Resource Group: {resourceGroupName}");
                Console.WriteLine($"Front Door Profile: {frontDoorProfileName}");
                Console.WriteLine();

                // Register services
                services.AddFrontDoorServices();
                services.AddDnsZoneService();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Get services
        var frontDoorService = host.Services.GetRequiredService<IFrontDoorService>();
        var dnsZoneService = host.Services.GetRequiredService<IDnsZoneService>();
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
                await RunTestsAsync(frontDoorService, dnsZoneService, logger, jsonFilePath);
            }
            else
            {
                // Use default test file
                string defaultFile = Path.Combine(dataRoot, "test-domains.json");
                if (!File.Exists(defaultFile))
                {
                    Console.WriteLine($"Error: Default test file not found: {defaultFile}");
                    Console.WriteLine("Usage: AzureFrontDoorTest [path-to-json-file]");
                    return;
                }
                await RunTestsAsync(frontDoorService, dnsZoneService, logger, defaultFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test execution failed");
            Console.WriteLine($"\nTest execution failed: {ex.Message}");
        }
    }

    static async Task RunTestsAsync(
        IFrontDoorService frontDoorService,
        IDnsZoneService dnsZoneService,
        ILogger logger,
        string jsonFilePath)
    {
        Console.WriteLine($"Processing test file: {Path.GetFileName(jsonFilePath)}");
        Console.WriteLine(new string('-', 60));

        try
        {
            string json = await File.ReadAllTextAsync(jsonFilePath);
            var testDomains = JsonSerializer.Deserialize<List<TestDomain>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (testDomains == null || testDomains.Count == 0)
            {
                Console.WriteLine("No test domains found in file.");
                return;
            }

            Console.WriteLine($"Loaded {testDomains.Count} test domain(s)\n");

            int passCount = 0;
            int failCount = 0;
            int skipCount = 0;

            int testNumber = 1;
            foreach (var testDomain in testDomains)
            {
                var result = await RunSingleTestAsync(frontDoorService, dnsZoneService, logger, testDomain, testNumber++);
                
                switch (result)
                {
                    case TestResult.Pass:
                        passCount++;
                        break;
                    case TestResult.Fail:
                        failCount++;
                        break;
                    case TestResult.Skip:
                        skipCount++;
                        break;
                }
            }

            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Test Summary");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"Total Tests: {testDomains.Count}");
            Console.WriteLine($"Passed: {passCount}");
            Console.WriteLine($"Failed: {failCount}");
            Console.WriteLine($"Skipped: {skipCount}");
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

    static async Task<TestResult> RunSingleTestAsync(
        IFrontDoorService frontDoorService,
        IDnsZoneService dnsZoneService,
        ILogger logger,
        TestDomain testDomain,
        int testNumber)
    {
        Console.WriteLine($"\nTest {testNumber}: {testDomain.DomainName}");
        Console.WriteLine($"Description: {testDomain.Description}");
        Console.WriteLine(new string('-', 60));

        try
        {
            // Validate domain name
            if (string.IsNullOrWhiteSpace(testDomain.DomainName))
            {
                Console.WriteLine("  ❌ FAILED: Missing domain name");
                return TestResult.Fail;
            }

            // Test 1: Check if DNS zone exists or can be created
            Console.WriteLine($"  Step 1: Checking DNS Zone for {testDomain.DomainName}...");
            bool dnsZoneExists = await dnsZoneService.DnsZoneExistsAsync(testDomain.DomainName);
            Console.WriteLine($"  Result: DNS Zone {(dnsZoneExists ? "exists" : "does not exist")}");

            if (!dnsZoneExists)
            {
                Console.WriteLine($"  ⚠️  NOTE: DNS Zone would need to be created");
                Console.WriteLine($"  ⚠️  Skipping DNS Zone creation (dry run mode)");
            }

            // Test 2: Check if domain exists in Front Door
            Console.WriteLine($"  Step 2: Checking Front Door configuration for {testDomain.DomainName}...");
            bool domainExists = await frontDoorService.DomainExistsAsync(testDomain.DomainName);
            Console.WriteLine($"  Result: Domain {(domainExists ? "already exists" : "does not exist")} in Front Door");

            if (domainExists)
            {
                Console.WriteLine($"  ℹ️  Domain is already configured in Azure Front Door");
                Console.WriteLine($"  ⚠️  SKIPPED: Domain already exists");
                return TestResult.Skip;
            }

            // Test 3: Validate domain format for Azure Front Door
            Console.WriteLine($"  Step 3: Validating domain format for Azure Front Door...");
            
            // Azure Front Door custom domain naming rules:
            // - Must be a valid domain name
            // - Resource names use hyphens instead of dots
            string sanitizedName = testDomain.DomainName.Replace(".", "-");
            Console.WriteLine($"  Sanitized resource name: {sanitizedName}");

            if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName.Length > 250)
            {
                Console.WriteLine($"  ❌ FAILED: Invalid domain name format");
                return TestResult.Fail;
            }

            Console.WriteLine($"  ✓ Domain format validated");

            // Test 4: Verify readiness for Front Door integration
            Console.WriteLine($"  Step 4: Verifying readiness for Front Door integration...");
            Console.WriteLine($"  ⚠️  NOTE: Actual Front Door configuration not performed (dry run mode)");
            Console.WriteLine($"  ⚠️  Production would configure:");
            Console.WriteLine($"    - Custom domain: {testDomain.DomainName}");
            Console.WriteLine($"    - TLS Version: Minimum TLS 1.2");
            Console.WriteLine($"    - Managed Certificate: Yes");
            Console.WriteLine($"    - DNS Validation: Required");

            Console.WriteLine($"  ✅ TEST PASSED: {testDomain.DomainName} is ready for Azure Front Door integration");
            return TestResult.Pass;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test failed for domain {DomainName}", testDomain.DomainName);
            Console.WriteLine($"  ❌ TEST FAILED: {ex.Message}");
            return TestResult.Fail;
        }
    }

    enum TestResult
    {
        Pass,
        Fail,
        Skip
    }
}

// Program class needed for User Secrets generic type parameter
public partial class Program { }
