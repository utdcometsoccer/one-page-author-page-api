using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing.Mocks
{
    /// <summary>
    /// Mock implementation of IGoogleDomainsService for testing scenarios
    /// </summary>
    public class MockGoogleDomainsService : IGoogleDomainsService
    {
        private readonly ILogger<MockGoogleDomainsService> _logger;
        private readonly TestingConfiguration _testConfig;

        public MockGoogleDomainsService(ILogger<MockGoogleDomainsService> logger, TestingConfiguration testConfig)
        {
            _logger = logger;
            _testConfig = testConfig;
        }

        public async Task<bool> RegisterDomainAsync(DomainRegistration domainRegistration)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] RegisterDomainAsync called for domain: {DomainName} in scenario: {TestScenario}", 
                    domainRegistration.Domain?.FullDomainName, _testConfig.TestScenario);
            }

            // Check if we should skip domain purchase
            if (_testConfig.SkipDomainPurchase)
            {
                _logger.LogInformation("[MOCK] Skipping actual domain purchase due to SKIP_DOMAIN_PURCHASE setting");
                await Task.Delay(500); // Simulate processing time
                
                _logger.LogInformation("[MOCK] Successfully 'registered' domain {DomainName} (mocked)", 
                    domainRegistration.Domain?.FullDomainName);
                return true;
            }

            // Simulate processing time for domain registration
            await Task.Delay(1000);

            // Simulate different outcomes based on test scenario
            switch (_testConfig.TestScenario.ToLower())
            {
                case "domain-registration-failure":
                    _logger.LogWarning("[MOCK] Simulating domain registration failure");
                    return false;
                case "domain-unavailable":
                    _logger.LogWarning("[MOCK] Simulating domain unavailable");
                    return false;
                case "payment-failure":
                    _logger.LogWarning("[MOCK] Simulating payment failure during domain registration");
                    return false;
                default:
                    // Estimate domain cost (varies by TLD)
                    var domainName = domainRegistration.Domain?.FullDomainName ?? "";
                    var estimatedCost = EstimateDomainCost(domainName);
                    
                    _logger.LogInformation("[MOCK] Successfully registered domain {DomainName}. Estimated cost: ${Cost:F2}", 
                        domainName, estimatedCost);
                    
                    // Check if cost exceeds test limit
                    if (estimatedCost > _testConfig.MaxTestCostLimit)
                    {
                        _logger.LogWarning("[MOCK] Domain cost ${Cost:F2} exceeds test limit ${Limit:F2}", 
                            estimatedCost, _testConfig.MaxTestCostLimit);
                    }
                    
                    return true;
            }
        }

        public async Task<bool> IsDomainAvailableAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] IsDomainAvailableAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(200);

            // Simulate availability based on test scenario
            switch (_testConfig.TestScenario.ToLower())
            {
                case "domain-unavailable":
                case "domain-exists":
                    return false;
                case "domain-available":
                case "domain-not-exists":
                    return true;
                default:
                    // Most test domains should be available
                    return true;
            }
        }

        private decimal EstimateDomainCost(string domainName)
        {
            // Simulate domain pricing based on TLD
            var tld = domainName.Split('.').LastOrDefault()?.ToLower();
            
            return tld switch
            {
                "com" => 12.00m,
                "net" => 12.00m,
                "org" => 12.00m,
                "io" => 35.00m,
                "dev" => 12.00m,
                "app" => 20.00m,
                _ => 15.00m // Default price
            };
        }
    }
}