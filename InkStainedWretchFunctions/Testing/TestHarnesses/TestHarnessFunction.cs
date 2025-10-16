using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing.TestHarnesses
{
    /// <summary>
    /// Test harness functions for individual component testing
    /// These functions allow you to test each major component independently
    /// </summary>
    public class TestHarnessFunction
    {
        private readonly ILogger<TestHarnessFunction> _logger;
        private readonly TestingConfiguration _testConfig;

        public TestHarnessFunction(ILogger<TestHarnessFunction> logger, TestingConfiguration testConfig)
        {
            _logger = logger;
            _testConfig = testConfig;
        }

        /// <summary>
        /// Test harness for Front Door operations
        /// POST /api/test/frontdoor
        /// Body: { "domainName": "test.example.com", "operation": "add|remove|validate|exists" }
        /// </summary>
        [Function("TestFrontDoor")]
        public async Task<IActionResult> TestFrontDoor(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/frontdoor")] HttpRequest req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var testRequest = JsonSerializer.Deserialize<TestRequest>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (testRequest == null || string.IsNullOrEmpty(testRequest.DomainName))
                {
                    return new BadRequestObjectResult("Missing domainName in request body");
                }

                _logger.LogInformation("[TEST HARNESS] Testing Front Door operation: {Operation} for domain: {DomainName}", 
                    testRequest.Operation, testRequest.DomainName);

                var result = new TestResult
                {
                    TestName = "FrontDoor",
                    DomainName = testRequest.DomainName,
                    Operation = testRequest.Operation,
                    TestScenario = _testConfig.TestScenario,
                    IsMocked = _testConfig.MockAzureInfrastructure,
                    Timestamp = DateTime.UtcNow
                };

                // Simulate the operation based on the request
                switch (testRequest.Operation?.ToLower())
                {
                    case "add":
                        result.Success = await SimulateFrontDoorAdd(testRequest.DomainName);
                        result.Message = result.Success ? "Domain added to Front Door" : "Failed to add domain";
                        result.EstimatedCost = _testConfig.MockAzureInfrastructure ? 0m : 0.10m; // Azure Front Door cost
                        break;
                    case "remove":
                        result.Success = await SimulateFrontDoorRemove(testRequest.DomainName);
                        result.Message = result.Success ? "Domain removed from Front Door" : "Failed to remove domain";
                        break;
                    case "validate":
                        result.Success = await SimulateFrontDoorValidate(testRequest.DomainName);
                        result.Message = result.Success ? "Domain validation successful" : "Domain validation failed";
                        break;
                    case "exists":
                        result.Success = await SimulateFrontDoorExists(testRequest.DomainName);
                        result.Message = result.Success ? "Domain exists in Front Door" : "Domain does not exist";
                        break;
                    default:
                        return new BadRequestObjectResult("Invalid operation. Use: add, remove, validate, or exists");
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST HARNESS] Error testing Front Door");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Test harness for DNS Zone operations
        /// POST /api/test/dns
        /// Body: { "domainName": "test.example.com", "operation": "create|delete|exists|nameservers" }
        /// </summary>
        [Function("TestDnsZone")]
        public async Task<IActionResult> TestDnsZone(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/dns")] HttpRequest req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var testRequest = JsonSerializer.Deserialize<TestRequest>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (testRequest == null || string.IsNullOrEmpty(testRequest.DomainName))
                {
                    return new BadRequestObjectResult("Missing domainName in request body");
                }

                _logger.LogInformation("[TEST HARNESS] Testing DNS Zone operation: {Operation} for domain: {DomainName}", 
                    testRequest.Operation, testRequest.DomainName);

                var result = new TestResult
                {
                    TestName = "DnsZone",
                    DomainName = testRequest.DomainName,
                    Operation = testRequest.Operation,
                    TestScenario = _testConfig.TestScenario,
                    IsMocked = _testConfig.MockAzureInfrastructure,
                    Timestamp = DateTime.UtcNow
                };

                switch (testRequest.Operation?.ToLower())
                {
                    case "create":
                        result.Success = await SimulateDnsZoneCreate(testRequest.DomainName);
                        result.Message = result.Success ? "DNS zone created" : "Failed to create DNS zone";
                        result.EstimatedCost = _testConfig.MockAzureInfrastructure ? 0m : 0.50m; // Azure DNS zone cost
                        break;
                    case "delete":
                        result.Success = await SimulateDnsZoneDelete(testRequest.DomainName);
                        result.Message = result.Success ? "DNS zone deleted" : "Failed to delete DNS zone";
                        break;
                    case "exists":
                        result.Success = await SimulateDnsZoneExists(testRequest.DomainName);
                        result.Message = result.Success ? "DNS zone exists" : "DNS zone does not exist";
                        break;
                    case "nameservers":
                        var nameServers = await SimulateDnsZoneGetNameServers(testRequest.DomainName);
                        result.Success = nameServers.Length > 0;
                        result.Message = result.Success ? $"Retrieved {nameServers.Length} name servers" : "Failed to get name servers";
                        result.Data = nameServers;
                        break;
                    default:
                        return new BadRequestObjectResult("Invalid operation. Use: create, delete, exists, or nameservers");
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST HARNESS] Error testing DNS Zone");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Test harness for Google Domains operations
        /// POST /api/test/googledomains
        /// Body: { "domainName": "test.example.com", "operation": "register|available" }
        /// </summary>
        [Function("TestGoogleDomains")]
        public async Task<IActionResult> TestGoogleDomains(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/googledomains")] HttpRequest req)
        {
            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var testRequest = JsonSerializer.Deserialize<TestRequest>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (testRequest == null || string.IsNullOrEmpty(testRequest.DomainName))
                {
                    return new BadRequestObjectResult("Missing domainName in request body");
                }

                _logger.LogInformation("[TEST HARNESS] Testing Google Domains operation: {Operation} for domain: {DomainName}", 
                    testRequest.Operation, testRequest.DomainName);

                var result = new TestResult
                {
                    TestName = "GoogleDomains",
                    DomainName = testRequest.DomainName,
                    Operation = testRequest.Operation,
                    TestScenario = _testConfig.TestScenario,
                    IsMocked = _testConfig.MockGoogleDomains,
                    Timestamp = DateTime.UtcNow
                };

                switch (testRequest.Operation?.ToLower())
                {
                    case "register":
                        if (!_testConfig.SkipDomainPurchase && !_testConfig.MockGoogleDomains)
                        {
                            result.EstimatedCost = EstimateDomainCost(testRequest.DomainName);
                            if (result.EstimatedCost > _testConfig.MaxTestCostLimit)
                            {
                                result.Success = false;
                                result.Message = $"Domain registration cost ${result.EstimatedCost:F2} exceeds test limit ${_testConfig.MaxTestCostLimit:F2}";
                                break;
                            }
                        }
                        
                        result.Success = await SimulateGoogleDomainsRegister(testRequest.DomainName);
                        result.Message = result.Success ? "Domain registration initiated" : "Failed to register domain";
                        break;
                    case "available":
                        result.Success = await SimulateGoogleDomainsAvailable(testRequest.DomainName);
                        result.Message = result.Success ? "Domain is available" : "Domain is not available";
                        break;
                    default:
                        return new BadRequestObjectResult("Invalid operation. Use: register or available");
                }

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST HARNESS] Error testing Google Domains");
                return new StatusCodeResult(500);
            }
        }

        // Private simulation methods
        private async Task<bool> SimulateFrontDoorAdd(string domainName)
        {
            await Task.Delay(100);
            return _testConfig.TestScenario != "failure";
        }

        private async Task<bool> SimulateFrontDoorRemove(string domainName)
        {
            await Task.Delay(100);
            return true;
        }

        private async Task<bool> SimulateFrontDoorValidate(string domainName)
        {
            await Task.Delay(200);
            return _testConfig.TestScenario != "failure";
        }

        private async Task<bool> SimulateFrontDoorExists(string domainName)
        {
            await Task.Delay(50);
            return _testConfig.TestScenario == "domain-exists";
        }

        private async Task<bool> SimulateDnsZoneCreate(string domainName)
        {
            await Task.Delay(300);
            return _testConfig.TestScenario != "dns-failure";
        }

        private async Task<bool> SimulateDnsZoneDelete(string domainName)
        {
            await Task.Delay(200);
            return true;
        }

        private async Task<bool> SimulateDnsZoneExists(string domainName)
        {
            await Task.Delay(50);
            return _testConfig.TestScenario == "dns-exists";
        }

        private async Task<string[]> SimulateDnsZoneGetNameServers(string domainName)
        {
            await Task.Delay(100);
            return new[] { "ns1-test.azure-dns.com", "ns2-test.azure-dns.net" };
        }

        private async Task<bool> SimulateGoogleDomainsRegister(string domainName)
        {
            await Task.Delay(1000);
            return _testConfig.TestScenario != "domain-registration-failure";
        }

        private async Task<bool> SimulateGoogleDomainsAvailable(string domainName)
        {
            await Task.Delay(200);
            return _testConfig.TestScenario != "domain-unavailable";
        }

        private decimal EstimateDomainCost(string domainName)
        {
            var tld = domainName.Split('.').LastOrDefault()?.ToLower();
            return tld switch
            {
                "com" => 12.00m,
                "net" => 12.00m,
                "org" => 12.00m,
                "io" => 35.00m,
                "dev" => 12.00m,
                "app" => 20.00m,
                _ => 15.00m
            };
        }
    }

    public class TestRequest
    {
        public string? DomainName { get; set; }
        public string? Operation { get; set; }
    }

    public class TestResult
    {
        public string TestName { get; set; } = "";
        public string DomainName { get; set; } = "";
        public string Operation { get; set; } = "";
        public string TestScenario { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public bool IsMocked { get; set; }
        public decimal EstimatedCost { get; set; }
        public DateTime Timestamp { get; set; }
        public object? Data { get; set; }
    }
}