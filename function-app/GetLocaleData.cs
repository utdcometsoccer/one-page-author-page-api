using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InkStainedWretch.OnePageAuthorAPI.API;

namespace function_app;

public class GetLocaleData
{
    private readonly ILogger<GetLocaleData> _logger;
    private readonly ILocaleDataService _localeDataService;

    public GetLocaleData(ILogger<GetLocaleData> logger, ILocaleDataService localeDataService)
    {
        _logger = logger;
        _localeDataService = localeDataService;
    }

    [Function("GetLocaleData")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetLocaleData/{languageName}/{regionName?}")] HttpRequest req,
        string languageName,
        string? regionName)
    {
        _logger.LogInformation($"Received request for Language: {languageName}, Region: {regionName}");
        var result = _localeDataService.GetLocalesAsync(languageName, regionName).GetAwaiter().GetResult();
        if (result == null || result.Count == 0)
        {
            return new NotFoundObjectResult("No locale found for the specified language and region.");
        }
        return new OkObjectResult(result[0]);
    }
}
