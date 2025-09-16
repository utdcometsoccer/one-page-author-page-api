using InkStainedWretch.OnePageAuthorLib.API.Stripe;
using Microsoft.Extensions.DependencyInjection;

public static class StripeServiceFactory
{
    public static void AddStripeServices(this IServiceCollection services)
    {
        // Register Stripe services here
           services.AddTransient<ICreateCustomer, CreateCustomer>();
           services.AddScoped<IPriceService, PricesService>();
           services.AddScoped<IPriceServiceWrapper, PricesServiceWrapper>();
           services.AddScoped<ICheckoutSessionService, CheckoutSessionsService>();
           services.AddScoped<ISubscriptionService, SubscriptionsService>();
    }
}