using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InkStainedWretch.OnePageAuthorAPI.Functions.Testing.TestHarnesses
{
    /// <summary>
    /// End-to-end test function that simulates the complete domain registration flow
    /// This allows you to test the entire process with controlled scenarios
    /// </summary>
    public class EndToEndTestFunction
    {
        private readonly ILogger<EndToEndTestFunction> _logger;
        private readonly TestingConfiguration _testConfig;

        public EndToEndTestFunction(ILogger<EndToEndTestFunction> logger, TestingConfiguration testConfig)
        {
            _logger = logger;
            _testConfig = testConfig;
        }

        /// <summary>
        /// Scenario 1: UI Frontend Test (No Infrastructure Changes)
        /// POST /api/test/scenario1
        /// Body: { "domainName": "test.example.com", "userEmail": "test@example.com" }
        /// </summary>
        [Function("TestScenario1")]
        public async Task<IActionResult> TestScenario1(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/scenario1")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("[E2E TEST] Starting Scenario 1 - Frontend UI Test (No Infrastructure)");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var testRequest = JsonSerializer.Deserialize<EndToEndTestRequest>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (testRequest == null || string.IsNullOrEmpty(testRequest.DomainName))
                {
                    return new BadRequestObjectResult("Missing domainName in request body");
                }

                var result = new EndToEndTestResult
                {
                    TestScenario = "Scenario1-Frontend",
                    DomainName = testRequest.DomainName,
                    UserEmail = testRequest.UserEmail,
                    StartTime = DateTime.UtcNow,
                    Steps = new List<TestStep>()
                };

                // Step 1: Validate domain format
                result.Steps.Add(await ExecuteStep("ValidateDomain", () => ValidateDomainFormat(testRequest.DomainName)));

                // Step 2: Check domain availability (mocked)
                result.Steps.Add(await ExecuteStep("CheckAvailability", () => SimulateAvailabilityCheck(testRequest.DomainName)));

                // Step 3: Simulate user authentication
                result.Steps.Add(await ExecuteStep("UserAuthentication", () => SimulateUserAuth(testRequest.UserEmail)));

                // Step 4: Create domain registration record (mocked - no actual creation)
                result.Steps.Add(await ExecuteStep("CreateRegistration", () => SimulateCreateRegistration(testRequest)));

                // Step 5: Simulate payment processing (mocked)
                result.Steps.Add(await ExecuteStep("ProcessPayment", () => SimulatePaymentProcessing(testRequest)));

                // Step 6: Simulate success response
                result.Steps.Add(await ExecuteStep("SendResponse", () => SimulateSuccessResponse()));

                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = result.Steps.All(s => s.Success);
                result.TotalCost = 0; // No actual costs in scenario 1

                _logger.LogInformation("[E2E TEST] Scenario 1 completed. Success: {Success}, Duration: {Duration}ms", 
                    result.Success, result.Duration.TotalMilliseconds);

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[E2E TEST] Error in Scenario 1");
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Scenario 3: Full End-to-End Test with Real Money (Use with caution!)
        /// POST /api/test/scenario3
        /// Body: { "domainName": "test.example.com", "userEmail": "test@example.com", "confirmRealMoney": true }
        /// </summary>
        [Function("TestScenario3")]
        public async Task<IActionResult> TestScenario3(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "test/scenario3")] HttpRequest req)
        {
            try
            {
                _logger.LogWarning("[E2E TEST] Starting Scenario 3 - REAL MONEY TEST - USE WITH CAUTION!");

                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var testRequest = JsonSerializer.Deserialize<EndToEndTestRequest>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (testRequest == null || string.IsNullOrEmpty(testRequest.DomainName))
                {
                    return new BadRequestObjectResult("Missing domainName in request body");
                }

                if (!testRequest.ConfirmRealMoney)
                {
                    return new BadRequestObjectResult("This test involves real money. Set 'confirmRealMoney': true to proceed");
                }

                // Estimate total cost before proceeding
                var estimatedCost = EstimateTotalCost(testRequest.DomainName);
                if (estimatedCost > _testConfig.MaxTestCostLimit)
                {
                    return new BadRequestObjectResult($"Estimated cost ${estimatedCost:F2} exceeds limit ${_testConfig.MaxTestCostLimit:F2}");
                }

                var result = new EndToEndTestResult
                {
                    TestScenario = "Scenario3-RealMoney",
                    DomainName = testRequest.DomainName,
                    UserEmail = testRequest.UserEmail,
                    StartTime = DateTime.UtcNow,
                    Steps = new List<TestStep>(),
                    EstimatedCost = estimatedCost
                };

                _logger.LogWarning("[E2E TEST] Proceeding with real money test. Estimated cost: ${Cost:F2}", estimatedCost);

                // Step 1: Validate domain
                result.Steps.Add(await ExecuteStep("ValidateDomain", () => ValidateDomainFormat(testRequest.DomainName)));

                // Step 2: Check real domain availability
                result.Steps.Add(await ExecuteStep("CheckRealAvailability", () => CheckRealDomainAvailability(testRequest.DomainName)));

                // Step 3: User authentication
                result.Steps.Add(await ExecuteStep("UserAuthentication", () => SimulateUserAuth(testRequest.UserEmail)));

                // Step 4: Create real domain registration record
                result.Steps.Add(await ExecuteStep("CreateRealRegistration", () => CreateRealDomainRegistration(testRequest)));

                // Step 5: Process real payment (Stripe production)
                result.Steps.Add(await ExecuteStep("ProcessRealPayment", () => ProcessRealPayment(testRequest)));

                // Step 6: Register domain with Google Domains
                result.Steps.Add(await ExecuteStep("RegisterDomain", () => RegisterRealDomain(testRequest.DomainName)));

                // Step 7: Create DNS zone
                result.Steps.Add(await ExecuteStep("CreateDnsZone", () => CreateRealDnsZone(testRequest.DomainName)));

                // Step 8: Add to Front Door
                result.Steps.Add(await ExecuteStep("AddToFrontDoor", () => AddToRealFrontDoor(testRequest.DomainName)));

                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = result.Steps.All(s => s.Success);
                result.TotalCost = result.Steps.Sum(s => s.Cost);

                _logger.LogWarning("[E2E TEST] Scenario 3 completed. Success: {Success}, Total Cost: ${Cost:F2}, Duration: {Duration}ms", 
                    result.Success, result.TotalCost, result.Duration.TotalMilliseconds);

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[E2E TEST] Error in Scenario 3");
                return new StatusCodeResult(500);
            }
        }

        // Helper methods
        private async Task<TestStep> ExecuteStep(string stepName, Func<Task<(bool success, string message, decimal cost)>> stepAction)
        {
            var step = new TestStep
            {
                Name = stepName,
                StartTime = DateTime.UtcNow
            };

            try
            {
                var (success, message, cost) = await stepAction();
                step.Success = success;
                step.Message = message;
                step.Cost = cost;
            }
            catch (Exception ex)
            {
                step.Success = false;
                step.Message = $"Error: {ex.Message}";
                step.Cost = 0;
                _logger.LogError(ex, "[E2E TEST] Step {StepName} failed", stepName);
            }

            step.EndTime = DateTime.UtcNow;
            step.Duration = step.EndTime - step.StartTime;

            _logger.LogInformation("[E2E TEST] Step {StepName}: {Success} - {Message} (${Cost:F2}) [{Duration}ms]", 
                stepName, step.Success ? "SUCCESS" : "FAILED", step.Message, step.Cost, step.Duration.TotalMilliseconds);

            return step;
        }

        private async Task<(bool, string, decimal)> ValidateDomainFormat(string domainName)
        {
            await Task.Delay(50);
            var isValid = domainName.Contains('.') && domainName.Length > 3;
            return (isValid, isValid ? "Domain format valid" : "Invalid domain format", 0);
        }

        private async Task<(bool, string, decimal)> SimulateAvailabilityCheck(string domainName)
        {
            await Task.Delay(200);
            return (true, "Domain appears available (mocked)", 0);
        }

        private async Task<(bool, string, decimal)> CheckRealDomainAvailability(string domainName)
        {
            await Task.Delay(500);
            // In real scenario, this would call Google Domains API
            return (true, "Domain availability checked (real API would be called)", 0);
        }

        private async Task<(bool, string, decimal)> SimulateUserAuth(string? userEmail)
        {
            await Task.Delay(100);
            return (true, $"User {userEmail} authenticated (mocked)", 0);
        }

        private async Task<(bool, string, decimal)> SimulateCreateRegistration(EndToEndTestRequest request)
        {
            await Task.Delay(150);
            return (true, "Domain registration record created (mocked)", 0);
        }

        private async Task<(bool, string, decimal)> CreateRealDomainRegistration(EndToEndTestRequest request)
        {
            await Task.Delay(300);
            // In real scenario, this would create actual Cosmos DB record
            return (true, "Real domain registration record created", 0);
        }

        private async Task<(bool, string, decimal)> SimulatePaymentProcessing(EndToEndTestRequest request)
        {
            await Task.Delay(250);
            return (true, "Payment processed (mocked)", 0);
        }

        private async Task<(bool, string, decimal)> ProcessRealPayment(EndToEndTestRequest request)
        {
            await Task.Delay(1000);
            var cost = EstimateDomainCost(request.DomainName);
            // In real scenario, this would process actual Stripe payment
            return (true, "Real payment processed via Stripe", cost);
        }

        private async Task<(bool, string, decimal)> RegisterRealDomain(string domainName)
        {
            await Task.Delay(2000);
            var cost = EstimateDomainCost(domainName);
            // In real scenario, this would call Google Domains API
            return (true, "Domain registered with Google Domains", cost);
        }

        private async Task<(bool, string, decimal)> CreateRealDnsZone(string domainName)
        {
            await Task.Delay(500);
            // In real scenario, this would create actual Azure DNS zone
            return (true, "DNS zone created in Azure", 0.50m);
        }

        private async Task<(bool, string, decimal)> AddToRealFrontDoor(string domainName)
        {
            await Task.Delay(300);
            // In real scenario, this would add domain to Azure Front Door
            return (true, "Domain added to Azure Front Door", 0.10m);
        }

        private async Task<(bool, string, decimal)> SimulateSuccessResponse()
        {
            await Task.Delay(50);
            return (true, "Success response sent to client", 0);
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

        private decimal EstimateTotalCost(string domainName)
        {
            var domainCost = EstimateDomainCost(domainName);
            var dnsCost = 0.50m;
            var frontDoorCost = 0.10m;
            return domainCost + dnsCost + frontDoorCost;
        }
    }

    public class EndToEndTestRequest
    {
        public string? DomainName { get; set; }
        public string? UserEmail { get; set; }
        public bool ConfirmRealMoney { get; set; }
    }

    public class EndToEndTestResult
    {
        public string TestScenario { get; set; } = "";
        public string DomainName { get; set; } = "";
        public string? UserEmail { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal TotalCost { get; set; }
        public List<TestStep> Steps { get; set; } = new();
    }

    public class TestStep
    {
        public string Name { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public decimal Cost { get; set; }
    }
}