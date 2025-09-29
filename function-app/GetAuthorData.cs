using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InkStainedWretch.Function;

public class GetAuthorData
{
    private readonly ILogger<GetAuthorData> _logger;
    private readonly InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService _authorDataService;

    public GetAuthorData(ILogger<GetAuthorData> logger, InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService authorDataService)
    {
        _logger = logger;
        _authorDataService = authorDataService;
    }

    [Function("GetAuthorData")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetAuthorData/{topLevelDomain}/{secondLevelDomain}/{languageName}/{regionName?}")] HttpRequest req,
        string topLevelDomain,
        string secondLevelDomain,
        string languageName,
        string? regionName)
    {
        _logger.LogInformation($"Received request for TLD: {topLevelDomain}, SLD: {secondLevelDomain}, Language: {languageName}, Region: {regionName}");
        var result = _authorDataService.GetAuthorWithDataAsync(topLevelDomain, secondLevelDomain, languageName, regionName).GetAwaiter().GetResult();
        if (result == null)
        {
            return new NotFoundObjectResult("No author found for the specified domain and culture.");
        }
        return new OkObjectResult(result);
    }
}