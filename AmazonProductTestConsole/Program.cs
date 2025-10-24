using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorLib.API.Amazon;
using System.Text.Json;

namespace AmazonProductTestConsole;

/// <summary>
/// Console application to test Amazon Product Advertising API integration
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Amazon Product API Test Console ===");
        Console.WriteLine();

        // Get author name from command line args or use default
        var authorName = args.Length > 0 ? string.Join(" ", args) : "Stephen King";
        Console.WriteLine($"Searching for books by: {authorName}");
        Console.WriteLine();

        try
        {
            // Build the host with DI container
            var host = CreateHostBuilder(args).Build();

            // Get the service from DI container
            var amazonService = host.Services.GetRequiredService<IAmazonProductService>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting Amazon Product API test for author: {AuthorName}", authorName);

            // Test the service
            Console.WriteLine("Calling Amazon Product API...");
            using var result = await amazonService.SearchBooksByAuthorAsync(authorName);

            Console.WriteLine("✅ Success! API call completed.");
            Console.WriteLine();
            Console.WriteLine("Response:");
            Console.WriteLine("=========");

            // Pretty print the JSON response
            var jsonString = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            {
                WriteIndented = true
            });
            Console.WriteLine(jsonString);

            logger.LogInformation("Amazon Product API test completed successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Partner Tag"))
        {
            Console.WriteLine("❌ Configuration Error:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("Please ensure you have:");
            Console.WriteLine("1. A valid Amazon Associates account");
            Console.WriteLine("2. Approval for Product Advertising API access");
            Console.WriteLine("3. Correct Partner Tag in your user secrets");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("❌ HTTP Error:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("This might indicate:");
            Console.WriteLine("- Network connectivity issues");
            Console.WriteLine("- Invalid API endpoint or credentials");
            Console.WriteLine("- Amazon API service problems");
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

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Creates and configures the host builder with dependency injection
    /// </summary>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Add user secrets configuration
                config.AddUserSecrets<Program>();
            })
            .ConfigureServices((context, services) =>
            {
                // Register logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Register Amazon Product API services
                services.AddSingleton<IAmazonProductConfig, AmazonProductConfig>();
                services.AddHttpClient<IAmazonProductService, AmazonProductService>();
                
                // Alternative registration if you prefer transient
                // services.AddTransient<IAmazonProductService, AmazonProductService>();
            });
}

/// <summary>
/// Extension methods for easier service testing
/// </summary>
public static class AmazonServiceExtensions
{
    /// <summary>
    /// Tests the Amazon Product Service with multiple authors
    /// </summary>
    public static async Task TestMultipleAuthorsAsync(this IAmazonProductService service, params string[] authors)
    {
        foreach (var author in authors)
        {
            Console.WriteLine($"\n--- Testing: {author} ---");
            try
            {
                using var result = await service.SearchBooksByAuthorAsync(author);
                Console.WriteLine($"✅ Success for {author}");
                
                // Extract some basic info from the response
                if (result.RootElement.TryGetProperty("SearchResult", out var searchResult) &&
                    searchResult.TryGetProperty("Items", out var items) &&
                    items.ValueKind == JsonValueKind.Array)
                {
                    Console.WriteLine($"   Found {items.GetArrayLength()} items");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed for {author}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Displays configuration information
    /// </summary>
    public static void DisplayConfiguration(this IAmazonProductConfig config)
    {
        Console.WriteLine("Current Configuration:");
        Console.WriteLine($"  API Endpoint: {config.ApiEndpoint}");
        Console.WriteLine($"  Region: {config.Region}");
        Console.WriteLine($"  Marketplace: {config.Marketplace}");
        Console.WriteLine($"  Partner Tag: {config.PartnerTag}");
        Console.WriteLine($"  Access Key: {MaskSecret(config.AccessKey)}");
        Console.WriteLine($"  Secret Key: {MaskSecret(config.SecretKey)}");
        Console.WriteLine();
    }

    private static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length <= 8)
            return "[HIDDEN]";
        
        return secret.Substring(0, 4) + "****" + secret.Substring(secret.Length - 4);
    }
}