using Microsoft.Extensions.Logging;
using Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    /// <summary>
    /// Service implementation for mapping Stripe prices to subscription plans with features from Stripe products.
    /// </summary>
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly ILogger<SubscriptionPlanService> _logger;

        public SubscriptionPlanService(ILogger<SubscriptionPlanService> logger)
        {
            _logger = logger;
        }

        public async Task<SubscriptionPlan> MapToSubscriptionPlanAsync(PriceDto priceDto)
        {
            if (priceDto == null)
            {
                throw new ArgumentNullException(nameof(priceDto));
            }

            _logger.LogInformation("Mapping PriceDto to SubscriptionPlan for product {ProductId}", priceDto.ProductId);

            var features = await GetProductFeaturesAsync(priceDto.ProductId, priceDto.ProductName, priceDto.ProductDescription);

            var plan = new SubscriptionPlan
            {
                Id = priceDto.Id,
                StripePriceId = priceDto.Id,
                Label = GetValidLabel(priceDto.Nickname, priceDto.ProductName),
                Name = priceDto.ProductName,
                Description = priceDto.ProductDescription,
                Price = priceDto.AmountDecimal,
                Currency = priceDto.Currency.ToUpperInvariant(),
                Duration = CalculateDurationInMonths(priceDto),
                Features = features
            };

            _logger.LogInformation("Successfully mapped SubscriptionPlan with {FeatureCount} features", features.Count);
            return plan;
        }

        public async Task<List<SubscriptionPlan>> MapToSubscriptionPlansAsync(IEnumerable<PriceDto> priceDtos)
        {
            if (priceDtos == null)
            {
                throw new ArgumentNullException(nameof(priceDtos));
            }

            var plans = new List<SubscriptionPlan>();
            
            foreach (var priceDto in priceDtos)
            {
                try
                {
                    var plan = await MapToSubscriptionPlanAsync(priceDto);
                    plans.Add(plan);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error mapping PriceDto {PriceId} to SubscriptionPlan", priceDto?.Id);
                    // Continue processing other prices even if one fails
                }
            }

            _logger.LogInformation("Successfully mapped {PlanCount} subscription plans", plans.Count);
            return plans;
        }

        /// <summary>
        /// Retrieves features from Stripe product metadata and features.
        /// </summary>
        /// <param name="productId">The Stripe product ID</param>
        /// <param name="productName">The product name for fallback feature generation</param>
        /// <param name="productDescription">The product description for fallback feature generation</param>
        /// <returns>List of features for the product</returns>
        private async Task<List<string>> GetProductFeaturesAsync(string productId, string productName = "", string productDescription = "")
        {
            if (string.IsNullOrEmpty(productId))
            {
                _logger.LogWarning("ProductId is null or empty, using product name for default features");
                // Create a mock product with the available information for feature generation
                var mockProduct = new Product 
                { 
                    Id = "mock", 
                    Name = productName ?? "Default Plan", 
                    Description = productDescription ?? "A default subscription plan" 
                };
                return GetDefaultFeaturesForProduct(mockProduct);
            }

            try
            {
                _logger.LogInformation("Retrieving features for Stripe product {ProductId}", productId);

                var productService = new ProductService();
                var product = await productService.GetAsync(productId);

                if (product == null)
                {
                    _logger.LogWarning("Stripe product {ProductId} not found", productId);
                    return new List<string>();
                }

                var features = new List<string>();

                // Check if product has features in metadata
                if (product.Metadata?.ContainsKey("features") == true)
                {
                    var featuresString = product.Metadata["features"];
                    if (!string.IsNullOrEmpty(featuresString))
                    {
                        // Split by comma or semicolon for multiple features
                        var metadataFeatures = featuresString
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(f => f.Trim())
                            .Where(f => !string.IsNullOrEmpty(f))
                            .ToList();
                        
                        features.AddRange(metadataFeatures);
                    }
                }

                // Check for individual feature metadata keys (feature_1, feature_2, etc.)
                if (product.Metadata != null)
                {
                    var featureKeys = product.Metadata.Keys
                        .Where(k => k.StartsWith("feature_", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(k => k)
                        .ToList();

                    foreach (var key in featureKeys)
                    {
                        var featureValue = product.Metadata[key];
                        if (!string.IsNullOrEmpty(featureValue))
                        {
                            features.Add(featureValue);
                        }
                    }
                }

                // If no features found in metadata, check for default features based on product name/description
                if (features.Count == 0)
                {
                    features = GetDefaultFeaturesForProduct(product);
                }

                _logger.LogInformation("Retrieved {FeatureCount} features for product {ProductId}", features.Count, productId);
                return features.Distinct().ToList();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while retrieving product {ProductId}: {Message}", productId, ex.Message);
                // Return default features when Stripe API fails, using product name for better defaults
                var mockProduct = new Product 
                { 
                    Id = productId, 
                    Name = productName ?? "Default Plan", 
                    Description = productDescription ?? "A default subscription plan" 
                };
                return GetDefaultFeaturesForProduct(mockProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving features for product {ProductId}", productId);
                // Return default features when any error occurs, using product name for better defaults
                var mockProduct = new Product 
                { 
                    Id = productId, 
                    Name = productName ?? "Default Plan", 
                    Description = productDescription ?? "A default subscription plan" 
                };
                return GetDefaultFeaturesForProduct(mockProduct);
            }
        }

        /// <summary>
        /// Provides default features when the product ID is empty or when Stripe API calls fail.
        /// Uses a mock product with basic information for feature generation.
        /// </summary>
        /// <param name="productId">The product ID (can be empty)</param>
        /// <returns>List of default features</returns>
        private List<string> GetDefaultFeaturesBasedOnProductId(string productId)
        {
            // Create a mock product for default feature generation
            var mockProduct = new Product 
            { 
                Id = productId ?? "unknown", 
                Name = "Default Plan", 
                Description = "A default subscription plan" 
            };
            return GetDefaultFeaturesForProduct(mockProduct);
        }

        /// <summary>
        /// Gets a valid, non-null, non-empty label from Stripe data, with fallbacks to ensure a value is always returned.
        /// </summary>
        /// <param name="nickname">The Stripe price nickname</param>
        /// <param name="productName">The Stripe product name</param>
        /// <returns>A guaranteed non-null, non-empty label</returns>
        private string GetValidLabel(string? nickname, string? productName)
        {
            // First, try the nickname if it's not null or empty
            if (!string.IsNullOrWhiteSpace(nickname))
            {
                return nickname.Trim();
            }

            // If nickname is null/empty, extract from product name
            return ExtractLabelFromProductName(productName);
        }

        /// <summary>
        /// Extracts a short label from the product name for display purposes.
        /// </summary>
        /// <param name="productName">The full product name</param>
        /// <returns>A guaranteed non-null, non-empty label extracted from the product name</returns>
        private string ExtractLabelFromProductName(string? productName)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                return "Plan";
            }

            var name = productName.Trim().ToLowerInvariant();
            
            if (name.Contains("basic") || name.Contains("starter"))
                return "Basic";
            else if (name.Contains("professional") || name.Contains("pro"))
                return "Pro";
            else if (name.Contains("premium"))
                return "Premium";
            else if (name.Contains("enterprise") || name.Contains("business"))
                return "Enterprise";
            else
            {
                // Use first word, but ensure it's not empty
                var firstWord = productName.Trim().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                return !string.IsNullOrWhiteSpace(firstWord) ? firstWord : "Plan";
            }
        }

        /// <summary>
        /// Calculates the duration in months based on the recurring interval.
        /// </summary>
        /// <param name="priceDto">The price DTO containing interval information</param>
        /// <returns>Duration in months</returns>
        private int CalculateDurationInMonths(PriceDto priceDto)
        {
            if (!priceDto.IsRecurring)
            {
                return 0; // One-time payment
            }

            var intervalCount = priceDto.RecurringIntervalCount ?? 1;
            var interval = priceDto.RecurringInterval?.ToLowerInvariant() ?? "month";

            return interval switch
            {
                "day" => Math.Max(1, (int)Math.Ceiling(intervalCount / 30.0)), // Convert days to months
                "week" => Math.Max(1, (int)Math.Ceiling(intervalCount / 4.0)), // Convert weeks to months
                "month" => (int)intervalCount,
                "year" => (int)(intervalCount * 12),
                _ => 1 // Default to 1 month
            };
        }

        /// <summary>
        /// Provides default features for products when no features are defined in Stripe metadata.
        /// </summary>
        /// <param name="product">The Stripe product</param>
        /// <returns>List of default features</returns>
        private List<string> GetDefaultFeaturesForProduct(Product product)
        {
            var features = new List<string>();
            var name = product.Name?.ToLowerInvariant() ?? "";
            var description = product.Description?.ToLowerInvariant() ?? "";

            // Basic tier detection based on common naming patterns
            if (name.Contains("basic") || name.Contains("starter") || name.Contains("free"))
            {
                features.AddRange(new[]
                {
                    "Basic author profile",
                    "Single book listing",
                    "Contact form",
                    "Basic social media links"
                });
            }
            else if (name.Contains("professional") || name.Contains("pro") || name.Contains("premium"))
            {
                features.AddRange(new[]
                {
                    "Professional author profile",
                    "Unlimited book listings",
                    "Custom domain support",
                    "Advanced social media integration",
                    "Analytics dashboard",
                    "Custom themes",
                    "Priority support"
                });
            }
            else if (name.Contains("enterprise") || name.Contains("business"))
            {
                features.AddRange(new[]
                {
                    "Enterprise author profile",
                    "Unlimited everything",
                    "Custom domain with SSL",
                    "Full social media suite",
                    "Advanced analytics",
                    "Custom branding",
                    "API access",
                    "24/7 support",
                    "Custom integrations"
                });
            }
            else
            {
                // Default features for unknown products
                features.AddRange(new[]
                {
                    "Author profile",
                    "Book listings",
                    "Contact information",
                    "Social media links"
                });
            }

            return features;
        }
    }
}