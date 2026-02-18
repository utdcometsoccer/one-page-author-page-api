using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing.Mocks
{
    /// <summary>
    /// Mock implementation of IDnsZoneService for testing scenarios
    /// </summary>
    public class MockDnsZoneService : IDnsZoneService
    {
        private readonly ILogger<MockDnsZoneService> _logger;
        private readonly TestingConfiguration _testConfig;

        public MockDnsZoneService(ILogger<MockDnsZoneService> logger, TestingConfiguration testConfig)
        {
            _logger = logger;
            _testConfig = testConfig;
        }

        public async Task<bool> CreateDnsZoneAsync(DomainRegistration domainRegistration)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] CreateDnsZoneAsync called for domain: {DomainName} in scenario: {TestScenario}", 
                    domainRegistration.Domain?.FullDomainName, _testConfig.TestScenario);
            }

            // Simulate processing time
            await Task.Delay(200);

            // Simulate different outcomes based on test scenario
            switch (_testConfig.TestScenario.ToLower())
            {
                case "dns-failure":
                    _logger.LogWarning("[MOCK] Simulating DNS zone creation failure");
                    return false;
                case "dns-timeout":
                    await Task.Delay(10000); // Simulate timeout
                    return false;
                default:
                    _logger.LogInformation("[MOCK] Successfully created DNS zone for domain {DomainName}", 
                        domainRegistration.Domain?.FullDomainName);
                    
                    // Log estimated cost for testing
                    var estimatedCost = 0.50m; // Azure DNS zone cost
                    _logger.LogInformation("[MOCK] Estimated cost for DNS zone: ${Cost:F2}", estimatedCost);
                    
                    return true;
            }
        }

        public async Task<bool> DeleteDnsZoneAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] DeleteDnsZoneAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(100);
            _logger.LogInformation("[MOCK] Successfully deleted DNS zone for domain {DomainName}", domainName);
            return true;
        }

        public async Task<bool> DnsZoneExistsAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] DnsZoneExistsAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(50);
            
            // Simulate DNS zone existence based on test scenario
            switch (_testConfig.TestScenario.ToLower())
            {
                case "dns-exists":
                    return true;
                case "dns-not-exists":
                    return false;
                default:
                    // For most tests, assume DNS zone doesn't exist initially
                    return false;
            }
        }

        public async Task<string[]?> GetNameServersAsync(string domainName)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] GetNameServersAsync called for domain: {DomainName}", domainName);
            }

            await Task.Delay(100);
            
            // Return mock name servers
            return new[]
            {
                "ns1-test.azure-dns.com",
                "ns2-test.azure-dns.net",
                "ns3-test.azure-dns.org",
                "ns4-test.azure-dns.info"
            };
        }

        public async Task<bool> EnsureDnsZoneExistsAsync(DomainRegistration domainRegistration)
        {
            if (_testConfig.EnableTestLogging)
            {
                _logger.LogInformation("[MOCK] EnsureDnsZoneExistsAsync called for domain: {DomainName}", 
                    domainRegistration.Domain?.FullDomainName);
            }

            // Check if DNS zone exists first
            var exists = await DnsZoneExistsAsync(domainRegistration.Domain?.FullDomainName ?? "");
            
            if (!exists)
            {
                // Create DNS zone if it doesn't exist
                return await CreateDnsZoneAsync(domainRegistration);
            }

            _logger.LogInformation("[MOCK] DNS zone already exists for domain {DomainName}", 
                domainRegistration.Domain?.FullDomainName);
            return true;
        }
    }
}