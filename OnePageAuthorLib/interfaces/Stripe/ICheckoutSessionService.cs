using System.Threading.Tasks;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface ICheckoutSessionService
    {
        Task<CreateCheckoutSessionResponse> CreateAsync(CreateCheckoutSessionRequest request);
        Task<GetCheckoutSessionResponse?> GetAsync(string checkoutSessionId);
    }
}
