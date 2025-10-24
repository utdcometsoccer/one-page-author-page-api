using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorLib.API.Amazon;
using System.Text.Json;

namespace AmazonProductTestConsole;

/// <summary>
/// Enhanced console application to test and debug Amazon Product Advertising API integration
/// </summary>
class AdvancedProgram
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Amazon Product API Advanced Test Console ===");
        Console.WriteLine();

        // Parse command line arguments
        var options = ParseArguments(args);
        
        if (options.ShowHelp)
        {
            ShowHelp();
            return;
        }

        if (options.ShowPartnerTagHelp)
        {
            PartnerTagValidator.ShowPartnerTagGuidance();
            if (!options.NoWait)
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            return;
        }

        try
        {
            // Build the host with DI container
            var host = CreateHostBuilder(args).Build();

            // Get services from DI container
            var amazonService = host.Services.GetRequiredService<IAmazonProductService>();
            var amazonConfig = host.Services.GetRequiredService<IAmazonProductConfig>();
            var logger = host.Services.GetRequiredService<ILogger<AdvancedProgram>>();

            // Display configuration if requested
            if (options.ShowConfig)
            {
                amazonConfig.DisplayConfiguration();
                
                // Analyze the partner tag
                Console.WriteLine();
                PartnerTagValidator.AnalyzePartnerTag(amazonConfig.PartnerTag);
                Console.WriteLine();
            }

            // Test single author
            if (!string.IsNullOrEmpty(options.AuthorName))
            {
                await TestSingleAuthor(amazonService, options.AuthorName, logger);
            }

            // Test multiple authors
            if (options.TestMultiple)
            {
                await TestMultipleAuthors(amazonService);
            }

            logger.LogInformation("Amazon Product API testing completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Fatal Error:");
            Console.WriteLine(ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }

        if (!options.NoWait)
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    static async Task TestSingleAuthor(IAmazonProductService service, string authorName, ILogger logger)
    {
        Console.WriteLine($"🔍 Testing: {authorName}");
        Console.WriteLine(new string('=', 50));

        try
        {
            logger.LogInformation("Starting Amazon Product API test for author: {AuthorName}", authorName);

            Console.WriteLine("📞 Calling Amazon Product API...");
            Console.WriteLine("🔧 Debugging signature generation...");
            
            // Create a test hash to verify our hash functions
            TestHashFunctions();
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            using var result = await service.SearchBooksByAuthorAsync(authorName);
            
            stopwatch.Stop();
            Console.WriteLine($"✅ Success! API call completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine();

            // Analyze the response
            AnalyzeResponse(result, authorName);

        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Partner Tag"))
        {
            Console.WriteLine("❌ Configuration Error:");
            Console.WriteLine(ex.Message);
            ShowConfigurationHelp();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("❌ HTTP Error:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            AnalyzeHttpError(ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Unexpected Error:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            if (ex.InnerException != null)
            {
                Console.WriteLine("Inner Exception:");
                Console.WriteLine(ex.InnerException.Message);
            }
        }
    }

    static async Task TestMultipleAuthors(IAmazonProductService service)
    {
        var authors = new[] 
        { 
            "Stephen King", 
            "J.K. Rowling", 
            "George R.R. Martin", 
            "Agatha Christie",
            "Isaac Asimov"
        };

        Console.WriteLine("🔍 Testing Multiple Authors");
        Console.WriteLine(new string('=', 50));

        await service.TestMultipleAuthorsAsync(authors);
    }

    static void AnalyzeResponse(JsonDocument result, string authorName)
    {
        Console.WriteLine("📊 Response Analysis:");
        Console.WriteLine(new string('-', 30));

        try
        {
            var root = result.RootElement;
            
            // Check for errors first
            if (root.TryGetProperty("__type", out var errorType))
            {
                Console.WriteLine($"❌ API Error Type: {errorType.GetString()}");
                if (root.TryGetProperty("Errors", out var errors))
                {
                    Console.WriteLine("Error Details:");
                    foreach (var error in errors.EnumerateArray())
                    {
                        if (error.TryGetProperty("Message", out var message))
                        {
                            Console.WriteLine($"  - {message.GetString()}");
                        }
                    }
                }
                return;
            }

            // Analyze successful response
            if (root.TryGetProperty("SearchResult", out var searchResult))
            {
                if (searchResult.TryGetProperty("Items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    var itemCount = items.GetArrayLength();
                    Console.WriteLine($"📚 Found {itemCount} books for '{authorName}'");
                    
                    if (itemCount > 0)
                    {
                        Console.WriteLine("\n📖 Sample Books:");
                        var count = 0;
                        foreach (var item in items.EnumerateArray())
                        {
                            if (count >= 3) break; // Show first 3 books
                            
                            var title = "Unknown";
                            var asin = "Unknown";
                            
                            if (item.TryGetProperty("ASIN", out var asinProp))
                                asin = asinProp.GetString() ?? "Unknown";
                                
                            if (item.TryGetProperty("ItemInfo", out var itemInfo) &&
                                itemInfo.TryGetProperty("Title", out var titleInfo) &&
                                titleInfo.TryGetProperty("DisplayValue", out var titleProp))
                            {
                                title = titleProp.GetString() ?? "Unknown";
                            }
                            
                            Console.WriteLine($"  {count + 1}. {title} (ASIN: {asin})");
                            count++;
                        }
                    }
                }
                
                // Check for pagination info
                if (searchResult.TryGetProperty("TotalResultCount", out var totalCount))
                {
                    Console.WriteLine($"📈 Total available results: {totalCount.GetInt32()}");
                }
            }

            // Display raw JSON if small enough
            if (result.RootElement.GetRawText().Length < 2000)
            {
                Console.WriteLine("\n📄 Raw Response:");
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine($"\n📄 Raw response too large ({result.RootElement.GetRawText().Length} chars) - skipping display");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error analyzing response: {ex.Message}");
            Console.WriteLine("Raw response (first 500 chars):");
            var raw = result.RootElement.GetRawText();
            Console.WriteLine(raw.Length > 500 ? raw.Substring(0, 500) + "..." : raw);
        }
    }

    static void AnalyzeHttpError(HttpRequestException ex)
    {
        Console.WriteLine("🔧 HTTP Error Analysis:");
        
        if (ex.Message.Contains("404"))
        {
            Console.WriteLine("• 404 Not Found typically indicates:");
            Console.WriteLine("  - Invalid Partner Tag (most common)");
            Console.WriteLine("  - Incorrect API endpoint URL");
            Console.WriteLine("  - Malformed request signature");
            Console.WriteLine("  - API access not approved");
        }
        else if (ex.Message.Contains("403"))
        {
            Console.WriteLine("• 403 Forbidden typically indicates:");
            Console.WriteLine("  - Invalid AWS credentials");
            Console.WriteLine("  - Insufficient permissions");
            Console.WriteLine("  - Request signature issues");
        }
        else if (ex.Message.Contains("400"))
        {
            Console.WriteLine("• 400 Bad Request typically indicates:");
            Console.WriteLine("  - Invalid request parameters");
            Console.WriteLine("  - Malformed JSON payload");
        }
        
        ShowConfigurationHelp();
    }

    /// <summary>
    /// Tests hash functions to verify they're working correctly
    /// </summary>
    static void TestHashFunctions()
    {
        Console.WriteLine("🔧 Testing Hash Functions:");
        
        // Test SHA256 hash
        var testString = "Hello World";
        var expectedSha256 = "a591a6d40bf420404a011733cfb7b190d62c65bf0bcda32b57b277d9ad9f146e"; // Known SHA256 of "Hello World"
        var actualSha256 = GetSha256Hash(testString);
        
        Console.WriteLine($"   SHA256 Test: {(actualSha256 == expectedSha256 ? "✅ PASS" : "❌ FAIL")}");
        Console.WriteLine($"   Input: '{testString}'");
        Console.WriteLine($"   Expected: {expectedSha256}");
        Console.WriteLine($"   Actual:   {actualSha256}");
        
        // Test HMAC-SHA256
        var testKey = "key";
        var testData = "The quick brown fox jumps over the lazy dog";
        var expectedHmac = "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8"; // Known HMAC-SHA256
        var actualHmac = BytesToHex(HmacSha256(System.Text.Encoding.UTF8.GetBytes(testKey), testData));
        
        Console.WriteLine($"   HMAC Test: {(actualHmac == expectedHmac ? "✅ PASS" : "❌ FAIL")}");
        Console.WriteLine($"   Key: '{testKey}', Data: '{testData}'");
        Console.WriteLine($"   Expected: {expectedHmac}");
        Console.WriteLine($"   Actual:   {actualHmac}");
        Console.WriteLine();
    }

    /// <summary>
    /// Computes SHA-256 hash of a string (same as AmazonProductService)
    /// </summary>
    static string GetSha256Hash(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return BytesToHex(hash);
    }

    /// <summary>
    /// Converts byte array to hexadecimal string (same as AmazonProductService)
    /// </summary>
    static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Computes HMAC-SHA256 (same as AmazonProductService)
    /// </summary>
    static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
    }

    static void ShowConfigurationHelp()
    {
        Console.WriteLine();
        Console.WriteLine("🛠️  Configuration Help:");
        Console.WriteLine("To fix Amazon Product API issues:");
        Console.WriteLine("1. Sign up for Amazon Associates: https://affiliate-program.amazon.com/");
        Console.WriteLine("2. Apply for Product Advertising API access (separate from Associates)");
        Console.WriteLine("3. Get your real Partner Tag from your Associates account");
        Console.WriteLine("4. Update user secrets with your real Partner Tag:");
        Console.WriteLine("   dotnet user-secrets set \"AMAZON_PRODUCT_PARTNER_TAG\" \"your-real-tag-20\"");
    }

    static TestOptions ParseArguments(string[] args)
    {
        var options = new TestOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--config":
                case "-c":
                    options.ShowConfig = true;
                    break;
                case "--multiple":
                case "-m":
                    options.TestMultiple = true;
                    break;
                case "--partner-tag":
                case "-p":
                    options.ShowPartnerTagHelp = true;
                    break;
                case "--nowait":
                case "-n":
                    options.NoWait = true;
                    break;
                case "--author":
                case "-a":
                    if (i + 1 < args.Length)
                    {
                        options.AuthorName = args[++i];
                    }
                    break;
                default:
                    // If no flag specified, treat as author name
                    if (!args[i].StartsWith("-") && string.IsNullOrEmpty(options.AuthorName))
                    {
                        options.AuthorName = string.Join(" ", args.Skip(i));
                        break;
                    }
                    break;
            }
        }

        // Default to Stephen King if no author specified and not showing help
        if (string.IsNullOrEmpty(options.AuthorName) && !options.ShowHelp && !options.TestMultiple)
        {
            options.AuthorName = "Stephen King";
        }

        return options;
    }

    static void ShowHelp()
    {
        Console.WriteLine("Amazon Product API Test Console");
        Console.WriteLine("Usage: AmazonProductTestConsole [options] [author name]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help        Show this help message");
        Console.WriteLine("  -c, --config      Display current configuration");
        Console.WriteLine("  -m, --multiple    Test multiple authors");
        Console.WriteLine("  -p, --partner-tag Show Partner Tag help and guidance");
        Console.WriteLine("  -n, --nowait      Don't wait for key press at end");
        Console.WriteLine("  -a, --author      Specify author name (alternative to positional)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  AmazonProductTestConsole");
        Console.WriteLine("  AmazonProductTestConsole \"Stephen King\"");
        Console.WriteLine("  AmazonProductTestConsole --config --author \"J.K. Rowling\"");
        Console.WriteLine("  AmazonProductTestConsole --multiple");
        Console.WriteLine("  AmazonProductTestConsole --partner-tag");
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddUserSecrets<AdvancedProgram>();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug); // Enable debug logging to see signature details
                });

                services.AddSingleton<IAmazonProductConfig, AmazonProductConfig>();
                services.AddHttpClient<IAmazonProductService, AmazonProductService>();
            });

    public class TestOptions
    {
        public bool ShowHelp { get; set; }
        public bool ShowConfig { get; set; }
        public bool TestMultiple { get; set; }
        public bool ShowPartnerTagHelp { get; set; }
        public bool NoWait { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}