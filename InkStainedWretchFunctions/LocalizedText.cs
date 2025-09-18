using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InkStainedWretchFunctions;

public class LocalizedText
{
    private readonly ILogger<LocalizedText> _logger;

    public LocalizedText(ILogger<LocalizedText> logger)
    {
        _logger = logger;
    }

    [Function("LocalizedText")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
