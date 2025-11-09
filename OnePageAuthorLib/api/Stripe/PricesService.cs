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

                if (request?.IncludeProductDetails == true)
                {
                    options.Expand = new List<string> { "data.product" };
                }

                var stripeResponse = await service.ListAsync(options);

                // Map Stripe prices to DTOs
                var mappedPrices = stripeResponse.Data
                    .Select(MapStripePriceToDto)
                    .Where(p => p is not null)
                    .Cast<PriceDto>();

                // Apply LINQ filtering based on request parameters
                var filteredPrices = ApplyFilters(mappedPrices, request).ToList();

                var response = new PriceListResponse
                {
                    Prices = filteredPrices,
                    HasMore = stripeResponse.HasMore,
                    LastId = filteredPrices.LastOrDefault()?.Id ?? string.Empty
                };

                _logger.LogInformation("Retrieved {Count} Stripe prices (filtered from {TotalCount})", 
                    response.Prices.Count, stripeResponse.Data.Count);
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
                prices = prices.Where(p => p.Active == request.Active.Value);
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
