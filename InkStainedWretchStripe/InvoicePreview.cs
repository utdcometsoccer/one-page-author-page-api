using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InkStainedWretchStripe;

public class InvoicePreview
{
    private readonly ILogger<InvoicePreview> _logger;

    public InvoicePreview(ILogger<InvoicePreview> logger)
    {
        _logger = logger;
    }

    [Function("InvoicePreview")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
