using Microsoft.Extensions.DependencyInjection;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using InkStainedWretch.OnePageAuthorAPI.API;

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
            
            services.AddDomainRegistrationServices();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var domainService = serviceProvider.GetRequiredService<IDomainRegistrationService>();

            // Assert - Should be able to resolve successfully (which means all dependencies were injected)
            Assert.NotNull(domainService);
            Assert.IsType<DomainRegistrationService>(domainService);
        }
    }
}