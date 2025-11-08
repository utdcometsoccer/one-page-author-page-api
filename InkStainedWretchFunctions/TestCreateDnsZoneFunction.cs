using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using System.Net;

namespace InkStainedWretch.OnePageAuthorAPI.Functions
{
    /// <summary>
    /// Test function to manually trigger DNS zone creation logic for debugging
    /// </summary>
    public class TestCreateDnsZoneFunction
    {
        private readonly ILogger<TestCreateDnsZoneFunction> _logger;
        private readonly IDnsZoneService _dnsZoneService;
        private readonly CreateDnsZoneFunction _createDnsZoneFunction;

        public TestCreateDnsZoneFunction(
            ILogger<TestCreateDnsZoneFunction> logger,
            IDnsZoneService dnsZoneService,
            CreateDnsZoneFunction createDnsZoneFunction)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dnsZoneService = dnsZoneService ?? throw new ArgumentNullException(nameof(dnsZoneService));
            _createDnsZoneFunction = createDnsZoneFunction ?? throw new ArgumentNullException(nameof(createDnsZoneFunction));
        }

        [Function("TestCreateDnsZone")]
        public async Task<HttpResponseData> TestRun(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("=== Testing DNS Zone Creation ===");

            // Create test domain registration
            var testDomainRegistration = new DomainRegistration
            {
                id = Guid.NewGuid().ToString(),
                Status = DomainRegistrationStatus.Pending,
                Upn = "test-user@example.com",
                Domain = new Domain
                {
                    SecondLevelDomain = "test-debug",
                    TopLevelDomain = "com"
                }
            };

            _logger.LogInformation("Test domain: {Domain}", testDomainRegistration.Domain.FullDomainName);

            try
            {
                // Test the DNS zone creation logic
                var testInput = new List<DomainRegistration> { testDomainRegistration };
                await _createDnsZoneFunction.Run(testInput);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"DNS zone creation test completed for: {testDomainRegistration.Domain.FullDomainName}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DNS zone creation test");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync($"Error: {ex.Message}");
                return response;
            }
        }
    }
}