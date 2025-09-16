using System;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    [Obsolete("Use InkStainedWretch.OnePageAuthorLib.API.Stripe.IPriceService in interfaces/Stripe instead.")]
    public interface IStripePriceService
    {
        Task<PriceListResponse> GetPricesAsync(PriceListRequest request);
        Task<PriceDto?> GetPriceByIdAsync(string priceId);
    }
}