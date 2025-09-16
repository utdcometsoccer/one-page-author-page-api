using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class PricesServiceWrapper : IPriceServiceWrapper
    {
        private readonly IPriceService _inner;
        private readonly ILogger<PricesServiceWrapper> _logger;

        public PricesServiceWrapper(IPriceService inner, ILogger<PricesServiceWrapper> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<SubscriptionPlanListResponse> GetPricesAsync(PriceListRequest request)
        {
            _logger.LogDebug("Wrapper forwarding GetPricesAsync");
            var response = await _inner.GetPricesAsync(request);
            return new SubscriptionPlanListResponse
            {
                LastId = response.LastId,
                HasMore = response.HasMore,
                Plans = response.Prices.Select(price => Utility.MapToSubscriptionPlan(price)).ToList()
            };
        }

        public async Task<SubscriptionPlan?> GetPriceByIdAsync(string priceId)
        {
            _logger.LogDebug("Wrapper forwarding GetPriceByIdAsync for {PriceId}", priceId);
            return await _inner.GetPriceByIdAsync(priceId) switch
            {
                PriceDto dto => Utility.MapToSubscriptionPlan(dto),
                null => null
            };
        }
    }
}
