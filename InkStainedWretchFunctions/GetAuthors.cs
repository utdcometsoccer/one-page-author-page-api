using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InkStainedWretchFunctions;

public class GetAuthors
{
    private readonly ILogger<GetAuthors> _logger;
    private readonly InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService _authorDataService;

    public GetAuthors(
        ILogger<GetAuthors> logger, 
        InkStainedWretch.OnePageAuthorAPI.API.IAuthorDataService authorDataService)
    {
        _logger = logger;
        _authorDataService = authorDataService;
    }

    [Function("GetAuthors")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{secondLevelDomain}/{topLevelDomain}")] HttpRequest req,
        string secondLevelDomain,
        string topLevelDomain)
    {
        _logger.LogInformation($"Received request for authors with TLD: {topLevelDomain}, SLD: {secondLevelDomain}");

        try
        {
            var authors = await _authorDataService.GetAuthorsByDomainAsync(topLevelDomain, secondLevelDomain);
            
            if (authors == null || !authors.Any())
            {
                _logger.LogInformation($"No authors found for TLD: {topLevelDomain}, SLD: {secondLevelDomain}");
                return new NotFoundObjectResult(new { error = "Domain not found" });
            }

            _logger.LogInformation($"Successfully retrieved {authors.Count} authors for TLD: {topLevelDomain}, SLD: {secondLevelDomain}");
            return new OkObjectResult(authors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authors for TLD: {TopLevelDomain}, SLD: {SecondLevelDomain}", topLevelDomain, secondLevelDomain);
            return new ObjectResult(new { error = "Internal server error" })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}