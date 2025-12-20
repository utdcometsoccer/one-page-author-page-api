using System.Text.Json;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
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
                                    Console.WriteLine("Starting Testimonial Seeding...");
                                    Console.WriteLine($"Cosmos DB Endpoint configured: {Utility.MaskUrl(endpointUri)}");
                                    Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

                                    // Register Cosmos via extensions and domain services
                                    services
                                        .AddCosmosClient(endpointUri, primaryKey)
                                        .AddCosmosDatabase(databaseId)
                                        .AddTestimonialRepository();
                                })
                                .Build())
        {
            // Get the Testimonial repository from DI
            var testimonialRepository = host.Services.GetRequiredService<ITestimonialRepository>();

            // Read testimonials data file
            string dataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            string testimonialsFile = Path.Combine(dataRoot, "testimonials.json");
            
            if (!File.Exists(testimonialsFile))
            {
                Console.WriteLine($"Testimonials data file not found: {testimonialsFile}");
                return;
            }

            Console.WriteLine($"Processing testimonials file: {testimonialsFile}");

            try
            {
                string json = File.ReadAllText(testimonialsFile);
                var testimonials = JsonSerializer.Deserialize<List<Testimonial>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (testimonials == null || testimonials.Count == 0)
                {
                    Console.WriteLine("No testimonials found in file");
                    return;
                }

                Console.WriteLine($"Found {testimonials.Count} testimonials to seed");

                int created = 0;
                int skipped = 0;

                foreach (var testimonial in testimonials)
                {
                    try
                    {
                        // Generate a deterministic ID based on author name and locale for idempotency
                        testimonial.id = $"{testimonial.AuthorName.Replace(" ", "-").ToLower()}-{testimonial.Locale.ToLower()}";
                        
                        // Check if testimonial already exists
                        var existing = await testimonialRepository.GetByIdAsync(testimonial.id);
                        if (existing != null)
                        {
                            Console.WriteLine($"Testimonial already exists: {testimonial.AuthorName} ({testimonial.Locale}) - skipping");
                            skipped++;
                            continue;
                        }

                        // Create testimonial
                        await testimonialRepository.CreateAsync(testimonial);
                        Console.WriteLine($"Created testimonial: {testimonial.AuthorName} ({testimonial.Locale})");
                        created++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating testimonial {testimonial.AuthorName}: {ex.Message}");
                    }
                }

                Console.WriteLine("\nSeeding Summary:");
                Console.WriteLine($"Total testimonials processed: {testimonials.Count}");
                Console.WriteLine($"Created: {created}");
                Console.WriteLine($"Skipped (already exist): {skipped}");
                Console.WriteLine("\nSeeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing testimonials file: {ex.Message}");
            }
        }
    }
}
