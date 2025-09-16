using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InkStainedWretchStripe;

public class UpdateSubscription
{
    private readonly ILogger<UpdateSubscription> _logger;

    public UpdateSubscription(ILogger<UpdateSubscription> logger)
    {
        _logger = logger;
    }

    [Function("UpdateSubscription")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
