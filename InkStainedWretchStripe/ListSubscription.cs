using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InkStainedWretchStripe;

public class ListSubscription
{
    private readonly ILogger<ListSubscription> _logger;

    public ListSubscription(ILogger<ListSubscription> logger)
    {
        _logger = logger;
    }

    [Function("ListSubscription")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
