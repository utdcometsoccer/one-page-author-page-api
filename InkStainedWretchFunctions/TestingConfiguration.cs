using Microsoft.Extensions.Configuration;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing
{
    /// <summary>
    /// Configuration class for managing testing scenarios and feature flags
    /// </summary>
    public class TestingConfiguration
    {
        private readonly IConfiguration _configuration;

        public TestingConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Determines if the system is in testing mode
        /// </summary>
        public bool IsTestingMode => bool.Parse(_configuration["TESTING_MODE"] ?? "false");

        /// <summary>
        /// Mock all Azure infrastructure operations (DNS, Front Door, Domain Registration)
        /// </summary>
        public bool MockAzureInfrastructure => bool.Parse(_configuration["MOCK_AZURE_INFRASTRUCTURE"] ?? "false");

        /// <summary>
        /// Mock Google Domains API calls
        /// </summary>
        public bool MockGoogleDomains => bool.Parse(_configuration["MOCK_GOOGLE_DOMAINS"] ?? "false");

        /// <summary>
        /// Mock Stripe payment operations
        /// </summary>
        public bool MockStripePayments => bool.Parse(_configuration["MOCK_STRIPE_PAYMENTS"] ?? "false");

        /// <summary>
        /// Use Stripe test mode (when not mocking)
        /// </summary>
        public bool UseStripeTestMode => bool.Parse(_configuration["STRIPE_TEST_MODE"] ?? "true");

        /// <summary>
        /// Mock external API calls (Amazon, Penguin Random House)
        /// </summary>
        public bool MockExternalApis => bool.Parse(_configuration["MOCK_EXTERNAL_APIS"] ?? "false");

        /// <summary>
        /// Enable detailed test logging
        /// </summary>
        public bool EnableTestLogging => bool.Parse(_configuration["ENABLE_TEST_LOGGING"] ?? "false");

        /// <summary>
        /// Test scenario identifier for tracking different test runs
        /// </summary>
        public string TestScenario => _configuration["TEST_SCENARIO"] ?? "default";

        /// <summary>
        /// Maximum cost limit for testing operations (in USD)
        /// </summary>
        public decimal MaxTestCostLimit => decimal.Parse(_configuration["MAX_TEST_COST_LIMIT"] ?? "50.00");

        /// <summary>
        /// Test domain suffix to use for testing (e.g., "test.example.com")
        /// </summary>
        public string TestDomainSuffix => _configuration["TEST_DOMAIN_SUFFIX"] ?? "test-domain.local";

        /// <summary>
        /// Whether to skip actual domain purchases during testing
        /// </summary>
        public bool SkipDomainPurchase => bool.Parse(_configuration["SKIP_DOMAIN_PURCHASE"] ?? "true");
    }
}