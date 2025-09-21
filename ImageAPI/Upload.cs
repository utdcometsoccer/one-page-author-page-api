using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ImageAPI;

public class Upload
{
    private readonly ILogger<Upload> _logger;

    public Upload(ILogger<Upload> logger)
    {
        _logger = logger;
    }

    [Function("Upload")]
    [Authorize(Policy = "RequireRole.Admin")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
