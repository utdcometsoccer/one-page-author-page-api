using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InkStainedWretchStripe;

public class WebHook
{
    private readonly ILogger<WebHook> _logger;

    public WebHook(ILogger<WebHook> logger)
    {
        _logger = logger;
    }

    [Function("WebHook")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
