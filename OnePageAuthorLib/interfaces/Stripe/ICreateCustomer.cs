using InkStainedWretch.OnePageAuthorLib.Entities.Stripe;

namespace InkStainedWretch.OnePageAuthorLib.API.Stripe
{
    public interface ICreateCustomer
    {
        CreateCustomerResponse Execute(CreateCustomerRequest request);
    }
}
