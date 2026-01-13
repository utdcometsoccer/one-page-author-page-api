using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.API;
using InkStainedWretch.OnePageAuthorLib.Interfaces.Stripe;

namespace OnePageAuthor.Test.Integration
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void ServiceFactory_RegistersUserIdentityService_Successfully()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add logging (required by DomainRegistrationService)
            services.AddLogging();
            
            // Add the domain registration services (which should include IUserIdentityService)
            services.AddDomainRegistrationServices();
            
            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var userIdentityService = serviceProvider.GetService<IUserIdentityService>();

            // Assert
            Assert.NotNull(userIdentityService);
            Assert.IsType<UserIdentityService>(userIdentityService);
        }

        [Fact]
        public void ServiceFactory_CanResolveUserIdentityService_AsScoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDomainRegistrationServices();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act - Create two scopes and get services from each
            using var scope1 = serviceProvider.CreateScope();
            using var scope2 = serviceProvider.CreateScope();
            
            var service1a = scope1.ServiceProvider.GetRequiredService<IUserIdentityService>();
            var service1b = scope1.ServiceProvider.GetRequiredService<IUserIdentityService>();
            var service2 = scope2.ServiceProvider.GetRequiredService<IUserIdentityService>();

            // Assert - Same instance within scope, different instances across scopes
            Assert.Same(service1a, service1b); // Same instance within the same scope
            Assert.NotSame(service1a, service2); // Different instances across scopes
        }

        [Fact]
        public void ServiceFactory_DomainRegistrationService_ReceivesUserIdentityService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            
            // Mock the repository dependency
            var mockRepo = new Moq.Mock<IDomainRegistrationRepository>();
            services.AddSingleton(mockRepo.Object);
            
            // Mock the subscription validation service dependency
            var mockSubscriptionValidationService = new Moq.Mock<ISubscriptionValidationService>();
            services.AddSingleton(mockSubscriptionValidationService.Object);
            
            // Mock user profile repository (needed by SubscriptionValidationService)
            var mockUserProfileRepo = new Moq.Mock<IUserProfileRepository>();
            services.AddSingleton(mockUserProfileRepo.Object);
            
            services.AddDomainRegistrationServices();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var domainService = serviceProvider.GetRequiredService<IDomainRegistrationService>();

            // Assert - Should be able to resolve successfully (which means all dependencies were injected)
            Assert.NotNull(domainService);
            Assert.IsType<DomainRegistrationService>(domainService);
        }

        [Fact]
        public void ServiceFactory_RegistersNoOpSubscriptionValidationService_WithoutStripe()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            
            // Add domain registration services only (no Stripe services)
            services.AddDomainRegistrationServices();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var subscriptionValidationService = serviceProvider.GetRequiredService<ISubscriptionValidationService>();

            // Assert - Should resolve to NoOp implementation when Stripe is not configured
            Assert.NotNull(subscriptionValidationService);
            Assert.IsType<InkStainedWretch.OnePageAuthorLib.API.Stripe.NoOpSubscriptionValidationService>(subscriptionValidationService);
        }

        [Fact]
        public void ServiceFactory_DomainRegistrationService_CanBeResolved_WithoutStripe()
        {
            // Arrange - Simulate the configuration without Stripe API key
            var services = new ServiceCollection();
            services.AddLogging();
            
            // Mock the repository dependency
            var mockRepo = new Moq.Mock<IDomainRegistrationRepository>();
            services.AddSingleton(mockRepo.Object);
            
            // Add domain registration services (which includes NoOp subscription validation)
            services.AddDomainRegistrationServices();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act - Should not throw InvalidOperationException
            var domainService = serviceProvider.GetRequiredService<IDomainRegistrationService>();

            // Assert - Should be able to resolve successfully
            Assert.NotNull(domainService);
            Assert.IsType<DomainRegistrationService>(domainService);
        }
    }
}