using System.Threading.Tasks;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IPriceService
    {
        Task<PriceListResponse> GetPricesAsync(PriceListRequest request);
        Task<PriceDto?> GetPriceByIdAsync(string priceId);
    }
}
