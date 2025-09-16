using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InkStainedWretchStripe;

public class CancelSubscription
{
    private readonly ILogger<CancelSubscription> _logger;

    public CancelSubscription(ILogger<CancelSubscription> logger)
    {
        _logger = logger;
    }

    [Function("CancelSubscription")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
