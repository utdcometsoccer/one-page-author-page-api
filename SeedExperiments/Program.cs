using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace SeedExperiments;

/// <summary>
/// Seeds sample A/B test experiments into Cosmos DB for testing the experiments API.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== A/B Test Experiments Seeder ===");
        Console.WriteLine();

        // Load configuration from environment variables
        var endpointUri = Environment.GetEnvironmentVariable("COSMOSDB_ENDPOINT_URI");
        var primaryKey = Environment.GetEnvironmentVariable("COSMOSDB_PRIMARY_KEY");
        var databaseId = Environment.GetEnvironmentVariable("COSMOSDB_DATABASE_ID") ?? "OnePageAuthor";

        if (string.IsNullOrWhiteSpace(endpointUri) || string.IsNullOrWhiteSpace(primaryKey))
        {
            Console.WriteLine("ERROR: Missing required environment variables:");
            Console.WriteLine("  - COSMOSDB_ENDPOINT_URI");
            Console.WriteLine("  - COSMOSDB_PRIMARY_KEY");
            Console.WriteLine("  - COSMOSDB_DATABASE_ID (optional, defaults to 'OnePageAuthor')");
            Console.WriteLine();
            Console.WriteLine("Please set these environment variables and try again.");
            return;
        }

        Console.WriteLine($"Cosmos DB Endpoint: {MaskUrl(endpointUri)}");
        Console.WriteLine($"Database: {databaseId}");
        Console.WriteLine();

        // Set up dependency injection
        var services = new ServiceCollection()
            .AddLogging()
            .AddCosmosClient(endpointUri, primaryKey)
            .AddCosmosDatabase(databaseId)
            .AddExperimentRepository();

        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<IExperimentRepository>();

        Console.WriteLine("Starting experiment seeding...");
        Console.WriteLine();

        try
        {
            // Seed Landing Page Experiments
            await SeedLandingPageExperiments(repository);
            
            // Seed Pricing Page Experiments
            await SeedPricingPageExperiments(repository);

            Console.WriteLine();
            Console.WriteLine("✓ All experiments seeded successfully!");
            Console.WriteLine();
            Console.WriteLine("You can now test the API with:");
            Console.WriteLine("  GET /api/experiments?page=landing");
            Console.WriteLine("  GET /api/experiments?page=pricing&userId=test-user-123");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to seed experiments: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static async Task SeedLandingPageExperiments(IExperimentRepository repository)
    {
        Console.WriteLine("Seeding Landing Page experiments...");

        // Experiment 1: Hero Button Color
        var heroButtonExperiment = new Experiment
        {
            id = "hero-button-color-test",
            Name = "Hero Button Color Test",
            Page = "landing",
            IsActive = true,
            Variants = new List<ExperimentVariant>
            {
                new ExperimentVariant
                {
                    Id = "control",
                    Name = "Blue Button (Control)",
                    TrafficPercentage = 50,
                    Config = new Dictionary<string, object>
                    {
                        { "buttonColor", "#007bff" },
                        { "buttonText", "Get Started" }
                    }
                },
                new ExperimentVariant
                {
                    Id = "variant_a",
                    Name = "Green Button",
                    TrafficPercentage = 50,
                    Config = new Dictionary<string, object>
                    {
                        { "buttonColor", "#28a745" },
                        { "buttonText", "Get Started" }
                    }
                }
            }
        };

        try
        {
            await repository.CreateAsync(heroButtonExperiment);
            Console.WriteLine($"  ✓ Created: {heroButtonExperiment.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Failed to create {heroButtonExperiment.Name}: {ex.Message}");
        }

        // Experiment 2: Headline Text
        var headlineExperiment = new Experiment
        {
            id = "hero-headline-test",
            Name = "Hero Headline Test",
            Page = "landing",
            IsActive = true,
            Variants = new List<ExperimentVariant>
            {
                new ExperimentVariant
                {
                    Id = "control",
                    Name = "Create Your Author Page (Control)",
                    TrafficPercentage = 33,
                    Config = new Dictionary<string, object>
                    {
                        { "headline", "Create Your Author Page" },
                        { "subheadline", "Share your stories with the world" }
                    }
                },
                new ExperimentVariant
                {
                    Id = "variant_a",
                    Name = "Build Your Author Brand",
                    TrafficPercentage = 33,
                    Config = new Dictionary<string, object>
                    {
                        { "headline", "Build Your Author Brand" },
                        { "subheadline", "Connect with readers everywhere" }
                    }
                },
                new ExperimentVariant
                {
                    Id = "variant_b",
                    Name = "Start Your Author Journey",
                    TrafficPercentage = 34,
                    Config = new Dictionary<string, object>
                    {
                        { "headline", "Start Your Author Journey" },
                        { "subheadline", "Showcase your books and engage readers" }
                    }
                }
            }
        };

        try
        {
            await repository.CreateAsync(headlineExperiment);
            Console.WriteLine($"  ✓ Created: {headlineExperiment.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Failed to create {headlineExperiment.Name}: {ex.Message}");
        }
    }

    static async Task SeedPricingPageExperiments(IExperimentRepository repository)
    {
        Console.WriteLine("Seeding Pricing Page experiments...");

        // Experiment 1: Pricing Card Design
        var pricingCardExperiment = new Experiment
        {
            id = "pricing-card-design-test",
            Name = "Pricing Card Design Test",
            Page = "pricing",
            IsActive = true,
            Variants = new List<ExperimentVariant>
            {
                new ExperimentVariant
                {
                    Id = "control",
                    Name = "Traditional Card (Control)",
                    TrafficPercentage = 50,
                    Config = new Dictionary<string, object>
                    {
                        { "cardStyle", "traditional" },
                        { "showBadge", false },
                        { "highlightColor", "#007bff" }
                    }
                },
                new ExperimentVariant
                {
                    Id = "variant_a",
                    Name = "Modern Card with Badge",
                    TrafficPercentage = 50,
                    Config = new Dictionary<string, object>
                    {
                        { "cardStyle", "modern" },
                        { "showBadge", true },
                        { "highlightColor", "#28a745" },
                        { "badgeText", "Most Popular" }
                    }
                }
            }
        };

        try
        {
            await repository.CreateAsync(pricingCardExperiment);
            Console.WriteLine($"  ✓ Created: {pricingCardExperiment.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Failed to create {pricingCardExperiment.Name}: {ex.Message}");
        }

        // Experiment 2: CTA Button Text
        var ctaButtonExperiment = new Experiment
        {
            id = "pricing-cta-button-test",
            Name = "Pricing CTA Button Text Test",
            Page = "pricing",
            IsActive = true,
            Variants = new List<ExperimentVariant>
            {
                new ExperimentVariant
                {
                    Id = "control",
                    Name = "Subscribe Now (Control)",
                    TrafficPercentage = 40,
                    Config = new Dictionary<string, object>
                    {
                        { "buttonText", "Subscribe Now" }
                    }
                },
                new ExperimentVariant
                {
                    Id = "variant_a",
                    Name = "Start Free Trial",
                    TrafficPercentage = 30,
                    Config = new Dictionary<string, object>
                    {
                        { "buttonText", "Start Free Trial" }
                    }
                },
                new ExperimentVariant
                {
                    Id = "variant_b",
                    Name = "Get Started Today",
                    TrafficPercentage = 30,
                    Config = new Dictionary<string, object>
                    {
                        { "buttonText", "Get Started Today" }
                    }
                }
            }
        };

        try
        {
            await repository.CreateAsync(ctaButtonExperiment);
            Console.WriteLine($"  ✓ Created: {ctaButtonExperiment.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Failed to create {ctaButtonExperiment.Name}: {ex.Message}");
        }
    }

    static string MaskUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "(not set)";
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}:***";
    }
}
