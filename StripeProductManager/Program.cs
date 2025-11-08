using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Text.Json;

namespace InkStainedWretch.StripeProductManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Ink Stained Wretch - Stripe Product Manager");
            Console.WriteLine("===============================================");

            var host = CreateHostBuilder(args).Build();
            var productManager = host.Services.GetRequiredService<StripeProductManager>();

            try
            {
                await productManager.CreateOrUpdateProductsAsync();
                Console.WriteLine("✅ All products and prices have been successfully created/updated!");
                Console.WriteLine("✅ Culture information added to all products!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddUserSecrets<Program>();
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging();
                    
                    // Configure Stripe settings
                    var stripeSettings = new StripeSettings();
                    context.Configuration.GetSection("Stripe").Bind(stripeSettings);
                    services.AddSingleton(stripeSettings);
                    
                    services.AddSingleton<StripeProductManager>();
                    
                    // Configure Stripe API key
                    if (string.IsNullOrEmpty(stripeSettings.SecretKey))
                    {
                        throw new InvalidOperationException("Stripe:SecretKey is required. Set it in appsettings.json or user secrets.");
                    }
                    StripeConfiguration.ApiKey = stripeSettings.SecretKey;
                });
    }

    public class StripeProductManager
    {
        private readonly ILogger<StripeProductManager> _logger;
        private readonly StripeSettings _stripeSettings;
        private readonly ProductService _productService;
        private readonly PriceService _priceService;

        public StripeProductManager(ILogger<StripeProductManager> logger, StripeSettings stripeSettings)
        {
            _logger = logger;
            _stripeSettings = stripeSettings;
            _productService = new ProductService();
            _priceService = new PriceService();
        }

        public async Task CreateOrUpdateProductsAsync()
        {
            var products = GetProductDefinitions();

            foreach (var productDef in products)
            {
                _logger.LogInformation("Processing product: {ProductName}", productDef.Name);
                Console.WriteLine($"\n🔄 Processing: {productDef.Name}");
                
                // Show culture information
                var productConfig = _stripeSettings.Products.FirstOrDefault(p => p.Name == productDef.Name);
                if (productConfig != null)
                {
                    Console.WriteLine($"   📍 Cultures: {productConfig.PrimaryCulture} (primary), {string.Join(", ", productConfig.SupportedCultures.Where(c => c != productConfig.PrimaryCulture))}");
                    Console.WriteLine($"   🌐 Localized versions: {productConfig.CultureSpecificInfo.Count} languages");
                }

                try
                {
                    // Create or update product
                    var product = await CreateOrUpdateProductAsync(productDef);
                    Console.WriteLine($"✅ Product created/updated: {product.Id}");

                    // Create or update price
                    var price = await CreateOrUpdatePriceAsync(product.Id, productDef);
                    Console.WriteLine($"✅ Price created/updated: {price.Id} (Nickname: {price.Nickname})");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing product {ProductName}", productDef.Name);
                    Console.WriteLine($"❌ Error processing {productDef.Name}: {ex.Message}");
                }
            }
        }

        private async Task<Product> CreateOrUpdateProductAsync(ProductDefinition productDef)
        {
            // Try to find existing product by name
            var existingProducts = await _productService.ListAsync(new ProductListOptions
            {
                Active = true,
                Limit = 100
            });

            var existingProduct = existingProducts.Data
                .FirstOrDefault(p => p.Name.Equals(productDef.Name, StringComparison.OrdinalIgnoreCase));

            if (existingProduct != null)
            {
                _logger.LogInformation("Updating existing product: {ProductId}", existingProduct.Id);
                
                var updateOptions = new ProductUpdateOptions
                {
                    Description = productDef.Description,
                    Metadata = productDef.Metadata
                };

                return await _productService.UpdateAsync(existingProduct.Id, updateOptions);
            }
            else
            {
                _logger.LogInformation("Creating new product: {ProductName}", productDef.Name);
                
                var createOptions = new ProductCreateOptions
                {
                    Name = productDef.Name,
                    Description = productDef.Description,
                    Type = "service",
                    Metadata = productDef.Metadata
                };

                return await _productService.CreateAsync(createOptions);
            }
        }

        private async Task<Price> CreateOrUpdatePriceAsync(string productId, ProductDefinition productDef)
        {
            // Try to find existing price by nickname and product
            var existingPrices = await _priceService.ListAsync(new PriceListOptions
            {
                Product = productId,
                Active = true,
                Limit = 100
            });

            var existingPrice = existingPrices.Data
                .FirstOrDefault(p => p.Nickname?.Equals(productDef.PriceNickname, StringComparison.OrdinalIgnoreCase) == true);

            if (existingPrice != null)
            {
                _logger.LogInformation("Updating existing price: {PriceId}", existingPrice.Id);
                
                var updateOptions = new PriceUpdateOptions
                {
                    Nickname = productDef.PriceNickname,
                    Metadata = productDef.PriceMetadata
                };

                return await _priceService.UpdateAsync(existingPrice.Id, updateOptions);
            }
            else
            {
                _logger.LogInformation("Creating new price for product: {ProductId}", productId);
                
                var createOptions = new PriceCreateOptions
                {
                    Product = productId,
                    UnitAmount = productDef.PriceInCents,
                    Currency = "usd",
                    Nickname = productDef.PriceNickname,
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = "year",
                        IntervalCount = productDef.IntervalCount
                    },
                    Metadata = productDef.PriceMetadata
                };

                return await _priceService.CreateAsync(createOptions);
            }
        }

        private List<ProductDefinition> GetProductDefinitions()
        {
            var productDefinitions = new List<ProductDefinition>();

            foreach (var productConfig in _stripeSettings.Products)
            {
                var productDefinition = new ProductDefinition
                {
                    Name = productConfig.Name,
                    Description = productConfig.Description,
                    PriceInCents = productConfig.PriceInCents,
                    PriceNickname = productConfig.PriceNickname,
                    IntervalCount = productConfig.IntervalCount,
                    Metadata = GetProductMetadata(productConfig.PlanType, productConfig),
                    PriceMetadata = GetPriceMetadata(productConfig.PlanType, productConfig.IntervalCount)
                };

                productDefinitions.Add(productDefinition);
            }

            return productDefinitions;
        }

        private Dictionary<string, string> GetProductMetadata(string planType, StripeProductSettings productConfig)
        {
            var features = GetProductFeatures();
            var defaultCultureData = GetCultureMetadata();

            var metadata = new Dictionary<string, string>
            {
                ["plan_type"] = planType,
                ["platform"] = "ink_stained_wretch",
                ["version"] = "1.0",
                ["created_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["features"] = string.Join(";", features.Take(10)), // Stripe metadata has character limits
                ["total_features"] = features.Count.ToString(),
                ["supported_cultures"] = string.Join(",", productConfig.SupportedCultures),
                ["primary_language"] = productConfig.PrimaryCulture,
                ["multi_language"] = productConfig.SupportedCultures.Count > 1 ? "true" : "false",
                ["cloud_provider"] = "azure",
                ["architecture"] = "serverless",
                ["culture_count"] = productConfig.SupportedCultures.Count.ToString(),
                ["localized_versions"] = productConfig.CultureSpecificInfo.Count.ToString()
            };

            // Add individual feature flags
            for (int i = 0; i < Math.Min(features.Count, 15); i++) // Reduced to make room for culture info
            {
                metadata[$"feature_{i + 1:D2}"] = features[i];
            }

            // Add product-specific culture information
            var cultureIndex = 1;
            foreach (var cultureCode in productConfig.SupportedCultures.Take(8)) // Limit due to metadata constraints
            {
                metadata[$"culture_{cultureIndex:D2}_code"] = cultureCode;
                
                if (defaultCultureData.ContainsKey(cultureCode))
                {
                    metadata[$"culture_{cultureIndex:D2}_name"] = defaultCultureData[cultureCode];
                }

                // Add localized product information if available
                if (productConfig.CultureSpecificInfo.ContainsKey(cultureCode))
                {
                    var cultureInfo = productConfig.CultureSpecificInfo[cultureCode];
                    metadata[$"culture_{cultureIndex:D2}_localized_name"] = cultureInfo.LocalizedName.Length > 50 
                        ? cultureInfo.LocalizedName.Substring(0, 47) + "..." 
                        : cultureInfo.LocalizedName;
                    metadata[$"culture_{cultureIndex:D2}_localized_nickname"] = cultureInfo.LocalizedNickname;
                }

                cultureIndex++;
            }

            return metadata;
        }

        private Dictionary<string, string> GetPriceMetadata(string planType, int years)
        {
            return new Dictionary<string, string>
            {
                ["billing_cycle"] = $"{years}_year",
                ["plan_type"] = planType,
                ["renewal_period"] = $"{years} year(s)",
                ["currency"] = "USD",
                ["pricing_model"] = "subscription",
                ["auto_renewal"] = "true",
                ["trial_period"] = "false",
                ["cancellation_policy"] = "anytime",
                ["refund_policy"] = "pro_rated",
                ["created_date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["last_updated"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            };
        }

        private List<string> GetProductFeatures()
        {
            return new List<string>
            {
                // Authentication & Identity
                "Microsoft Entra ID Integration",
                "Single Sign-On (SSO)",
                "User Profile Management",
                "Identity Claims Processing",
                "Session Management",
                
                // Image Management
                "Multi-Format Image Upload",
                "Azure Blob Storage Integration",
                "Tier-Based Storage Plans",
                "Image Organization & Retrieval",
                "Secure Upload & Access Control",
                
                // Domain Registration
                "Custom Domain Registration",
                "Domain Validation Services",
                "Automated DNS Zone Creation",
                "SSL Certificate Management",
                "Domain Portfolio Management",
                
                // Payment & Subscriptions
                "Stripe Payment Integration",
                "Recurring Billing Management",
                "Multiple Payment Methods",
                "Invoice Management",
                "Payment Analytics",
                
                // Content Discovery
                "Penguin Random House Integration",
                "Amazon Product API",
                "Book Search & Discovery",
                "Author Search Functionality",
                "Affiliate Marketing Support",
                
                // Internationalization
                "Multi-Language Support (6 languages)",
                "English (EN) Support",
                "Spanish (ES) Localization",
                "French (FR) Support",
                "Arabic (AR) RTL Support",
                "Chinese Simplified (ZH-CN)",
                "Chinese Traditional (ZH-TW)",
                
                // Geographic Services
                "Country Data Management",
                "State/Province Support",
                "Address Validation",
                "International Address Formats",
                "Geographic Search",
                
                // Developer Features
                "RESTful API Design",
                "API Documentation",
                "Rate Limiting",
                "API Versioning",
                "Webhook Support",
                
                // Infrastructure
                "Azure Functions Serverless",
                "Azure Cosmos DB",
                "Azure Front Door CDN",
                "Auto-Scaling",
                "Global Distribution",
                
                // Security & Compliance
                "Enterprise Security",
                "Data Encryption",
                "Role-Based Access Control",
                "Audit Logging",
                "WCAG Compliance",
                
                // Business Intelligence
                "Usage Analytics",
                "Revenue Tracking",
                "Performance Insights",
                "Custom Reporting",
                "Real-Time Monitoring",
                
                // Accessibility
                "Cross-Platform Support",
                "Responsive Design",
                "Screen Reader Support",
                "Keyboard Navigation",
                "High Contrast Mode"
            };
        }

        private Dictionary<string, string> GetCultureMetadata()
        {
            return new Dictionary<string, string>
            {
                ["en-US"] = "English (United States)",
                ["en-CA"] = "English (Canada)",
                ["es-US"] = "Spanish (United States)",
                ["es-MX"] = "Spanish (Mexico)",
                ["fr-CA"] = "French (Canada)",
                ["fr-FR"] = "French (France)",
                ["ar-SA"] = "Arabic (Saudi Arabia)",
                ["ar-EG"] = "Arabic (Egypt)",
                ["zh-CN"] = "Chinese (Simplified, China)",
                ["zh-TW"] = "Chinese (Traditional, Taiwan)",
                ["pt-BR"] = "Portuguese (Brazil)",
                ["de-DE"] = "German (Germany)",
                ["it-IT"] = "Italian (Italy)",
                ["ja-JP"] = "Japanese (Japan)",
                ["ko-KR"] = "Korean (South Korea)"
            };
        }
    }

    public class ProductDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long PriceInCents { get; set; }
        public string PriceNickname { get; set; } = string.Empty;
        public int IntervalCount { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public Dictionary<string, string> PriceMetadata { get; set; } = new();
    }
}