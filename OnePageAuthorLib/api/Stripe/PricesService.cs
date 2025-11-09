using Microsoft.Extensions.Logging;
using Stripe;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class PricesService : IPriceService
    {
        private readonly ILogger<PricesService> _logger;

        public PricesService(ILogger<PricesService> logger)
        {
            _logger = logger;
        }

        public async Task<PriceListResponse> GetPricesAsync(PriceListRequest request)
        {
            _logger.LogInformation("Retrieving Stripe prices with filters: Active={Active}, ProductId={ProductId}",
                request?.Active, request?.ProductId);

            try
            {
                var service = new PriceService();
                var options = new PriceListOptions
                {
                    // Pass Active filter to Stripe API for efficiency (reduces data transfer)
                    // We'll also apply LINQ filtering for additional control
                    Active = request?.Active,
                    Limit = request?.Limit ?? 100
                };

                var productId = request?.ProductId;
                if (!string.IsNullOrEmpty(productId))
                {
                    options.Product = productId;
                }

                var currency = request?.Currency;
                if (!string.IsNullOrEmpty(currency))
                {
                    options.Currency = currency;
                }

                // Always expand product details when Active filtering is requested
                // This is required to check both Price.Active and Product.Active status
                if (request?.IncludeProductDetails == true || request?.Active.HasValue == true)
                {
                    options.Expand = new List<string> { "data.product" };
                }

                var stripeResponse = await service.ListAsync(options);

                // Map Stripe prices to DTOs
                var mappedPrices = stripeResponse.Data
                    .Select(MapStripePriceToDto)
                    .Where(p => p is not null)
                    .Cast<PriceDto>();

                // Log debug info if filtering by Active status
                if (request?.Active.HasValue == true)
                {
                    var allPrices = mappedPrices.ToList();
                    var activePrices = allPrices.Count(p => p.Active);
                    var activeProducts = allPrices.Count(p => p.ProductActive);
                    var fullyActive = allPrices.Count(p => p.Active && p.ProductActive);
                    
                    _logger.LogDebug("Before filtering: Total={Total}, ActivePrices={ActivePrices}, ActiveProducts={ActiveProducts}, FullyActive={FullyActive}",
                        allPrices.Count, activePrices, activeProducts, fullyActive);
                }

                // Apply LINQ filtering based on request parameters
                var filteredPrices = ApplyFilters(mappedPrices, request).ToList();

                var response = new PriceListResponse
                {
                    Prices = filteredPrices,
                    HasMore = stripeResponse.HasMore,
                    // Use LastId from Stripe response for proper cursor-based pagination
                    LastId = stripeResponse.Data.LastOrDefault()?.Id ?? string.Empty
                };

                _logger.LogInformation("Retrieved {Count} Stripe prices (filtered from {TotalCount}) with Active filter: {ActiveFilter}", 
                    response.Prices.Count, stripeResponse.Data.Count, request?.Active?.ToString() ?? "null");
                return response;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while retrieving prices: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Stripe prices");
                throw;
            }
        }

        public async Task<PriceDto?> GetPriceByIdAsync(string priceId)
        {
            if (string.IsNullOrEmpty(priceId))
            {
                throw new ArgumentException("Price ID cannot be null or empty", nameof(priceId));
            }

            _logger.LogInformation("Retrieving Stripe price by ID: {PriceId}", priceId);

            try
            {
                var service = new PriceService();
                var options = new PriceGetOptions
                {
                    Expand = new List<string> { "product" }
                };

                var price = await service.GetAsync(priceId, options);
                return MapStripePriceToDto(price);
            }
            catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
            {
                _logger.LogWarning("Stripe price not found: {PriceId}", priceId);
                return null;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while retrieving price {PriceId}: {Message}",
                    priceId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Stripe price {PriceId}", priceId);
                throw;
            }
        }

        private IEnumerable<PriceDto> ApplyFilters(IEnumerable<PriceDto> prices, PriceListRequest? request)
        {
            if (request == null)
            {
                return prices;
            }

            // Apply LINQ filtering by Active status for complete control
            // Note: We also pass the Active filter to Stripe API for efficiency,
            // but LINQ filtering ensures we have explicit control over the results
            // and allows for additional filtering logic beyond what Stripe API supports
            if (request.Active.HasValue)
            {
                _logger.LogDebug("Applying LINQ filter for Active={Active}", request.Active.Value);
                
                if (request.Active.Value)
                {
                    // When filtering for active items, ensure BOTH price and product are active
                    // This is crucial for subscription plans - both price and product must be active
                    prices = prices.Where(p => p.Active && p.ProductActive);
                    _logger.LogDebug("Applied enhanced active filter: both Price.Active and Product.Active must be true");
                }
                else
                {
                    // When filtering for inactive items, show items where price is inactive
                    prices = prices.Where(p => p.Active == false);
                }
            }

            return prices;
        }

        private PriceDto? MapStripePriceToDto(Price? price)
        {
            if (price == null) return null;

            return new PriceDto
            {
                Id = price.Id,
                ProductId = price.ProductId ?? string.Empty,
                ProductName = price.Product?.Name ?? string.Empty,
                ProductDescription = price.Product?.Description ?? string.Empty,
                UnitAmount = price.UnitAmount,
                Currency = price.Currency ?? string.Empty,
                Active = price.Active,
                ProductActive = price.Product?.Active ?? false,
                Nickname = price.Nickname ?? string.Empty,
                LookupKey = price.LookupKey ?? string.Empty,
                Type = price.Type ?? string.Empty,
                IsRecurring = price.Recurring != null,
                RecurringInterval = price.Recurring?.Interval ?? string.Empty,
                RecurringIntervalCount = price.Recurring?.IntervalCount,
                CreatedDate = price.Created
            };
        }
    }
}
