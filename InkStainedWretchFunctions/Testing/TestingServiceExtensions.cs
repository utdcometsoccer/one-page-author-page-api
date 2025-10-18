using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI.Functions.Testing.Mocks;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing
{
    /// <summary>
    /// Extension methods for registering testing services and mock implementations
    /// </summary>
    public static class TestingServiceExtensions
    {
        /// <summary>
        /// Registers testing services and conditionally replaces production services with mocks
        /// based on configuration flags
        /// </summary>
        public static IServiceCollection AddTestingServices(this IServiceCollection services)
        {
            // Register the testing configuration
            services.AddSingleton<TestingConfiguration>();

            // Register testing services conditionally based on configuration
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var testingConfig = new TestingConfiguration(configuration);

            // Replace services with mocks when testing flags are enabled
            if (testingConfig.MockAzureInfrastructure)
            {
                // Replace Azure infrastructure services with mocks
                services.AddScoped<IFrontDoorService, MockFrontDoorService>();
                services.AddScoped<IDnsZoneService, MockDnsZoneService>();
            }

            if (testingConfig.MockGoogleDomains)
            {
                // Replace Google Domains service with mock
                services.AddScoped<IGoogleDomainsService, MockGoogleDomainsService>();
            }

            // Note: For external APIs (Amazon, Penguin), mock implementations would be added here
            // if (testingConfig.MockExternalApis)
            // {
            //     services.AddScoped<IAmazonProductService, MockAmazonProductService>();
            //     services.AddScoped<IPenguinRandomHouseService, MockPenguinRandomHouseService>();
            // }

            return services;
        }
    }
}