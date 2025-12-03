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
        private readonly StripeClient _stripeClient;

        public SubscriptionPlanService(ILogger<SubscriptionPlanService> logger, StripeClient stripeClient)
        {
            _logger = logger;
            _stripeClient = stripeClient ?? throw new ArgumentNullException(nameof(stripeClient));
        }

        public async Task<SubscriptionPlan> MapToSubscriptionPlanAsync(PriceDto priceDto, string? culture = null)
        {
            if (priceDto == null)
            {
                throw new ArgumentNullException(nameof(priceDto));
            }

            _logger.LogInformation("Mapping PriceDto to SubscriptionPlan for product {ProductId} with culture {Culture}", priceDto.ProductId, culture ?? "default");

            var features = await GetProductFeaturesAsync(priceDto.ProductId, priceDto.ProductName, priceDto.ProductDescription);
            var (localizedLabel, localizedName, localizedDescription) = await GetLocalizedContentAsync(priceDto.ProductId, priceDto.Nickname, priceDto.ProductName, priceDto.ProductDescription, culture);

            var plan = new SubscriptionPlan
            {
                Id = priceDto.Id,
                StripePriceId = priceDto.Id,
                Label = GetValidLabel(localizedLabel, localizedName),
                Name = localizedName,
                Description = localizedDescription,
                Price = priceDto.AmountDecimal,
                Currency = priceDto.Currency.ToUpperInvariant(),
                Duration = CalculateDurationInMonths(priceDto),
                Features = features
            };

            _logger.LogInformation("Successfully mapped SubscriptionPlan with {FeatureCount} features for culture {Culture}", features.Count, culture ?? "default");
            return plan;
        }

        public async Task<List<SubscriptionPlan>> MapToSubscriptionPlansAsync(IEnumerable<PriceDto> priceDtos, string? culture = null)
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
                    var plan = await MapToSubscriptionPlanAsync(priceDto, culture);
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

                var productService = new ProductService(_stripeClient);
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

        /// <summary>
        /// Gets localized content for a product from Stripe metadata based on culture.
        /// Falls back to default/original content if culture is not provided or localized content is not found.
        /// </summary>
        /// <param name="productId">The Stripe product ID</param>
        /// <param name="originalNickname">The original price nickname</param>
        /// <param name="originalName">The original product name</param>
        /// <param name="originalDescription">The original product description</param>
        /// <param name="culture">The culture code (e.g., "es-US", "fr-CA")</param>
        /// <returns>Tuple containing localized nickname, name, and description</returns>
        private async Task<(string nickname, string name, string description)> GetLocalizedContentAsync(string productId, string originalNickname, string originalName, string originalDescription, string? culture)
        {
            // If no culture specified, return original content
            if (string.IsNullOrEmpty(culture))
            {
                _logger.LogDebug("No culture specified, using original content for product {ProductId}", productId);
                return (originalNickname, originalName, originalDescription);
            }

            try
            {
                var productService = new ProductService(_stripeClient);
                var product = await productService.GetAsync(productId);

                if (product?.Metadata == null)
                {
                    _logger.LogDebug("No metadata found for product {ProductId}, using original content", productId);
                    return (originalNickname, originalName, originalDescription);
                }

                // Normalize culture code to handle different formats
                var normalizedCulture = NormalizeCultureCode(culture);
                _logger.LogDebug("Looking for localized content for product {ProductId} in culture {Culture}", productId, normalizedCulture);

                // Look for culture-specific localized content in metadata
                string localizedNickname = originalNickname;
                string localizedName = originalName;
                string localizedDescription = originalDescription;

                // Check for localized nickname in metadata
                var nicknameKey = GetCultureMetadataKey(product.Metadata, normalizedCulture, "localized_nickname");
                if (!string.IsNullOrEmpty(nicknameKey) && product.Metadata.ContainsKey(nicknameKey))
                {
                    var metadataNickname = product.Metadata[nicknameKey];
                    if (!string.IsNullOrEmpty(metadataNickname))
                    {
                        localizedNickname = metadataNickname;
                        _logger.LogDebug("Found localized nickname for {Culture}: {Nickname}", normalizedCulture, localizedNickname);
                    }
                }

                // Check for localized name in metadata
                var nameKey = GetCultureMetadataKey(product.Metadata, normalizedCulture, "localized_name");
                if (!string.IsNullOrEmpty(nameKey) && product.Metadata.ContainsKey(nameKey))
                {
                    var metadataName = product.Metadata[nameKey];
                    if (!string.IsNullOrEmpty(metadataName))
                    {
                        localizedName = metadataName;
                        _logger.LogDebug("Found localized name for {Culture}: {Name}", normalizedCulture, localizedName);
                    }
                }

                // For description, we can fallback to a general localized description if available
                // This would be stored in our StripeProductManager metadata
                var descriptionFromMetadata = GetLocalizedDescriptionFromMetadata(product.Metadata, normalizedCulture);
                if (!string.IsNullOrEmpty(descriptionFromMetadata))
                {
                    localizedDescription = descriptionFromMetadata;
                    _logger.LogDebug("Found localized description for {Culture}", normalizedCulture);
                }

                return (localizedNickname, localizedName, localizedDescription);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving localized content for product {ProductId} culture {Culture}, using original content", productId, culture);
                return (originalNickname, originalName, originalDescription);
            }
        }

        /// <summary>
        /// Normalizes culture code to a consistent format (e.g., "en-US")
        /// </summary>
        private string NormalizeCultureCode(string culture)
        {
            if (string.IsNullOrEmpty(culture))
                return "en-US"; // Default culture

            // Convert to lowercase and handle common variations
            var normalized = culture.Trim().ToLowerInvariant();
            
            // Map common culture codes
            return normalized switch
            {
                "en" or "english" => "en-US",
                "es" or "spanish" => "es-US", 
                "fr" or "french" => "fr-CA",
                "en-us" => "en-US",
                "en-ca" => "en-CA",
                "es-us" => "es-US",
                "es-mx" => "es-MX",
                "fr-ca" => "fr-CA",
                "fr-fr" => "fr-FR",
                _ => culture // Return as-is if not recognized
            };
        }

        /// <summary>
        /// Gets the metadata key for a specific culture and content type
        /// </summary>
        private string GetCultureMetadataKey(IDictionary<string, string> metadata, string culture, string contentType)
        {
            // Look for culture-specific keys in metadata
            // Format: culture_01_localized_name, culture_02_localized_nickname, etc.
            var cultureKeys = metadata.Keys
                .Where(k => k.Contains("_code") && metadata[k].Equals(culture, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var cultureKey in cultureKeys)
            {
                // Extract culture index (e.g., "01" from "culture_01_code")
                var parts = cultureKey.Split('_');
                if (parts.Length >= 2)
                {
                    var cultureIndex = parts[1];
                    var targetKey = $"culture_{cultureIndex}_{contentType}";
                    
                    if (metadata.ContainsKey(targetKey))
                    {
                        return targetKey;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets localized description from metadata if available
        /// </summary>
        private string GetLocalizedDescriptionFromMetadata(IDictionary<string, string> metadata, string culture)
        {
            // For now, we'll return the original description since the StripeProductManager
            // stores localized descriptions in the configuration, not directly in Stripe metadata
            // This could be enhanced in the future if needed
            return string.Empty;
        }
    }
}