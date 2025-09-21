using System.Security.Claims;
using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface IEnsureCustomerForUser
    {
        Task<CreateCustomerResponse> EnsureAsync(ClaimsPrincipal user, CreateCustomerRequest request);
    }
}
