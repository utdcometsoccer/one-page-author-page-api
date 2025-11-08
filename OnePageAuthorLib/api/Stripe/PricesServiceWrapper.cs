using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public class PricesServiceWrapper : IPriceServiceWrapper
    {
        private readonly IPriceService _inner;
        private readonly ISubscriptionPlanService _subscriptionPlanService;
        private readonly ILogger<PricesServiceWrapper> _logger;

        public PricesServiceWrapper(IPriceService inner, ISubscriptionPlanService subscriptionPlanService, ILogger<PricesServiceWrapper> logger)
        {
            _inner = inner;
            _subscriptionPlanService = subscriptionPlanService;
            _logger = logger;
        }

        public async Task<SubscriptionPlanListResponse> GetPricesAsync(PriceListRequest request)
        {
            _logger.LogDebug("Wrapper forwarding GetPricesAsync with culture {Culture}", request.Culture ?? "default");
            var response = await _inner.GetPricesAsync(request);
            var plans = await _subscriptionPlanService.MapToSubscriptionPlansAsync(response.Prices, request.Culture);
            return new SubscriptionPlanListResponse
            {
                LastId = response.LastId,
                HasMore = response.HasMore,
                Plans = plans
            };
        }

        public async Task<SubscriptionPlan?> GetPriceByIdAsync(string priceId, string? culture = null)
        {
            _logger.LogDebug("Wrapper forwarding GetPriceByIdAsync for {PriceId} with culture {Culture}", priceId, culture ?? "default");
            var priceDto = await _inner.GetPriceByIdAsync(priceId);
            if (priceDto == null)
            {
                return null;
            }
            return await _subscriptionPlanService.MapToSubscriptionPlanAsync(priceDto, culture);
        }
    }
}
