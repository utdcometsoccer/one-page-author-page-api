using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing.Mocks
{
    /// <summary>
    /// Mock implementation of IFrontDoorService for testing scenarios
    /// </summary>
    public class MockFrontDoorService : IFrontDoorService
    {
        private readonly ILogger<MockFrontDoorService> _logger;
        private readonly TestingConfiguration _testConfig;

        public MockFrontDoorService(ILogger<MockFrontDoorService> logger, TestingConfiguration testConfig)
        {
            _logger = logger;
            _testConfig = testConfig;
        }

        public async Task<bool> AddDomainToFrontDoorAsync(DomainRegistration domainRegistration)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] AddDomainToFrontDoorAsync called for domain: {DomainName} in scenario: {TestScenario}", 
                    domainRegistration.Domain?.FullDomainName, _testConfig.TestScenario);
            }

            // Simulate processing time
            await Task.Delay(100);

            // Simulate different outcomes based on test scenario
            switch (_testConfig.TestScenario.ToLower())
            {
                case "failure":
                    _logger.LogWarning("[MOCK] Simulating Front Door addition failure");
                    return false;
                case "timeout":
                    await Task.Delay(5000); // Simulate timeout
                    return false;
                default:
                    _logger.LogInformation("[MOCK] Successfully added domain {DomainName} to Front Door", 
                        domainRegistration.Domain?.FullDomainName);
                    return true;
            }
        }

        public async Task<bool> RemoveDomainFromFrontDoorAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] RemoveDomainFromFrontDoorAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(100);
            _logger.LogInformation("[MOCK] Successfully removed domain {DomainName} from Front Door", domainName);
            return true;
        }

        public async Task<bool> ValidateDomainAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] ValidateDomainAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(50);
            return true;
        }

        public async Task<bool> DomainExistsAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] DomainExistsAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(50);
            
            // Simulate domain existence based on test scenario
            switch (_testConfig.TestScenario.ToLower())
            {
                case "domain-exists":
                    return true;
                case "domain-not-exists":
                    return false;
                default:
                    // For most tests, assume domain doesn't exist initially
                    return false;
            }
        }
    }
}